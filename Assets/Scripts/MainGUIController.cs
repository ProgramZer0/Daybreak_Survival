using System;
using UnityEngine;
using UnityEngine.UI;

public class MainGUIController : MonoBehaviour
{

    [SerializeField] GameObject[] guis;

    [SerializeField] private GameObject HPBar;
    [SerializeField] private GameObject PauseMenu;
    [SerializeField] private GameObject MainMenu;

    [SerializeField] private Weapon empty;
    [SerializeField] private PlayerInterface player;

    private bool inUI = false;

    public void ShowNoGUI()
    {
        foreach (GameObject obj in guis)
        {
            obj.SetActive(false);
        }
        SetInUI(false);
    }   

    public Weapon ReturnEmptyWeapon()
    {
        return empty;
    }

    public void SetInUI(bool on)
    {
        player.LockMovement(on);
        inUI = on;
    }

    public bool GetInUI()
    {
        return inUI;
    }
}
