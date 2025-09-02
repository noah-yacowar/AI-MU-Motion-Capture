using UnityEngine;
using UnityEngine.UI;

public class ScanLineOverImage : MonoBehaviour
{
    public RectTransform scanLineImageRect;
    public RectTransform QRCodeRect;
    public float speed = 20f;

    private float dir = -1f;

    void Update()
    {
        // Dynamically calculate scan bounds
        float qrHeight = QRCodeRect.rect.height;
        float centerY = QRCodeRect.anchoredPosition.y;
        float yMin = centerY - qrHeight / 2f;
        float yMax = centerY + qrHeight / 2f;

        float curY = scanLineImageRect.anchoredPosition.y;
        curY += dir * speed * Time.deltaTime;

        if (curY <= yMin)
        {
            curY = yMin;
            dir = 1f;
        }
        else if (curY >= yMax)
        {
            curY = yMax;
            dir = -1f;
        }

        SetBarY(curY);
    }

    void SetBarY(float y)
    {
        Vector2 pos = scanLineImageRect.anchoredPosition;
        pos.y = y;
        scanLineImageRect.anchoredPosition = pos;
    }
}
