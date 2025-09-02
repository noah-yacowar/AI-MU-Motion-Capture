using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnterAgePage : MonoBehaviour
{
    public TMP_InputField ageField;

    private void Start()
    {
        ageField.onEndEdit.AddListener(SendAgeToServer);
    }

    private void SendAgeToServer(string ageString)
    {
        ServerConnectionManager.Instance.SendServerMessage(GeneralMessageType.AGE_ENETERED, ageString);
        Debug.Log($"Sent age to server: {ageString}");
        PageManager.Instance.GoToNextPage();

    }
}
