using UnityEngine;
using UnityEngine.UI;

public class NewSessionFinishedPage : Page
{
    public GameObject homePage;

    public Button returnButton;

    public override void Start()
    {
        base.Start();

        returnButton.onClick.AddListener(() => goToNextPage(homePage));
    }
}
