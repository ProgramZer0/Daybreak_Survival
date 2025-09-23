using UnityEngine;
using UnityEngine.UI;

public class LifestyleGUI : MonoBehaviour
{
    [SerializeField] private LifeStyleController controller;

    private Image image;

    private void Start()
    {
        image = GetComponentInChildren<Image>();
    }

    public void ClickedOn()
    {

    }
    
}
