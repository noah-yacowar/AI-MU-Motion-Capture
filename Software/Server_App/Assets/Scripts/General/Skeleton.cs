using System.Collections.Generic;
using UnityEngine;

public class Skeleton
{
    private Dictionary<SkeletonJoint, Vector3> jointLocalPositions = new();

    public float height_m;

    // Spine
    public float hipToWaist;
    public float waistToChest;
    public float chestToUpperChest;
    public float upperChestToNeck;
    public float neck;
    public float head;

    // Arms
    public float shoulderWidth;
    public float upperArm;
    public float lowerArm;
    public float hand;

    // Legs
    public float hipOffset; // lateral offset from hip center
    public float upperLeg;
    public float lowerLeg;
    public float foot;

    public Skeleton(float height_m)
    {
        this.height_m = height_m;

        // Spine (torso total ≈ 0.292 → split)
        hipToWaist = height_m * 0.097f;
        waistToChest = height_m * 0.097f;
        chestToUpperChest = height_m * 0.048f;
        upperChestToNeck = height_m * 0.048f;

        neck = height_m * 0.030f;
        head = height_m * 0.130f;

        // Shoulders and arms
        shoulderWidth = height_m * 0.240f;
        upperArm = height_m * 0.186f;
        lowerArm = height_m * 0.146f;
        hand = height_m * 0.108f;

        // Legs
        hipOffset = height_m * 0.1f;
        upperLeg = height_m * 0.245f;
        lowerLeg = height_m * 0.246f;
        foot = height_m * 0.152f;

        GenerateLocalSkeleton();
    }

    public Dictionary<SkeletonJoint, Vector3> GetJoints()
    {
        return jointLocalPositions;
    }

    private void GenerateLocalSkeleton()
    {
        jointLocalPositions.Clear();

        Vector3 hip = Vector3.zero;
        jointLocalPositions[SkeletonJoint.HIP] = hip;
        jointLocalPositions[SkeletonJoint.WAIST] = hip + Vector3.up * hipToWaist;
        jointLocalPositions[SkeletonJoint.CHEST] = jointLocalPositions[SkeletonJoint.WAIST] + Vector3.up * waistToChest;
        jointLocalPositions[SkeletonJoint.UPPER_CHEST] = jointLocalPositions[SkeletonJoint.CHEST] + Vector3.up * chestToUpperChest;
        jointLocalPositions[SkeletonJoint.NECK] = jointLocalPositions[SkeletonJoint.UPPER_CHEST] + Vector3.up * upperChestToNeck;
        jointLocalPositions[SkeletonJoint.HEAD] = jointLocalPositions[SkeletonJoint.NECK] + Vector3.up * neck + Vector3.up * head * 0.5f;

        Vector3 rShoulder = jointLocalPositions[SkeletonJoint.UPPER_CHEST] + Vector3.left * (shoulderWidth / 2);
        Vector3 lShoulder = jointLocalPositions[SkeletonJoint.UPPER_CHEST] + Vector3.right * (shoulderWidth / 2);
        jointLocalPositions[SkeletonJoint.LEFT_SHOULDER] = lShoulder;
        jointLocalPositions[SkeletonJoint.RIGHT_SHOULDER] = rShoulder;

        jointLocalPositions[SkeletonJoint.LEFT_ELBOW] = lShoulder + Vector3.down * upperArm;
        jointLocalPositions[SkeletonJoint.LEFT_WRIST] = jointLocalPositions[SkeletonJoint.LEFT_ELBOW] + Vector3.down * lowerArm;
        jointLocalPositions[SkeletonJoint.LEFT_HAND] = jointLocalPositions[SkeletonJoint.LEFT_WRIST] + Vector3.down * hand * 0.5f;

        jointLocalPositions[SkeletonJoint.RIGHT_ELBOW] = rShoulder + Vector3.down * upperArm;
        jointLocalPositions[SkeletonJoint.RIGHT_WRIST] = jointLocalPositions[SkeletonJoint.RIGHT_ELBOW] + Vector3.down * lowerArm;
        jointLocalPositions[SkeletonJoint.RIGHT_HAND] = jointLocalPositions[SkeletonJoint.RIGHT_WRIST] + Vector3.down * hand * 0.5f;

        Vector3 rHip = hip + Vector3.left * hipOffset;
        Vector3 lHip = hip + Vector3.right * hipOffset;
        jointLocalPositions[SkeletonJoint.LEFT_HIP] = lHip;
        jointLocalPositions[SkeletonJoint.LEFT_KNEE] = jointLocalPositions[SkeletonJoint.LEFT_HIP] + Vector3.down * upperLeg;
        jointLocalPositions[SkeletonJoint.LEFT_ANKLE] = jointLocalPositions[SkeletonJoint.LEFT_KNEE] + Vector3.down * lowerLeg;
        jointLocalPositions[SkeletonJoint.LEFT_FOOT] = jointLocalPositions[SkeletonJoint.LEFT_ANKLE] + Vector3.back * foot * 0.5f;

        jointLocalPositions[SkeletonJoint.RIGHT_HIP] = rHip;
        jointLocalPositions[SkeletonJoint.RIGHT_KNEE] = jointLocalPositions[SkeletonJoint.RIGHT_HIP] + Vector3.down * upperLeg;
        jointLocalPositions[SkeletonJoint.RIGHT_ANKLE] = jointLocalPositions[SkeletonJoint.RIGHT_KNEE] + Vector3.down * lowerLeg;
        jointLocalPositions[SkeletonJoint.RIGHT_FOOT] = jointLocalPositions[SkeletonJoint.RIGHT_ANKLE] + Vector3.back * foot * 0.5f;
    }

