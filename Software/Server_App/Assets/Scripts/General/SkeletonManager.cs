using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

using System.Linq; // Needed for FirstOrDefault


public class SkeletonManager : MonoBehaviour
{
    [Header("Rendering")]
    [Tooltip("Select the layer (must match defined layer name).")]
    public string skeletonLayer = "SkeletonView";

    [Header("Prefabs")]
    public SkinnedMeshRenderer characterMeshRenderer;
    public Transform skeletonParentTransform;
    public GameObject jointPrefab;
    public Material boneMaterial;
    public Camera skeletonCam;
    public Transform groundPlaneTransform;

    [Header("Skeleton Settings")]
    public float scale = 1f;
    public float hipDistanceFromCamera = 3f;

    private Skeleton skeleton;
    private Dictionary<SkeletonJoint, Transform> joints = new();
    private List<(Transform from, Transform to, LineRenderer line)> bones = new();
    private Vector3 origin;
    private BVHStreamRecorder stream;

    // --- BVH capture rate gating ---
    private float captureDt = 1f / 30f;  // seconds per captured frame
    private float accum = 0f;
    private bool isStreaming = false;


    private Transform CreateJoint(SkeletonJoint jointName, Vector3 position, Transform parent = null)
    {
        GameObject joint = Instantiate(jointPrefab, position, Quaternion.identity, skeletonParentTransform);
        joint.name = jointName.ToString();

        joints[jointName] = joint.transform;

        if (parent != null)
            joint.transform.SetParent(parent, worldPositionStays: true);

        return joint.transform;
    }

    private void DrawBone(SkeletonJoint from, SkeletonJoint to, float lineWidth)
    {
        GameObject lineObj = new GameObject($"Bone_{from}_{to}");
        lineObj.transform.SetParent(skeletonParentTransform);

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.material = boneMaterial;
        line.positionCount = 2;
        line.startWidth = line.endWidth = lineWidth;
        line.useWorldSpace = true;

        Transform fromTransform = joints[from];
        Transform toTransform = joints[to];

        line.SetPosition(0, fromTransform.position);
        line.SetPosition(1, toTransform.position);

        bones.Add((fromTransform, toTransform, line));
    }

