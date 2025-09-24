using UnityEngine;

public class ButtonMethods : MonoBehaviour
{
    [SerializeField] private MainGUIController GUI;
    [SerializeField] private GameManager GM;
    [SerializeField] private LifeStyleController LSC;
    [SerializeField] private LifestyleGUI LSGui;

    public void NextPage()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        LSGui.DisplayNextPage();
    }
    public void PreviousPage()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        LSGui.DisplayPreviousPage();
    }
    public void BackToMMFromLS()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        LSGui.DeleteAllPages();
        BackMainMenu_B();
    }
    public void OpenLifestyle_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        if (LSC.lifeStylesAvailable.Count == 0) return;
        GUI.ShowNoGUI();
        GUI.OpenLifestyle();
        LSGui.LoadLifeStyles();
    }
    public void ResumeGame_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GM.SaveGameData();
        GUI.ShowNoGUI();
    }

    public void BackMainMenu_B()
    {
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GM.SaveGameData();
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
        GM.SaveGameData();
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
