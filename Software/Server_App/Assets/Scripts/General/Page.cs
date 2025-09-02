using UnityEngine;

public class Page : MonoBehaviour
{
    public GameObject thisPage;

    public virtual void Start()
    {
        thisPage = this.gameObject;
    }

    public void goToNextPage(GameObject nextPage)
    {
        thisPage.SetActive(false);
        thisPage.transform.parent.gameObject.SetActive(false);
        nextPage.SetActive(true);
        nextPage.transform.parent.gameObject.SetActive(true);
    }
}