    public void ApplyIMURotations(Dictionary<SkeletonJoint, Quaternion> imuRotations)
    {
        foreach (var kvp in imuRotations)
        {
            SkeletonJoint jointName = kvp.Key;
            Quaternion imuGlobalRotation = kvp.Value;

            if (joints.ContainsKey(jointName))
            {
                Transform joint = joints[jointName];
                Transform parent = joint.parent;
                joint.localRotation = Quaternion.Inverse(parent.rotation) * imuGlobalRotation;
            }
        }

        foreach (var (from, to, line) in bones)
        {
            line.SetPosition(0, from.position);
            line.SetPosition(1, to.position);
        }
    }

public void DrawSkeleton(float height_m=1.75f)
    {
        skeleton = new Skeleton(height_m);
        var jointsPos = skeleton.GetJoints();

        // Assume RIGHT_FOOT is lowest
        float rightFootLocalY = jointsPos[SkeletonJoint.RIGHT_FOOT].y;
        float groundY = groundPlaneTransform != null ? groundPlaneTransform.position.y : 0f;

        Vector3 hipForward = skeletonCam.transform.position + skeletonCam.transform.forward * hipDistanceFromCamera;
        float verticalOffset = groundY - rightFootLocalY * scale;

        // Place hip at correct forward position and height so foot touches ground
        origin = new Vector3(hipForward.x, verticalOffset, hipForward.z);

        // Shortcut
        Vector3 World(SkeletonJoint j) => origin + jointsPos[j] * scale;

        // Create joints in hierarchy order
        CreateJoint(SkeletonJoint.HIP, World(SkeletonJoint.HIP));

        CreateJoint(SkeletonJoint.WAIST, World(SkeletonJoint.WAIST), joints[SkeletonJoint.HIP]);
        CreateJoint(SkeletonJoint.CHEST, World(SkeletonJoint.CHEST), joints[SkeletonJoint.WAIST]);
        CreateJoint(SkeletonJoint.UPPER_CHEST, World(SkeletonJoint.UPPER_CHEST), joints[SkeletonJoint.CHEST]);
        CreateJoint(SkeletonJoint.NECK, World(SkeletonJoint.NECK), joints[SkeletonJoint.UPPER_CHEST]);
        CreateJoint(SkeletonJoint.HEAD, World(SkeletonJoint.HEAD), joints[SkeletonJoint.NECK]);

        CreateJoint(SkeletonJoint.LEFT_SHOULDER, World(SkeletonJoint.LEFT_SHOULDER), joints[SkeletonJoint.UPPER_CHEST]);
        CreateJoint(SkeletonJoint.LEFT_ELBOW, World(SkeletonJoint.LEFT_ELBOW), joints[SkeletonJoint.LEFT_SHOULDER]);
        CreateJoint(SkeletonJoint.LEFT_WRIST, World(SkeletonJoint.LEFT_WRIST), joints[SkeletonJoint.LEFT_ELBOW]);
        CreateJoint(SkeletonJoint.LEFT_HAND, World(SkeletonJoint.LEFT_HAND), joints[SkeletonJoint.LEFT_WRIST]);

        CreateJoint(SkeletonJoint.RIGHT_SHOULDER, World(SkeletonJoint.RIGHT_SHOULDER), joints[SkeletonJoint.UPPER_CHEST]);
        CreateJoint(SkeletonJoint.RIGHT_ELBOW, World(SkeletonJoint.RIGHT_ELBOW), joints[SkeletonJoint.RIGHT_SHOULDER]);
        CreateJoint(SkeletonJoint.RIGHT_WRIST, World(SkeletonJoint.RIGHT_WRIST), joints[SkeletonJoint.RIGHT_ELBOW]);
        CreateJoint(SkeletonJoint.RIGHT_HAND, World(SkeletonJoint.RIGHT_HAND), joints[SkeletonJoint.RIGHT_WRIST]);

        CreateJoint(SkeletonJoint.LEFT_HIP, World(SkeletonJoint.LEFT_HIP), joints[SkeletonJoint.HIP]);
        CreateJoint(SkeletonJoint.LEFT_KNEE, World(SkeletonJoint.LEFT_KNEE), joints[SkeletonJoint.LEFT_HIP]);
        CreateJoint(SkeletonJoint.LEFT_ANKLE, World(SkeletonJoint.LEFT_ANKLE), joints[SkeletonJoint.LEFT_KNEE]);
        CreateJoint(SkeletonJoint.LEFT_FOOT, World(SkeletonJoint.LEFT_FOOT), joints[SkeletonJoint.LEFT_ANKLE]);

        CreateJoint(SkeletonJoint.RIGHT_HIP, World(SkeletonJoint.RIGHT_HIP), joints[SkeletonJoint.HIP]);
        CreateJoint(SkeletonJoint.RIGHT_KNEE, World(SkeletonJoint.RIGHT_KNEE), joints[SkeletonJoint.RIGHT_HIP]);
        CreateJoint(SkeletonJoint.RIGHT_ANKLE, World(SkeletonJoint.RIGHT_ANKLE), joints[SkeletonJoint.RIGHT_KNEE]);
        CreateJoint(SkeletonJoint.RIGHT_FOOT, World(SkeletonJoint.RIGHT_FOOT), joints[SkeletonJoint.RIGHT_ANKLE]);

        // Draw bones
        float lineWidth = Mathf.Max(0.005f, scale / 60f);
        void Connect(SkeletonJoint a, SkeletonJoint b) => DrawBone(a, b, lineWidth);

        Connect(SkeletonJoint.HIP, SkeletonJoint.WAIST);
        Connect(SkeletonJoint.WAIST, SkeletonJoint.CHEST);
        Connect(SkeletonJoint.CHEST, SkeletonJoint.UPPER_CHEST);
        Connect(SkeletonJoint.UPPER_CHEST, SkeletonJoint.NECK);
        Connect(SkeletonJoint.NECK, SkeletonJoint.HEAD);

        Connect(SkeletonJoint.UPPER_CHEST, SkeletonJoint.LEFT_SHOULDER);
        Connect(SkeletonJoint.LEFT_SHOULDER, SkeletonJoint.LEFT_ELBOW);
        Connect(SkeletonJoint.LEFT_ELBOW, SkeletonJoint.LEFT_WRIST);
        Connect(SkeletonJoint.LEFT_WRIST, SkeletonJoint.LEFT_HAND);

        Connect(SkeletonJoint.UPPER_CHEST, SkeletonJoint.RIGHT_SHOULDER);
        Connect(SkeletonJoint.RIGHT_SHOULDER, SkeletonJoint.RIGHT_ELBOW);
        Connect(SkeletonJoint.RIGHT_ELBOW, SkeletonJoint.RIGHT_WRIST);
        Connect(SkeletonJoint.RIGHT_WRIST, SkeletonJoint.RIGHT_HAND);

        Connect(SkeletonJoint.HIP, SkeletonJoint.LEFT_HIP);
        Connect(SkeletonJoint.LEFT_HIP, SkeletonJoint.LEFT_KNEE);
        Connect(SkeletonJoint.LEFT_KNEE, SkeletonJoint.LEFT_ANKLE);
        Connect(SkeletonJoint.LEFT_ANKLE, SkeletonJoint.LEFT_FOOT);

        Connect(SkeletonJoint.HIP, SkeletonJoint.RIGHT_HIP);
        Connect(SkeletonJoint.RIGHT_HIP, SkeletonJoint.RIGHT_KNEE);
        Connect(SkeletonJoint.RIGHT_KNEE, SkeletonJoint.RIGHT_ANKLE);
        Connect(SkeletonJoint.RIGHT_ANKLE, SkeletonJoint.RIGHT_FOOT);
    }

