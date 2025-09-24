using System;
using UnityEngine;
using UnityEngine.UI;

public class MainGUIController : MonoBehaviour
{

    [SerializeField] private GameObject[] guis;
    [SerializeField] private GameObject[] huds;

    [SerializeField] private GameObject HPBar;
    [SerializeField] private GameObject dashIndicator;
    [SerializeField] private GameObject weaponIndicator;
    [SerializeField] private GameObject pickupIndicator;
    [SerializeField] private GameObject miniMap;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject questView;
    [SerializeField] private GameObject settingMenuMM;
    [SerializeField] private GameObject settingMenuPM;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject lifestyleGUI;

    [SerializeField] private Weapon empty;
    [SerializeField] private PlayerInterface player;
    [SerializeField] private GameManager GM;

    private bool inUI = true;
    private bool escapePressed = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) escapePressed = true;

        if (escapePressed)
        {
            if (inUI)
                ShowNoGUI();
            else
                OpenPause();

            escapePressed = false;
        }

        if (!inUI)
            ShowAllHUDs();
    }

    public void ResetGUIData()
    {
        SetHUDVals();
    }

    public void ShowNoGUI()
    {
        foreach (GameObject obj in guis)
        {
            obj.SetActive(false);
        }
        SetInUI(false);
        Time.timeScale = 1f;
    }
    
    public void ShowNoHUDs()
    {
        foreach (GameObject obj in huds)
        {
            obj.SetActive(false);
        }
        dashIndicator.SetActive(false);
        pickupIndicator.SetActive(false);
        pickupIndicator.GetComponent<PickupIconScript>().stopPickup();  
    }
    public void ShowAllHUDs()
    {
        foreach (GameObject obj in huds)
        {
            obj.SetActive(true);
        }
    }

    public void SetHUDVals()
    {
        FindFirstObjectByType<HPBar>().SetMaxHP(player.GetMaxHP());
        FindFirstObjectByType<HPBar>().SetHP(player.GetCurrentHP());
    }

    public void ShowNoMM()
    {
        inUI = false;
        player.LockMovement(false);
        mainMenu.SetActive(false);
    }
    public void ShowNoLifestyle()
    {
        inUI = false;
        player.LockMovement(false);

        lifestyleGUI.SetActive(false);
    }
    public void OpenLifestyle()
    {
        setInUI();
        lifestyleGUI.SetActive(true);
    }
    public Weapon ReturnEmptyWeapon()
    {
        return empty;
    }
    public void StopSeeingPickup()
    {
        pickupIndicator.GetComponent<PickupIconScript>().stopPickup();
        pickupIndicator.SetActive(false);
    }
    public void SeePickup(float time)
    { 
        pickupIndicator.SetActive(true);
        pickupIndicator.GetComponent<PickupIconScript>().SetPickup(time);
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

    public void OpenQuestView()
    {
        setInUI();
        questView.SetActive(true);
    }
    public void OpenPause()
    {
        setInUI();
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
    }
    public void OpenSettingsMenuPM()
    {
        setInUI();
        settingMenuPM.SetActive(true);
        Time.timeScale = 0f;
    }
    public void OpenSettingsMenuMM()
    {
        setInUI();
        settingMenuMM.SetActive(true);
        Time.timeScale = 0f;
    }
    public void OpenMainMenu()
    {
        setInUI();
        mainMenu.SetActive(true);
        GM.MainMenu();
    }

    private void setInUI()
    {
        inUI = true;
        player.LockMovement(true);
        ShowNoHUDs();
    }
}
