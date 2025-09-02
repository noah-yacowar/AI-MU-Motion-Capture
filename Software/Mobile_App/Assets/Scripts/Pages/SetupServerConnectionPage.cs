using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class SetupServerConnectionPage : MonoBehaviour
{
    public RawImage rawImage; // UI element to display camera
    private WebCamTexture camTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            camTexture = new WebCamTexture(devices[0].name);
            rawImage.texture = camTexture;
            rawImage.material.mainTexture = camTexture;
            camTexture.Play();
        }
    }

    private void Update()
    {
        string serverAddress = QRCodeScanner.TryDecode(camTexture);
        if (!string.IsNullOrEmpty(serverAddress))
        {
            Debug.Log("Server address: " + serverAddress);

            //The QR code encodes the address as "IP:Port"
            string[] parts = serverAddress.Split(':');
            string ip = parts[0];
            int port = int.Parse(parts[1]);

            ServerConnectionManager.Instance.ConnectToServer(ip, port);

            PageManager.Instance.GoToNextPage();

            camTexture.Stop();
        }
    }

    private void OnEnable()
    {
        if (camTexture != null && !camTexture.isPlaying)
        {
            camTexture.Play();
        }
    }

    void OnDisable()
    {
        if (camTexture != null && camTexture.isPlaying)
            camTexture.Stop();
    }
}
