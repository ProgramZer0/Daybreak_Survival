using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    public List<int> availableLifestyleIds = new();
    public List<int> activeLifestyleIds = new();

    // Settings
    public bool crouchToggle = false;
    public float musicVolume = 1f;
    public float soundVolume = 1f;
}