    // Expose joints map to the exporter/recorder
    public void StartBVHStreaming(string outPath, float fps = 30f)
    {
        captureDt = 1f / Mathf.Max(1f, fps);
        accum = 0f;
        isStreaming = true;

        // ensure joints exist; build your skeleton first
        stream = new BVHStreamRecorder(fps, outPath);
        stream.SetRestOffsetsMeters(BuildRestOffsetsMeters()); // <-- use bone lengths (m)
        stream.Begin();
    }

    public void StopBVHStreaming()
    {
        isStreaming = false;
        stream?.Finish();
        stream = null;
    }

    private Dictionary<SkeletonJoint, Vector3> BuildRestOffsetsMeters()
    {
        var pos = skeleton.GetJoints(); // meters
        var parent = Skeleton.BuildParentMap();

        var offsetsM = new Dictionary<SkeletonJoint, Vector3>(pos.Count);
        foreach (var j in Skeleton.DepthFirstOrder)
        {
            var p = parent[j];
            if (p == null)
                offsetsM[j] = Vector3.zero;                      // root offset is 0
            else
                offsetsM[j] = pos[j] - pos[p.Value];             // parent->child in meters
        }
        return offsetsM;
    }

    // Call this after ApplyIMURotations() each frame:
    public void EnqueueCurrentPoseForBVH()
    {
        if (stream == null) return;

        var snap = new Dictionary<SkeletonJoint, BVHExporter.TransformSnapshot>(joints.Count);
        foreach (var j in Skeleton.DepthFirstOrder)
        {
            if (!joints.TryGetValue(j, out var t)) continue;
            snap[j] = new BVHExporter.TransformSnapshot
            {
                worldPosition = t.position,
                localRotation = t.localRotation,
                localPosition = t.localPosition
            };
        }
        stream.EnqueueFrame(snap);
    }

    private void Update()
    {
        if (isStreaming && stream != null)
        {
            accum += Time.unscaledDeltaTime; // unscaled avoids Time.timeScale effects
            while (accum + 1e-6f >= captureDt)
            {
                EnqueueCurrentPoseForBVH();   // exactly one BVH frame per _captureDt
                accum -= captureDt;
            }
        }

    }

}