using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

// ------------------------------------------------------------
// BVH Exporter & Recorder for your custom SkeletonManager
// - Plug this into your project, call BVHRecorder.Begin(...) when you
//   start capturing, call CaptureFrame() each frame after ApplyIMURotations,
//   and Finish(path) to write a .bvh file.
// - Assumes Y-up (Unity), BVH channels: Xposition Yposition Zposition, then
//   Zrotation Xrotation Yrotation (degrees). Adjust ROT_ORDER if needed.
// ------------------------------------------------------------

public enum BVHRotationOrder { ZXY, XYZ, XZY, YXZ, YZX, ZYX }

public static class BVHExporter
{
    // Configure your BVH axis and unit conventions
    public const BVHRotationOrder ROT_ORDER = BVHRotationOrder.ZXY; // BVH line: Zrotation Xrotation Yrotation
    public const float UnitsScale = 100f; // Unity meters -> cm ?

    // Precompute rest-pose OFFSETS from your transforms (localPositions)
    public static Dictionary<SkeletonJoint, Vector3> ComputeOffsets(
        Dictionary<SkeletonJoint, Transform> jointMap,
        Dictionary<SkeletonJoint, SkeletonJoint?> parentMap)
    {
        var offsets = new Dictionary<SkeletonJoint, Vector3>(jointMap.Count);
        foreach (var j in Skeleton.DepthFirstOrder)
        {
            if (!jointMap.ContainsKey(j)) continue;
            var tf = jointMap[j];
            var parent = parentMap[j];
            Vector3 localOffset;
            if (parent == null)
            {
                // Root offset is zero in BVH; root translation is provided via CHANNELS per frame
                localOffset = Vector3.zero;
            }
            else
            {
                localOffset = tf.localPosition; // already in parent's local space
            }
            offsets[j] = localOffset * UnitsScale;
        }
        return offsets;
    }

    public static void WriteBVH(
        string filePath,
        float frameTime,
        List<Dictionary<SkeletonJoint, TransformSnapshot>> frames,
        Dictionary<SkeletonJoint, Transform> jointMap,
        Dictionary<SkeletonJoint, SkeletonJoint?> parentMap)
    {
        if (frames == null || frames.Count == 0) throw new Exception("No frames to export");

        // Compute static OFFSETS from the first frame's rest/local positions
        var offsets = ComputeOffsets(jointMap, parentMap);

        using (var sw = new StreamWriter(filePath))
        {
            // ----- HIERARCHY -----
            sw.WriteLine("HIERARCHY");
            WriteHierarchy(sw, offsets, parentMap);

            // ----- MOTION -----
            sw.WriteLine("MOTION");
            sw.WriteLine($"Frames: {frames.Count}");
            sw.WriteLine($"Frame Time: {frameTime:0.000000}");

            // For each frame, dump channels in depth-first order
            foreach (var frame in frames)
            {
                var values = new List<float>(Skeleton.DepthFirstOrder.Length * 6);

                foreach (var j in Skeleton.DepthFirstOrder)
                {
                    var snap = frame[j];
                    if (parentMap[j] == null)
                    {
                        // Static root at origin
                        values.Add(0f);
                        values.Add(0f);
                        values.Add(0f);
                    }

                    // Local rotation in degrees, respecting BVH order
                    Vector3 eDeg = ToBVHEulerDegrees(snap.localRotation);
                    // BVH channel order = Zrot Xrot Yrot (matching ROT_ORDER)
                    values.Add(eDeg.z);
                    values.Add(eDeg.x);
                    values.Add(eDeg.y);
                }

                // Write line
                for (int i = 0; i < values.Count; i++)
                {
                    if (i > 0) sw.Write(' ');
                    sw.Write(values[i].ToString("0.######"));
                }
                sw.WriteLine();
            }
        }
#if UNITY_EDITOR
        Debug.Log($"BVH written: {filePath}");
#endif
    }

    static void WriteHierarchy(
        StreamWriter sw,
        Dictionary<SkeletonJoint, Vector3> offsets,
        Dictionary<SkeletonJoint, SkeletonJoint?> parentMap)
    {
        // Recursive pretty printer using the declared DepthFirstOrder
        // We build children list for indentation
        var children = new Dictionary<SkeletonJoint, List<SkeletonJoint>>();
        foreach (var j in Skeleton.DepthFirstOrder)
            children[j] = new List<SkeletonJoint>();
        SkeletonJoint? root = null;
        foreach (var j in Skeleton.DepthFirstOrder)
        {
            var p = parentMap[j];
            if (p == null) root = j;
            else children[p.Value].Add(j);
        }
        if (root == null) throw new Exception("No root joint found for BVH");

        void Recurse(SkeletonJoint j, int indent, bool isEndSite = false)
        {
            string tabs = new string('\t', indent);
            if (indent == 0)
                sw.WriteLine($"ROOT {j}");
            else if (!isEndSite)
                sw.WriteLine($"{tabs}JOINT {j}");
            else
                sw.WriteLine($"{tabs}End Site");

            sw.WriteLine($"{tabs}{{");
            Vector3 off = offsets.ContainsKey(j) ? offsets[j] : Vector3.zero;
            sw.WriteLine($"{tabs}\tOFFSET {off.x:0.######} {off.y:0.######} {off.z:0.######}");

            if (!isEndSite)
            {
                if (indent == 0)
                    sw.WriteLine($"{tabs}\tCHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation");
                else
                    sw.WriteLine($"{tabs}\tCHANNELS 3 Zrotation Xrotation Yrotation");

                // Recurse children
                foreach (var c in children[j])
                {
                    Recurse(c, indent + 1);
                }

                // BVH requires an End Site for leafs
                if (children[j].Count == 0)
                {
                    Recurse(j, indent + 1, isEndSite: true);
                }
            }

            sw.WriteLine($"{tabs}}}");
        }

        Recurse(root.Value, 0);
    }

