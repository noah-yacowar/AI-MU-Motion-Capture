using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PageManager : MonoBehaviour
{
    public static PageManager Instance { get; private set; }

    public List<GameObject> pages;
    public GameObject sessionInProgressPage;
    public GameObject sessionCompletePage;
    private int curPageIndex = 0;

    public void GoToNextPage()
    {
        if(curPageIndex == pages.Count - 1)
        {
            return;
        }

        pages[curPageIndex].SetActive(false);
        pages[curPageIndex + 1].SetActive(true);

        curPageIndex++;
    }

    public void GoToPreviousPage()
    {
        if (curPageIndex == 0)
        {
            return;
        }

        pages[curPageIndex].SetActive(false);
        pages[curPageIndex - 1].SetActive(true);

        curPageIndex--;
    }

    public void GoToSessionPage()
    {
        pages[curPageIndex].SetActive(false);
        sessionInProgressPage.SetActive(true);

        curPageIndex = pages.Count;
    }

    public void GoToSessionCompletePage() 
    {
        sessionInProgressPage.SetActive(false);
        sessionCompletePage.SetActive(true);

        curPageIndex = pages.Count+1;
    }

    private void Awake()
    {
        Instance = this;
    }
}
