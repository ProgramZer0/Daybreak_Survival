using UnityEngine;

public class ButtonMethods : MonoBehaviour
{
    [SerializeField] private MainGUIController GUI;
    [SerializeField] private GameManager GM;

    public void ResumeGame_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.ShowNoGUI();
    }

    public void BackMainMenu_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.ShowNoGUI();
        GUI.OpenMainMenu();
        GM.MainMenu();
    }

    public void SettingsFromPause_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.ShowNoGUI();
        GUI.OpenSettingsMenuPM();
    }
    public void SettingsFromMenu_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.ShowNoMM();
        GUI.OpenSettingsMenuMM();
    }

    public void PauseMenu_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.ShowNoGUI();
        GUI.OpenPause();
    }
    public void QuestView_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.ShowNoGUI();
        GUI.OpenQuestView();
    }
    public void StartGame_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.ShowNoGUI();
        GUI.ShowNoMM();
        GUI.ShowAllHUDs();
        GUI.SetHUDVals();
        GM.StartGame();
    }

    public void QuitGame_B()
    {
        Application.Quit(0);
    }
}
