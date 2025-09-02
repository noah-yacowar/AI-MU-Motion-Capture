using UnityEngine;
using System.Collections.Generic;
using System;

public class SensorsManager : MonoBehaviour
{
    public static SensorsManager Instance { get; private set; }

    public event System.Action<int> HeartRateReceived;

    private Dictionary<SensorType, Sensor> sensorsDict = new Dictionary<SensorType, Sensor>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instance = this;

        InitializeBLE();
    }

    private void InitializeBLE()
    {
        Debug.Log("Initializing Bluetooth...");
        BluetoothLEHardwareInterface.Initialize(
            true, false,
            () => {
                Debug.Log("BLE initialized.");
                // Wait for user input or other event before calling ScanForDevices()
            },
            (error) => {
                Debug.LogError("BLE init error: " + error);
            }
        );
    }

    public void AddNewSensor(string type, string moreInfo)
    {
        if (Enum.TryParse(type, out SensorType sensorType))
        {
            if (sensorsDict.ContainsKey(sensorType)) return;

            switch (sensorType)
            {
                case SensorType.IMU:
                    string serverPort = moreInfo;
                    ServerConnectionManager.Instance.SendServerMessage(GeneralMessageType.IMU_CONFIG, serverPort);
                    sensorsDict[SensorType.IMU] = new Sensor(serverPort); //Just pass in more info for name, sortof an id
                    break;
                case SensorType.CAMERA: 
                    break;
                case SensorType.HR:
                    string sensorID = moreInfo;
                    HeartRateSensor hrSensor = new HeartRateSensor(sensorID);
                    hrSensor.HeartRateReceived += (hr) => this.HeartRateReceived.Invoke(hr);
                    sensorsDict[SensorType.HR] = hrSensor;
                    hrSensor.ConnectToSensor();
                    break;
            }
            Debug.Log($"New {type} Sensor:");
        }
    }

    public bool FindIfSensorConnected(SensorType type)
    {
        return sensorsDict.ContainsKey(type);
    }
}

public enum SensorType
{
    IMU,
    CAMERA,
    HR,
}