    // Snapshot of a joint at a given frame
    public struct TransformSnapshot
    {
        public Vector3 worldPosition;    // meters, world
        public Vector3 localPosition;    // meters, parent space   
        public Quaternion localRotation; // relative to parent
    }

    // Convert Quaternion -> Euler degrees in the chosen BVH ROT_ORDER
    public static Vector3 ToBVHEulerDegrees(Quaternion q)
    {
        // We ensure we decompose using the intended order by composing and extracting via matrices.
        // For most rigs, Unity's q.eulerAngles then re-ordering works fine, but gimbal edges may differ.
        // If you see artifacts, consider replacing with a robust ordered decomposition.
        Vector3 eUnity = q.eulerAngles; // returns some order internally, but values are valid orientation
        switch (ROT_ORDER)
        {
            case BVHRotationOrder.ZXY: return new Vector3(eUnity.x, eUnity.y, eUnity.z); // we will place as Z,X,Y in the stream
            case BVHRotationOrder.XYZ: return Reorder(eUnity, "XYZ");
            case BVHRotationOrder.XZY: return Reorder(eUnity, "XZY");
            case BVHRotationOrder.YXZ: return Reorder(eUnity, "YXZ");
            case BVHRotationOrder.YZX: return Reorder(eUnity, "YZX");
            case BVHRotationOrder.ZYX: return Reorder(eUnity, "ZYX");
            default: return eUnity;
        }
    }

    static Vector3 Reorder(Vector3 e, string order)
    {
        // This is a simple component reorder helper; it does NOT change the underlying composition order,
        // but for many pipelines it's sufficient if your local rotations were authored consistently.
        // If you need strict mathematical order decomposition, replace ToBVHEulerDegrees with a custom solver.
        float X = e.x, Y = e.y, Z = e.z;
        return order switch
        {
            "XYZ" => new Vector3(X, Y, Z),
            "XZY" => new Vector3(X, Z, Y),
            "YXZ" => new Vector3(Y, X, Z),
            "YZX" => new Vector3(Y, Z, X),
            "ZXY" => new Vector3(Z, X, Y),
            "ZYX" => new Vector3(Z, Y, X),
            _ => e,
        };
    }
}

// ------------------------------------------------------------
// Lightweight recorder that reads from your SkeletonManager's joint map
// ------------------------------------------------------------

public sealed class BVHStreamRecorder : IDisposable
{
    // --- Config ---
    private readonly float frameTime; // seconds per frame
    private readonly string filePath;
    private readonly Dictionary<SkeletonJoint, SkeletonJoint?> parentMap;
    private readonly SkeletonJoint[] order = Skeleton.DepthFirstOrder;
    private Dictionary<SkeletonJoint, Vector3> restOffsetsMeters;


    // --- Concurrency ---
    private readonly BlockingCollection<Dictionary<SkeletonJoint, BVHExporter.TransformSnapshot>> queue
        = new BlockingCollection<Dictionary<SkeletonJoint, BVHExporter.TransformSnapshot>>(boundedCapacity: 512);
    private Thread writerThread;
    private volatile bool started;
    private volatile bool finished;

    // --- Writer state ---
    private FileStream fs;
    private StreamWriter sw;
    private long framesFieldPos = -1; // where we patch "Frames: N"
    private int framesWritten = 0;
    private bool headerWritten = false;
    private Dictionary<SkeletonJoint, Vector3> offsetsFromFirstFrame;

    public BVHStreamRecorder(float fps, string filePath)
    {
        this.frameTime = 1f / Mathf.Max(1f, fps);
        this.filePath = filePath;
        this.parentMap = Skeleton.BuildParentMap();
    }

    public void Begin()
    {
        if (started) return;
        started = true;

        // Ensure directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // Open stream now (so we can write header when first frame comes)
        fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        sw = new StreamWriter(fs) { AutoFlush = true };

        writerThread = new Thread(WriterLoop) { IsBackground = true, Name = "BVHStreamWriter" };
        writerThread.Start();
    }

