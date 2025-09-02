using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class SetupSensorPage : MonoBehaviour
{
    public RawImage rawImage; // UI element to display camera
    private WebCamTexture camTexture;

    public Transform sensorTagArea;
    public RectTransform exampleSensorTagRect;
    public GameObject sensorTagPrefab;

    private Dictionary<string, GameObject> tagDict = new Dictionary<string, GameObject>();

    public int spacing = 10;

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
        string sensorInfo = QRCodeScanner.TryDecode(camTexture);
        if (!string.IsNullOrEmpty(sensorInfo))
        {
            //The QR code encodes the info as "TYPE:NAME"
            string[] parts = sensorInfo.Split(':');
            string type = parts[0];
            string name = parts[1];


            AddNewSensorTag(type, name);
            SensorsManager.Instance.AddNewSensor(type, name);
            //camTexture.Stop();
        }
    }

    public void AddNewSensorTag(string type, string name)
    {
        if (tagDict.ContainsKey(type)) 
        {
            return;
        }

        GameObject newUserTag = Instantiate(sensorTagPrefab, sensorTagArea);
        RectTransform newTagRect = newUserTag.GetComponent<RectTransform>();
        newUserTag.GetComponent<ParticipantTagUIManager>().SetNameTag(name);

        // Match size, anchors, pivot, etc.
        newTagRect.anchorMin = exampleSensorTagRect.anchorMin;
        newTagRect.anchorMax = exampleSensorTagRect.anchorMax;
        newTagRect.pivot = exampleSensorTagRect.pivot;
        newTagRect.anchoredPosition = exampleSensorTagRect.anchoredPosition;
        newTagRect.sizeDelta = exampleSensorTagRect.sizeDelta;
        newTagRect.localScale = exampleSensorTagRect.localScale;

        // Adjust Y size
        Vector2 newPos = exampleSensorTagRect.anchoredPosition;
        float tagHeight = newTagRect.rect.height;   // actual on-screen height
        float relLoc = (tagHeight + spacing) * tagDict.Count;
        newTagRect.anchoredPosition = new Vector2(newPos.x, newPos.y - relLoc);

        tagDict[type] = newUserTag;
    }

    public void ChangeTagName(string type, string newName)
    {
        tagDict[type].GetComponent<ParticipantTagUIManager>().SetNameTag(newName);
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