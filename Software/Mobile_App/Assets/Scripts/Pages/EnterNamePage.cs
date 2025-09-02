using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnterNamePage : MonoBehaviour
{
    public TMP_InputField nameField;

    private void Start()
    {
        nameField.onEndEdit.AddListener(SendNameToServer);
    }

    private void SendNameToServer(string name)
    {
        ServerConnectionManager.Instance.SendServerMessage(GeneralMessageType.NAME_ENTERED, name);
        Debug.Log($"Sent name to server: {name}");
        PageManager.Instance.GoToNextPage();

    }
}
