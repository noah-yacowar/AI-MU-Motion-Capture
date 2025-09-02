using System;
using UnityEngine;
using ZXing;

public static class QRCodeScanner
{

    private static IBarcodeReader barcodeReader = new BarcodeReader
    {
        AutoRotate = false,
        Options = new ZXing.Common.DecodingOptions
        {
            TryHarder = false
        }
    };


    public static string TryDecode(WebCamTexture camTexture)
    {
        if (!camTexture.isPlaying || !camTexture.didUpdateThisFrame)
            return null;

        try
        {
            // Get the pixel data from the camera
            Color32[] cameraData = camTexture.GetPixels32();
            int width = camTexture.width;
            int height = camTexture.height;

            // Decode the camera image directly from the pixel array
            var result = barcodeReader.Decode(cameraData, width, height);
            return result?.Text;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("QR Decode failed: " + e.Message);
            return null;
        }
    }
}
