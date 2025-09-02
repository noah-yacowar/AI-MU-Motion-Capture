using UnityEngine;

public class BLESensor : Sensor
{
    [SerializeField] private int scanTimeSec = 10;
    protected string connectedAddress = null;

    public BLESensor(string name) : base(name) { }

    // Public entry point
    public override void ConnectToSensor()
    {
        Debug.Log($"Connecting to {name}");
        ScanForDevices();
    }

    // Scan for nearby BLE devices
    private void ScanForDevices()
    {
        Debug.Log("Scanning for BLE devices...");
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null,
            (address, name) => {
                if (!string.IsNullOrEmpty(name) && name.Contains(this.name))
                {
                    Debug.Log($"Matched target sensor: {name}");
                    BluetoothLEHardwareInterface.StopScan();
                    ConnectToDevice(address);
                }
            },
            null);
    }

    // Connect to selected BLE device
    private void ConnectToDevice(string address)
    {
        connectedAddress = address;

        BluetoothLEHardwareInterface.ConnectToPeripheral(
            address,
            null,  // onConnected (optional)
            null,  // onServiceDiscovered (optional)
            (address, serviceUUID, characteristicUUID) => {
                Debug.Log($"Connected to {address}. Service: {serviceUUID}, Characteristic: {characteristicUUID}");
                OnConnected(true);
            }
        );
    }

    // Override for subclasses to handle connection success/failure
    protected virtual void OnConnected(bool success)
    {
        Debug.Log("Sensor connection result: " + success);
    }
}
