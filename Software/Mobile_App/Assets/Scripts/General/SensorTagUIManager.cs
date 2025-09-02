using UnityEngine;
using TMPro;

public class ParticipantTagUIManager : MonoBehaviour
{
    public TMP_Text nameTag;

    public void SetNameTag(string name)
    {
        nameTag.text = name;
    }
}
