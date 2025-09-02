using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParticipantUIManager : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hrText;
    public Image exertion_bar;

    public void SetName(string name)
    {
        nameText.text = name;
    }

    public void SetHR(int hr)
    {
        hrText.text = $"BPM: {hr.ToString()}";
    }

    public void SetExertion(float percent)
    {
        float dec = Mathf.Clamp01(percent / 100f);
        exertion_bar.fillAmount = dec;
    }
}
