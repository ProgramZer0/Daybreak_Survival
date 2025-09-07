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
        GUI.OpenPause();
    }

    public void Settings_B()
    {
        GUI.ShowNoGUI();
        GUI.OpenSettingsMenu();
    }

    public void PauseMenu_B()
    {
        GUI.ShowNoGUI();
        GUI.OpenPause();
    }

    public void StartGame_B()
    {
        GM.StartGame();
    }
}
