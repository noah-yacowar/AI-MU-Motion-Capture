using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecordBaselinePage : MonoBehaviour
{
    public Sprite startRecordingTexture;
    public Sprite recordingTexture;

    public GameObject recordButtonGameObject;
    private Button recordButton;
    private Image recordImage;

    private List<int> heartRateSamples = new List<int>();
    private bool isRecording = false;

    public float recordTime_s = 120f;

    void Start()
    {
        recordButton = recordButtonGameObject.GetComponent<Button>();
        recordImage = recordButtonGameObject.GetComponent<Image>();

        recordButton.onClick.AddListener(StartRecording);

        SensorsManager.Instance.HeartRateReceived += OnHeartRateReceived;
    }

    private void StartRecording()
    {
        if (!SensorsManager.Instance.FindIfSensorConnected(SensorType.HR)) return;

        if (isRecording) return; // Prevent double start

        recordImage.sprite = recordingTexture;
        heartRateSamples.Clear();
        isRecording = true;

        StartCoroutine(RecordForOneMinute());
    }

    private void OnHeartRateReceived(int hr)
    {
        if (isRecording)
        {
            heartRateSamples.Add(hr);
        }
    }

    private IEnumerator RecordForOneMinute()
    {
        yield return new WaitForSeconds(recordTime_s); 

        isRecording = false;
        recordImage.sprite = startRecordingTexture;

        if (heartRateSamples.Count > 0)
        {
            int sum = 0;
            foreach (int hr in heartRateSamples)
            {
                sum += hr;
            }

            float avgHR = sum / (float)heartRateSamples.Count;
            Debug.Log($"Resting HR ({recordTime_s/60f}-min avg): {avgHR}");

            ServerConnectionManager.Instance.SendServerMessage(GeneralMessageType.HR_BASELINE, avgHR);
            PageManager.Instance.GoToNextPage();
        }
        else
        {
            Debug.LogWarning($"No heart rate data recorded during the {recordTime_s / 60f}-minute window.");
        }
    }
}
