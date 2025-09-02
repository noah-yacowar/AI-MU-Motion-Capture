using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ParticipantJoinListUIManager : MonoBehaviour
{
    public static ParticipantJoinListUIManager Instance { get; private set; }

    public Transform participantTagArea;
    public RectTransform exampleParticipantTagRect; //Used to reference scaling
    public GameObject participantTagPrefab;

    private Dictionary<int, GameObject> tagDict = new Dictionary<int, GameObject>();

    public int spacing = 10;

    private void Awake()
    {
        Instance = this;
    }

    public void AddNewUserTag(int userId)
    {
        GameObject newUserTag = Instantiate(participantTagPrefab, participantTagArea);
        RectTransform newTagRect = newUserTag.GetComponent<RectTransform>();

        // Match size, anchors, pivot, etc.
        newTagRect.anchorMin = exampleParticipantTagRect.anchorMin;
        newTagRect.anchorMax = exampleParticipantTagRect.anchorMax;
        newTagRect.pivot = exampleParticipantTagRect.pivot;
        newTagRect.anchoredPosition = exampleParticipantTagRect.anchoredPosition;
        newTagRect.sizeDelta = exampleParticipantTagRect.sizeDelta;
        newTagRect.localScale = exampleParticipantTagRect.localScale;

        // Adjust Y size
        Vector2 newPos = exampleParticipantTagRect.anchoredPosition;
        float tagHeight = newTagRect.sizeDelta.y;
        float relLoc = (tagHeight + spacing) * tagDict.Count;
        newTagRect.anchoredPosition = new Vector2(newPos.x, newPos.y - relLoc);

        tagDict[userId] = newUserTag;
    }

    public void ChangeTagName(int userId, string newName)
    {
        tagDict[userId].GetComponent<ParticipantTagUIManager>().SetNameTag(newName);
    }
}
