using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LifestyleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public LifeStyles lifeStyle;
    [SerializeField] private Button button;
    public Color highlightedColor;
    
    private LifeStyleController controller;
    private LifestyleGUI GUI;
    private GameObject toolTip;
    private Image image;
    private Color ImageColor;
    private bool isActive = false;
    private bool useUpdate = false;

    private void Update()
    {
        
        if (!useUpdate) return;

        if (!isActive)
        {
            image.color = ImageColor;
            button.enabled = !controller.CheckIfFull();
        }
        else
        {
            image.color = highlightedColor;
            button.enabled = true;
        }
    }

    public void Initilize(LifeStyleController con, LifestyleGUI gui, LifeStyles ls, bool isAct)
    {
        controller = con;
        GUI = gui;
        lifeStyle = ls;
        image = gameObject.GetComponentInChildren<Image>();
        image.sprite = lifeStyle.displaySpirte;
        ImageColor = image.color;
        isActive = isAct;
        useUpdate = true;
    }

    public void ButtonPressed()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
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
            toolTip = Instantiate(controller.toolTipPreFab, GUI.transform);
            toolTip.transform.position = transform.position + transform.localScale;
            toolTip.GetComponentInChildren<Image>().raycastTarget = false;
            toolTip.GetComponentInChildren<TextMeshProUGUI>().raycastTarget = false;
            toolTip.transform.SetAsLastSibling();
            TextMeshProUGUI[] text = toolTip.GetComponentsInChildren<TextMeshProUGUI>();
            if (text[0] != null)
                text[0].text = lifeStyle.description;
            if (text.Length == 2)
                text[1].text = lifeStyle.name;
        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideToolTip();
    }

    private void HideToolTip()
    {
        if(toolTip != null)
            Destroy(toolTip);
    }
}
