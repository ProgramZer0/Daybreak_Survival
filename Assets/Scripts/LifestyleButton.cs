using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LifestyleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public LifeStyles lifeStyle;
    [SerializeField] private Button button;
    
    private LifeStyleController controller;
    private LifestyleGUI GUI;
    private GameObject toolTip;
    private bool showTip = false;
    private bool isActive = false;
    private bool useUpdate = false;

    private void Update()
    {
        if (!useUpdate) return;

        if (!isActive)
            button.enabled = !controller.CheckIfFull();
        else
            button.enabled = true;

        if (showTip) ShowToolTip();
    }

    public void Initilize(LifeStyleController con, LifestyleGUI gui)
    {
        controller = con;
        GUI = gui;
        gameObject.GetComponentInChildren<Image>().sprite = lifeStyle.displaySpirte;
        useUpdate = true;
    }

    public void ButtonPressed()
    {
        if (!isActive)
        {
            if (controller.MakeLifestylesActive(lifeStyle))
                isActive = true;
        }
        else
        {
            controller.DeActiveLifestyles(lifeStyle);
            isActive = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(toolTip == null)
        {
            toolTip = Instantiate(controller.toolTipPreFab, GUI.gameObject.transform);
            toolTip.transform.SetAsLastSibling();
            TextMeshProUGUI[] text = toolTip.GetComponentsInChildren<TextMeshProUGUI>();
            if (text[0] != null)
                text[0].text = lifeStyle.description;
            if (text.Length == 2)
                text[1].text = lifeStyle.name;
        }

        showTip = true;
    }

    private void ShowToolTip()
    { 
        if(toolTip != null)
            toolTip.transform.position = Input.mousePosition;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        showTip = false;
        HideToolTip();
    }

    private void HideToolTip()
    {
        if(toolTip != null)
            Destroy(toolTip);
    }
}
