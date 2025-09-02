using UnityEngine;
using ZXing;
using ZXing.QrCode;
using UnityEngine.UI;
using TMPro;

public class GenerateQR : MonoBehaviour
{
    [SerializeField] private RawImage qrImage;
    private Texture2D qrTexture;
    private string ip;
    private string port;

    private void Start()
    {
        qrTexture = new Texture2D(256, 256);
        DrawIP();
    }

    private void DrawIP()
    {
        ip = NetworkInfo.GetLocalIPAddress();
        Debug.Log("IP: " + ip);
        port = NetworkInfo.defaultPort.ToString();

        string addressToWrite = ip + ":" + port;

        Color32[] qrPixels = Encode(addressToWrite, qrTexture.width, qrTexture.height);
        qrTexture.SetPixels32(qrPixels);
        qrTexture.Apply();

        qrImage.texture = qrTexture;
    }

    private Color32[] Encode(string encodeText, int width, int height)
    {
        BarcodeWriter writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width,
            }
        };

        return writer.Write(encodeText);
    }

}