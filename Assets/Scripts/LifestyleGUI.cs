using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LifestyleGUI : MonoBehaviour
{
    [SerializeField] private GameObject PagePreFab;
    [SerializeField] private GameObject lifeStylePrefab;
    [SerializeField] private LifeStyleController controller;
    [SerializeField] private int maxLSPerPage = 20;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject previousButton;

    public int displayingPage = 0;

    private List<GameObject> pages;
    private int currentPage = 0;
    private int currentLSonPage = 0;
    
    private void Start()
    {
        pages = new List<GameObject>();
    }

    public void LoadLifeStyles()
    {

        AddPage(false);
        foreach(LifeStyles ls in controller.lifeStylesAvailable)
        {
            if(currentLSonPage >= maxLSPerPage)
                AddPage(true);

            GameObject o =  Instantiate(lifeStylePrefab, pages[currentPage].transform);
            o.transform.SetAsLastSibling();
            o.GetComponent<LifestyleButton>().lifeStyle = ls;
            o.GetComponent<LifestyleButton>().Initilize(controller, this);
            currentLSonPage++;
        }

        DisplayPage(0);
    }

    private void AddPage(bool increment = true)
    {
        if(pages.Count != 0)
            pages[currentPage].SetActive(false);
        currentLSonPage = 0;
        GameObject o = Instantiate(PagePreFab, transform);
        o.transform.SetAsLastSibling();
        pages.Add(o);
        if (increment)
            currentPage++;
    }

    public void DisplayPage(int i)
    {
        if (pages.Count <= i) return;

        HideAllPages();

        pages[i].SetActive(true);
        displayingPage = i;
        CheckPageButtons();
    }
    private void HideAllPages()
    {
        foreach (GameObject obj in pages)
            obj.SetActive(false);
    } 

    public void DeleteAllPages()
    {
        foreach (GameObject obj in pages)
            Destroy(obj);
    }

    public void DisplayNextPage()
    {
        if (displayingPage + 1 > pages.Count) return;

        DisplayPage(displayingPage + 1);
    }

    public void DisplayPreviousPage()
    {
        if (displayingPage <= 0) return;

        DisplayPage(displayingPage - 1);
    }

    private void CheckPageButtons()
    {
        if (displayingPage <= 0) previousButton.SetActive(false);
        else previousButton.SetActive(true);

        if (displayingPage + 1 > pages.Count) nextButton.SetActive(false);
        else nextButton.SetActive(true);
    }
}
