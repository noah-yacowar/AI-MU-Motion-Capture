using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ParticipantIMUReceiver
{
    public SkeletonManager skeletonManager;          // Object to apply rotation
    UdpClient udpClient;
    Thread receiveThread;
    private Dictionary<SkeletonJoint, IMUData> imuData;

    private Dictionary<SkeletonJoint, Quaternion> calibrationOffsets;

    public YawOrientation hipMountOrientation = YawOrientation.Left;

    private HashSet<SkeletonJoint> jointsThisCycle;
    private object updateLock = new object();

    public ParticipantIMUReceiver(SkeletonManager skeletonManager, Button calibrateButton)
    {
        this.skeletonManager = skeletonManager;
        calibrateButton.onClick.AddListener(CalibrateIMUs);

        imuData = new Dictionary<SkeletonJoint, IMUData>();
        jointsThisCycle = new HashSet<SkeletonJoint>();

        // Add hips (with actual mounting orientation)
        AddNewIMU(SkeletonJoint.HIP, YawOrientation.Back, 90);
        AddNewIMU(SkeletonJoint.WAIST, YawOrientation.Back, 90);
        AddNewIMU(SkeletonJoint.CHEST, YawOrientation.Back, 0);
        AddNewIMU(SkeletonJoint.RIGHT_SHOULDER, YawOrientation.Right, 90);

        InitializeLegsToDefault();
    }

    public void InitializeSkeleton(float height_m)
    {
        skeletonManager.DrawSkeleton(height_m);
    }

    public void InitializeConnection(int port)
    {
        udpClient = new UdpClient(port);
        receiveThread = new Thread(()=>ReceiveData(port));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void InitializeLegsToDefault()
    {
        // Left leg joints (assuming default orientation)
        AddNewIMU(SkeletonJoint.LEFT_HIP, YawOrientation.Front, 0);
        AddNewIMU(SkeletonJoint.LEFT_KNEE, YawOrientation.Front, 0);
        AddNewIMU(SkeletonJoint.LEFT_ANKLE, YawOrientation.Front, 0);
        AddNewIMU(SkeletonJoint.LEFT_FOOT, YawOrientation.Front, 0);

        // Right leg joints (assuming default orientation)
        AddNewIMU(SkeletonJoint.RIGHT_HIP, YawOrientation.Front, 0);
        AddNewIMU(SkeletonJoint.RIGHT_KNEE, YawOrientation.Front, 0);
        AddNewIMU(SkeletonJoint.RIGHT_ANKLE, YawOrientation.Front, 0);
        AddNewIMU(SkeletonJoint.RIGHT_FOOT, YawOrientation.Front, 0);
    }

    
    public void CalibrateIMUs()
    {
        // Loop over each joint to calibrate
        foreach (var joint in imuData.Keys)
        {
            IMUData data = imuData[joint];
            Quaternion bodyOffsetWithError = data.rawRotation * data.offset.mountOrientationOffset;

            Vector3 eulerAngles = bodyOffsetWithError.eulerAngles;
            float xOff = NormalizeAngle(eulerAngles.x); // Around X axis
            float zOff = NormalizeAngle(eulerAngles.z); // Around Z axis

            // Update the body mounting error offset with the new one
            data.offset.bodyMountingErrorOffset = Quaternion.Inverse(Quaternion.Euler(xOff, 0f, zOff));
            data.offset.calibrationOffset = Quaternion.Inverse(bodyOffsetWithError * data.offset.bodyMountingErrorOffset);
            data.calibrated = true;
        }
    }
    

    /*
    public void CalibrateIMUs()
    {
        // --- Step 1: find hip yaw baseline ---
        if (!imuData.ContainsKey(SkeletonJoint.HIP))
        {
            Debug.LogWarning("No HIP IMU found for yaw calibration baseline!");
            return;
        }

        IMUData hipData = imuData[SkeletonJoint.HIP];
        Quaternion hipQ = hipData.rawRotation * hipData.offset.mountOrientationOffset;

        // Extract yaw-only quaternion from hip
        Vector3 hipForward = hipQ * Vector3.forward;
        hipForward.y = 0f; // project onto XZ plane
        Quaternion hipYawOnly = Quaternion.LookRotation(hipForward.normalized, Vector3.up);

        // --- Step 2: calibrate each joint relative to hip yaw ---
        foreach (var joint in imuData.Keys)
        {
            IMUData data = imuData[joint];
            Quaternion bodyOffsetWithError = data.rawRotation * data.offset.mountOrientationOffset;

            // --- Pitch + Roll correction (your original method) ---
            Vector3 eulerAngles = bodyOffsetWithError.eulerAngles;
            float xOff = NormalizeAngle(eulerAngles.x); // pitch
            float zOff = NormalizeAngle(eulerAngles.z); // roll

            // Pitch/roll error quaternion
            Quaternion prError = Quaternion.Euler(xOff, 0f, zOff);
            data.offset.bodyMountingErrorOffset = Quaternion.Inverse(prError);

            // Apply pitch/roll correction
            Quaternion corrected = bodyOffsetWithError * data.offset.bodyMountingErrorOffset;

            // --- Yaw correction relative to hip (quaternion method) ---
            Vector3 jointForward = corrected * Vector3.forward;
            jointForward.y = 0f;
            Quaternion jointYawOnly = Quaternion.LookRotation(jointForward.normalized, Vector3.up);

            // Yaw error = difference between hip yaw and joint yaw
            Quaternion yawError = Quaternion.Inverse(jointYawOnly) * hipYawOnly;

            // Final calibration offset includes pitch/roll and yaw correction
            data.offset.calibrationOffset = Quaternion.Inverse(corrected * yawError);

            data.calibrated = true;
        }
    }
    */

    float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    private void AddNewIMU(SkeletonJoint joint, YawOrientation yaw, int roll)
    {
        if (!imuData.ContainsKey(joint))
        {
            int yawDeg = GetYawDegrees(yaw);
            imuData[joint] = new IMUData(Quaternion.identity, new IMUOrientationOffset(yawDeg, roll, Quaternion.identity));
        }
    }

    void ReceiveData(int port)
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                // Expecting message like: LEFT_KNEE:0.1,0.2,0.3,0.9
                if (message.Contains(":"))
                {
                    string[] split = message.Split(':');
                    string jointName = split[0].Trim();
                    string[] parts = split[1].Split(',');

                    if (parts.Length == 4 && Enum.TryParse(jointName, out SkeletonJoint joint))
                    {
                        float x = float.Parse(parts[0]);
                        float y = float.Parse(parts[1]);
                        float z = float.Parse(parts[2]);
                        float w = float.Parse(parts[3]);

                        Quaternion rawRotation = new Quaternion(x, y, z, w);
                        //Debug.Log("Angles: " + rawRotation.eulerAngles);

                        lock (this)
                        {
                            IMUData jointData = imuData[joint];
                            jointData.rawRotation = Quaternion.Normalize(rawRotation);
                            jointData.rotation = Quaternion.Normalize(
                                rawRotation *
                                jointData.offset.mountOrientationOffset *
                                jointData.offset.bodyMountingErrorOffset
                            );

                            jointsThisCycle.Add(joint);

                            // Check if all joints are updated
                            if (jointsThisCycle.Count == 1) //Find better way to check this, ie. cannot use imuData.Count since it contains joints not necessarily set
                            {
                                jointsThisCycle.Clear(); // Reset for the next cycle
                                UpdateParticipantSkeleton();
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid message format or joint name: {message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Message missing joint prefix: {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("UDP receive error: " + ex.Message);
            }
        }
    }


    void UpdateParticipantSkeleton()
    {
        Dictionary<SkeletonJoint, Quaternion> correctedRotations = new();

        lock (this)
        {
            foreach (var kvp in imuData)
            {
                IMUData jointData = kvp.Value;
                SkeletonJoint joint = kvp.Key;
                Quaternion rotation = jointData.rotation;
                Quaternion correctedRotation = 
                    jointData.offset.calibrationOffset * 
                    rotation;
                correctedRotations[joint] = correctedRotation;
            }

        }

        MainThreadDispatcher.Instance.DispatchAction(() => skeletonManager.ApplyIMURotations(correctedRotations));
    }


    private int GetYawDegrees(YawOrientation yaw)
    {
        return yaw switch
        {
            YawOrientation.Front => 0,
            YawOrientation.Left => 90,
            YawOrientation.Back => 180,
            YawOrientation.Right => 270,
            _ => 0
        };
    }

    public void StopReceiving()
    {
        try
        {
            // Close the UDP client so Receive() unblocks
            udpClient?.Close();

            // Abort the thread if still running
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Abort();
                receiveThread = null;
            }

            Debug.Log("Receiver thread stopped.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error stopping receiver thread: " + ex.Message);
        }
    }
}

public class IMUData
{
    public Quaternion rotation;
    public Quaternion rawRotation;

    public IMUOrientationOffset offset;
    public bool calibrated = false;

    public IMUData(Quaternion rotation, IMUOrientationOffset offset)
    {
        this.rawRotation = rotation;
        this.rotation = rotation;
        this.offset = offset;
    }

    public void SetCalibrationState(bool calibrationState)
    {
        this.calibrated = calibrationState;
    }
}


public class IMUOrientationOffset
{
    public Quaternion mountOrientationOffset;
    public Quaternion bodyMountingErrorOffset;
    public Quaternion calibrationOffset;

    public IMUOrientationOffset(int yaw, int roll, Quaternion calibrationOffset)
    {
        this.calibrationOffset = calibrationOffset;
        this.bodyMountingErrorOffset = Quaternion.identity;  // Default to no imperfection

        Quaternion rollRot = Quaternion.Euler(0f, 0f, roll);
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        mountOrientationOffset = rollRot*yawRot;
    }
}

public enum YawOrientation
{
    Front,  // 0�
    Back,   // 180�
    Left,   // 270�
    Right   // 90�
}

