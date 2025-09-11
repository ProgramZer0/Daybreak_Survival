using System;
using UnityEngine;
using UnityEngine.UI;

public class MainGUIController : MonoBehaviour
{

    [SerializeField] GameObject[] guis;

    [SerializeField] private GameObject HPBar;
    [SerializeField] private GameObject PauseMenu;
    [SerializeField] private GameObject settingMenuMM;
    [SerializeField] private GameObject settingMenuPM;
    [SerializeField] private GameObject MainMenu;

    [SerializeField] private Weapon empty;
    [SerializeField] private PlayerInterface player;
    [SerializeField] private GameManager GM;

    private bool inUI = false;
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

    public void NoShowMM()
    {
        inUI = false;
        player.LockMovement(false);
        MainMenu.SetActive(false);
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

    public void OpenPause()
    {
        inUI = true;
        player.LockMovement(true);
        PauseMenu.SetActive(true);
        Time.timeScale = 0f;
    }
    public void OpenSettingsMenuPM()
    {
        inUI = true;
        player.LockMovement(true);
        settingMenuPM.SetActive(true);
        Time.timeScale = 0f;
    }
    public void OpenSettingsMenuMM()
    {
        inUI = true;
        player.LockMovement(true);
        settingMenuMM.SetActive(true);
        Time.timeScale = 0f;
    }
    public void OpenMainMenu()
    {
        inUI = true;
        player.LockMovement(true);
        MainMenu.SetActive(true);
        GM.ResetGame();
    }
}