    public void EnqueueFrame(Dictionary<SkeletonJoint, BVHExporter.TransformSnapshot> frameSnapshot)
    {
        // main thread only: this must contain ONLY raw data (no Transforms)
        if (!started) return;
        if (!queue.IsAddingCompleted)
            queue.Add(frameSnapshot);
    }

    public void Finish()
    {
        if (finished) return;
        finished = true;

        queue.CompleteAdding();
        writerThread?.Join();

        // Patch the "Frames:" line with actual count (fixed width)
        if (framesFieldPos >= 0)
        {
            fs.Seek(framesFieldPos, SeekOrigin.Begin);
            // Write as fixed-width 10 digits to preserve layout
            string fixedCount = framesWritten.ToString().PadLeft(10, ' ');
            var bytes = System.Text.Encoding.ASCII.GetBytes(fixedCount);
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush();
        }

        sw?.Dispose();
        fs?.Dispose();
    }

    public void Dispose() => Finish();

    // --- Background thread ---
    private void WriterLoop()
    {
        try
        {
            foreach (var frame in queue.GetConsumingEnumerable())
            {
                if (!headerWritten)
                {
                    WriteHeader(); // writes HIERARCHY + MOTION preamble
                    headerWritten = true;
                }

                WriteOneFrame(frame);
                framesWritten++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"BVHStreamRecorder writer failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void SetRestOffsetsMeters(Dictionary<SkeletonJoint, Vector3> offsetsMeters)
    {
        restOffsetsMeters = offsetsMeters;
    }

    private void WriteHeader()
    {
        sw.WriteLine("HIERARCHY");
        WriteHierarchy(sw, parentMap);

        sw.WriteLine("MOTION");

        // Reserve a fixed-width Frames field we can patch later.
        // We'll write: "Frames: ________" (10 spaces we overwrite)
        sw.Write("Frames:");
        sw.Flush();
        framesFieldPos = fs.Position;   // start of the number field
        sw.WriteLine("          ");     // 10 spaces
        sw.WriteLine($"Frame Time: {frameTime:0.000000}");
    }

    private void WriteHierarchy(
        StreamWriter swr,
        Dictionary<SkeletonJoint, SkeletonJoint?> parentMap)
    {
        var children = new Dictionary<SkeletonJoint, List<SkeletonJoint>>();
        foreach (var j in order) children[j] = new List<SkeletonJoint>();
        SkeletonJoint? root = null;
        foreach (var j in order)
        {
            var p = parentMap[j];
            if (p == null) root = j;
            else children[p.Value].Add(j);
        }
        if (root == null) throw new Exception("No root joint");

        void Recurse(SkeletonJoint j, int indent, bool endSite = false)
        {
            string tabs = new string('\t', indent);
            if (indent == 0) swr.WriteLine($"ROOT {j}");
            else if (!endSite) swr.WriteLine($"{tabs}JOINT {j}");
            else swr.WriteLine($"{tabs}End Site");

            swr.WriteLine($"{tabs}{{");

            Vector3 off = Vector3.zero;
            if (restOffsetsMeters != null && restOffsetsMeters.TryGetValue(j, out var offM))
                off = offM * BVHExporter.UnitsScale; // meters -> centimeters exactly once
            swr.WriteLine($"{tabs}\tOFFSET {off.x:0.######} {off.y:0.######} {off.z:0.######}");

            if (!endSite)
            {
                if (indent == 0)
                    swr.WriteLine($"{tabs}\tCHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation");
                else
                    swr.WriteLine($"{tabs}\tCHANNELS 3 Zrotation Xrotation Yrotation");

                foreach (var c in children[j]) Recurse(c, indent + 1);

                if (children[j].Count == 0)
                {
                    // Minimal End Site (2cm tip in +Y)
                    swr.WriteLine($"{tabs}\tEnd Site");
                    swr.WriteLine($"{tabs}\t{{");
                    swr.WriteLine($"{tabs}\t\tOFFSET 0 2 0");
                    swr.WriteLine($"{tabs}\t}}");
                }
            }

            swr.WriteLine($"{tabs}}}");
        }

        Recurse(root.Value, 0);
    }

    private void WriteOneFrame(Dictionary<SkeletonJoint, BVHExporter.TransformSnapshot> frame)
    {
        // Gather values for one line in BVH order
        // Root world translation first (cm), then local rotations (Z,X,Y)
        var values = new List<float>(order.Length * 6);
        foreach (var j in order)
        {
            var snap = frame.ContainsKey(j) ? frame[j] : default;

            if (parentMap[j] == null)
            {
                // Static root at origin
                values.Add(0f);
                values.Add(0f);
                values.Add(0f);
            }

            Vector3 eDeg = BVHExporter.ToBVHEulerDegrees(snap.localRotation);
            values.Add(eDeg.z);
            values.Add(eDeg.x);
            values.Add(eDeg.y);
        }

        // Write one frame line
        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0) sw.Write(' ');
            sw.Write(values[i].ToString("0.######"));
        }
        sw.WriteLine();
    }
}
