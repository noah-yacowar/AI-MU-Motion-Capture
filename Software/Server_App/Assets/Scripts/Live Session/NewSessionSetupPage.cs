using UnityEngine;
using UnityEngine.UI;

public class NewSessionSetupPage : Page
{
    public GameObject newSessionPage;

    public Button startSessionButton;

    public override void Start()
    {
        base.Start();

        startSessionButton.onClick.AddListener(StartSession);
    }

    public void StartSession()
    {
        SessionManager.Instance.StartSession();
        goToNextPage(newSessionPage);
    }
}
