using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnterHeightPage : MonoBehaviour
{
    public TMP_InputField heightField;

    private void Start()
    {
        heightField.onEndEdit.AddListener(SendHeightToServer);
    }

    private void SendHeightToServer(string heightString)
    {
        ServerConnectionManager.Instance.SendServerMessage(GeneralMessageType.HEIGHT_ENTERED, heightString);
        Debug.Log($"Sent height to server: {heightString}");
        PageManager.Instance.GoToNextPage();

    }
}