    // The hierarchy/order must match your SkeletonManager creation order and parent-child structure
    public static readonly SkeletonJoint[] DepthFirstOrder = new SkeletonJoint[]
    {
        // ROOT subtree
        SkeletonJoint.HIP,
            SkeletonJoint.WAIST,
                SkeletonJoint.CHEST,
                    SkeletonJoint.UPPER_CHEST,
                        SkeletonJoint.NECK,
                            SkeletonJoint.HEAD,
                        SkeletonJoint.LEFT_SHOULDER,
                            SkeletonJoint.LEFT_ELBOW,
                                SkeletonJoint.LEFT_WRIST,
                                    SkeletonJoint.LEFT_HAND,
                        SkeletonJoint.RIGHT_SHOULDER,
                            SkeletonJoint.RIGHT_ELBOW,
                                SkeletonJoint.RIGHT_WRIST,
                                    SkeletonJoint.RIGHT_HAND,
            SkeletonJoint.LEFT_HIP,
                SkeletonJoint.LEFT_KNEE,
                    SkeletonJoint.LEFT_ANKLE,
                        SkeletonJoint.LEFT_FOOT,
            SkeletonJoint.RIGHT_HIP,
                SkeletonJoint.RIGHT_KNEE,
                    SkeletonJoint.RIGHT_ANKLE,
                        SkeletonJoint.RIGHT_FOOT,
    };

    // Build a parent map consistent with your DrawSkeleton() parenting
    public static Dictionary<SkeletonJoint, SkeletonJoint?> BuildParentMap()
    {
        var p = new Dictionary<SkeletonJoint, SkeletonJoint?>();
        p[SkeletonJoint.HIP] = null; // ROOT
        p[SkeletonJoint.WAIST] = SkeletonJoint.HIP;
        p[SkeletonJoint.CHEST] = SkeletonJoint.WAIST;
        p[SkeletonJoint.UPPER_CHEST] = SkeletonJoint.CHEST;
        p[SkeletonJoint.NECK] = SkeletonJoint.UPPER_CHEST;
        p[SkeletonJoint.HEAD] = SkeletonJoint.NECK;

        p[SkeletonJoint.LEFT_SHOULDER] = SkeletonJoint.UPPER_CHEST;
        p[SkeletonJoint.LEFT_ELBOW] = SkeletonJoint.LEFT_SHOULDER;
        p[SkeletonJoint.LEFT_WRIST] = SkeletonJoint.LEFT_ELBOW;
        p[SkeletonJoint.LEFT_HAND] = SkeletonJoint.LEFT_WRIST;

        p[SkeletonJoint.RIGHT_SHOULDER] = SkeletonJoint.UPPER_CHEST;
        p[SkeletonJoint.RIGHT_ELBOW] = SkeletonJoint.RIGHT_SHOULDER;
        p[SkeletonJoint.RIGHT_WRIST] = SkeletonJoint.RIGHT_ELBOW;
        p[SkeletonJoint.RIGHT_HAND] = SkeletonJoint.RIGHT_WRIST;

        p[SkeletonJoint.LEFT_HIP] = SkeletonJoint.HIP;
        p[SkeletonJoint.LEFT_KNEE] = SkeletonJoint.LEFT_HIP;
        p[SkeletonJoint.LEFT_ANKLE] = SkeletonJoint.LEFT_KNEE;
        p[SkeletonJoint.LEFT_FOOT] = SkeletonJoint.LEFT_ANKLE;

        p[SkeletonJoint.RIGHT_HIP] = SkeletonJoint.HIP;
        p[SkeletonJoint.RIGHT_KNEE] = SkeletonJoint.RIGHT_HIP;
        p[SkeletonJoint.RIGHT_ANKLE] = SkeletonJoint.RIGHT_KNEE;
        p[SkeletonJoint.RIGHT_FOOT] = SkeletonJoint.RIGHT_ANKLE;
        return p;
    }

}


public enum SkeletonJoint
{
    // Spine
    HIP,
    WAIST,
    CHEST,
    UPPER_CHEST,
    NECK,
    HEAD,

    // Arms - Left
    LEFT_SHOULDER,
    LEFT_ELBOW,
    LEFT_WRIST,
    LEFT_HAND,

    // Arms - Right
    RIGHT_SHOULDER,
    RIGHT_ELBOW,
    RIGHT_WRIST,
    RIGHT_HAND,

    // Legs - Left
    LEFT_HIP,
    LEFT_KNEE,
    LEFT_ANKLE,
    LEFT_FOOT,

    // Legs - Right
    RIGHT_HIP,
    RIGHT_KNEE,
    RIGHT_ANKLE,
    RIGHT_FOOT
}
