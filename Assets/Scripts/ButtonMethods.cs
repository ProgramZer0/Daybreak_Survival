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
        
    }
    public void OpenLifestyle_B()
    {
        GUI.ShowNoGUI();
        FindAnyObjectByType<SoundManager>().Play("buttonClick");
        GUI.OpenLifestyle();
        LSGui.LoadLifeStyles();
        BackMainMenu_B();
    }
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
