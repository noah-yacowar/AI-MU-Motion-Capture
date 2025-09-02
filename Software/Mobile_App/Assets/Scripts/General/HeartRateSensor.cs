using UnityEngine;

public class HeartRateSensor : BLESensor
{
    private string heartRateServiceUUID = "180D";
    private string heartRateCharacteristicUUID = "2A37";

    public event System.Action<int> HeartRateReceived;

    public HeartRateSensor(string name) : base(name) { }

    protected override void OnConnected(bool success)
    {
        if (!success)
        {
            Debug.LogError("Failed to connect to HR device.");
            return;
        }

        Debug.Log("Connected to HR device!");

        // Subscribe to the Heart Rate Measurement characteristic
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(
            connectedAddress,
            heartRateServiceUUID,
            heartRateCharacteristicUUID,
            null, // optional notification callback
            (deviceAddress, characteristicUUID, data) => {
                OnHeartRateReceived(data);
            }
        );
    }

    private void OnHeartRateReceived(byte[] data)
    {
        if (data.Length > 1)
        {
            int hr = (data[0] & 0x01) == 0 ? data[1] : (data[1] | (data[2] << 8));
            Debug.Log($"Heart Rate: {hr}");
            HeartRateReceived(hr);
            ServerConnectionManager.Instance.SendServerMessage(GeneralMessageType.HEART_RATE, hr);
        }
        else
        {
            Debug.LogWarning("Invalid HR data received.");
        }
    }
}
