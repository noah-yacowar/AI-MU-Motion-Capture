using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NewSessionScreenPage : Page
{
    public GameObject finishedSessionPage;
    public GameObject popUp;  // The pop-up GameObject to display temporarily

    [SerializeField] private Button startButton;
    public Texture startTexture;
    public Texture recordingTexture;

    public float displayPopup_s = 2f;

    private bool recordingStarted = false;


    public override void Start()
    {
        base.Start();

        startButton.onClick.AddListener(RecordButtonPressed);
    }

    private void RecordButtonPressed()
    {
        
        if (recordingStarted)
        {
            SessionManager.Instance.EndSession();
            startButton.GetComponent<RawImage>().texture = startTexture;
            StartCoroutine(ShowPopUpAndGoToNextPage());
        }
        else
        {
            SessionManager.Instance.StartRecording();
            startButton.GetComponent<RawImage>().texture = recordingTexture;
        }

        recordingStarted = !recordingStarted;
    }

    private IEnumerator ShowPopUpAndGoToNextPage()
    {
        // Show the pop-up
        popUp.SetActive(true);

        // Wait for 2 seconds
        yield return new WaitForSeconds(displayPopup_s);

        // Hide the pop-up
        popUp.SetActive(false);

        // Transition to the next page
        goToNextPage(finishedSessionPage);
    }
    }
