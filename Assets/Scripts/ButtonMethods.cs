using UnityEngine;

public class ButtonMethods : MonoBehaviour
{
    [SerializeField] private MainGUIController GUI;
    [SerializeField] private GameManager GM;

    public void ResumeGame_B()
    {
        GUI.ShowNoGUI();
    }

    public void BackMainMenu_B()
    {
        GUI.ShowNoGUI();
        GUI.OpenMainMenu();
    }

    public void SettingsFromPause_B()
    {
        GUI.ShowNoGUI();
        GUI.OpenSettingsMenuPM();
    }
    public void SettingsFromMenu_B()
    {
        GUI.ShowNoMM();
        GUI.OpenSettingsMenuMM();
    }

    public void PauseMenu_B()
    {
        GUI.ShowNoGUI();
        GUI.OpenPause();
    }
    public void QuestView_B()
    {
        GUI.ShowNoGUI();
        GUI.OpenQuestView();
    }
    public void StartGame_B()
    {
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
