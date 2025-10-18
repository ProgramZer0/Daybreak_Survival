using System.Collections;
using System.Collections.Generic;
using NavMeshPlus;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using NavMeshPlus.Components;

public class GameManager : MonoBehaviour
{
    //[Header("Save Settings")]


    [Header("Used Game objects")]
    [SerializeField] private MainGUIController GUI;
    [SerializeField] private LifeStyleController LSC;
    [SerializeField] private TerrainBuilder TB;
    [SerializeField] private PlayerInterface player;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private OtherSpawner OS;
    [SerializeField] private SoundManager SM;
    [SerializeField] private Light2D daylight;
    [SerializeField] private NavMeshSurface surface;
        
    [Header("night/day cycle")]
    [SerializeField] private float daylightTimeSec = 800f;
    [SerializeField] private float nightTimeSec = 500f;
    [SerializeField] private float debugTimeMult = 10;
    [SerializeField] private float daylightMax = 0.75f;
    [SerializeField] private float nightDarkness = 0f;
    [SerializeField] private bool cycleEnabled = false;

    [Header("Main Game Settings")]
    [SerializeField] private float deathTime = 3600;
    [SerializeField] private float colorStart = 0.7f;
    [SerializeField] private Color deathColor;

    [Header("Sound settings")]
    [SerializeField] private string[] mainSongs;
    [SerializeField] private string[] nightSongs;
    [SerializeField] private string[] daySongs;
    [SerializeField] private float minNoMusic;
    [SerializeField] private float maxNoMusic;

    private static string SavePath => Path.Combine(Application.persistentDataPath, "playerSave.json");

    private float cycleTimer = 0;
    private float deathTimer = 0f;
    private bool isDay = true;
    private bool inMenu = true;
    private bool startedGame = false;
    private bool hitMMButton = false;
    private void Start()
    {
        ResetDNCycle();
        StartCoroutine(PlayLate(1,1));
        LoadData();
    }

    private void ResetDNCycle()
    {
        daylight.intensity = daylightMax;
        cycleTimer = daylightTimeSec / 2;
        isDay = true;
        cycleEnabled = false;
    }

    private void ResetPlayer()
    {
        playerRenderer.color = Color.white;
        player.transform.position = Vector2.zero;
        player.ResetPlayerData();
    }

    private void Update()
    {
        if (cycleEnabled)
        {
            cycleTimer += Time.deltaTime * debugTimeMult;
            deathTimer += Time.deltaTime * debugTimeMult;
        }

        if (!TB.isRunning && startedGame)
        {
            enemyController.enableSpawning(true);
            OS.SpawnAll();
            SetEnvMusic(10f);
            SetAmbiance();
            surface.BuildNavMesh();
            LSC.AddAllActive();
            deathTime = deathTime + player.ModDeathTimeAdd;
            if (player.hasNightVison)
                nightDarkness = 0.005f;
            GUI.ShowNoGUI();
            GUI.ShowAllHUDs();
            GUI.SetHUDVals();
            
            startedGame = false;
            Debug.Log("Bulding done");
        }

        if (!TB.isDeleting && hitMMButton)
        {
            Debug.Log("deleting ");
            GUI.ShowNoGUI();
            GUI.OpenMainMenu();
            hitMMButton = false;
        }
    }

    private void FixedUpdate()
    { 
        if (!cycleEnabled) return;

        if (deathTimer >= deathTime)
        {
            EndGameFail();
        }
        float deathProgress = Mathf.Clamp01(deathTimer / deathTime); 
        if (deathProgress > colorStart)
        {
            float colorProgress = Mathf.SmoothStep(0f, 1f, (deathProgress - colorStart) / (1f - colorStart));

            playerRenderer.color = Color.Lerp(Color.white, deathColor, colorProgress);
        }

        if (deathProgress >= .9)
            SM.FadeInSoundIfNotPlaying("deathComming");
            

        float cycleDuration = isDay ? daylightTimeSec : nightTimeSec;

        float progress = cycleTimer / cycleDuration;

        if (progress >= 1f)
        {
            cycleTimer = 0;
            progress = 0;

            if (isDay)
            {
                SetNight();
                isDay = false;
            }
            else
            {
                SetDay();
                isDay = true;
            }
        }

        if (isDay)
        {
            float lightVal = Mathf.Lerp(nightDarkness, daylightMax, Mathf.Sin(progress * Mathf.PI));
            daylight.intensity = lightVal;
        }
        else
        {
            daylight.intensity = nightDarkness;
        }
    }

    public void MainMenu()
    {
        if (inMenu) return;
        PlayRandomEnvMusic(5);
        ResetGame();
        inMenu = true;
        hitMMButton = true;
    }

    public void ResetGame()
    {
        ResetDNCycle();
        ResetPlayer();
        OS.DespawnAll();
        enemyController.enableSpawning(false);
        enemyController.DespawnAllEnemies();
        TB.ClearTerrain();
        SM.StopAll();
    }

    public void StartGame()
    {
        inMenu = false;
        cycleEnabled = true;
        ResetGame();
        TB.GenerateTerrain();
        startedGame = true;
    }

    public void EndGameFail()
    {
        Time.timeScale = 0f;

        GUI.ShowNoGUI();
        GUI.ShowAllHUDs();
        GUI.OpenDeath();
        SaveGameData();
    }

    public void SaveGameData()
    {
        var saveData = new PlayerSaveData();

        foreach (var ls in LSC.lifeStylesAvailable)
            saveData.availableLifestyleIds.Add(ls.id);

        var active = LSC.GetActiveLifestyles();
        if(active != null)
        {
            foreach (var ls in active)
                saveData.activeLifestyleIds.Add(ls.id);
        }

        saveData.crouchToggle = player.GetCrouchToggle();
        saveData.musicVolume = SM.getSoundMusicMod();
        saveData.soundVolume = SM.GetSoundMod();

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"Game saved to {SavePath}");
    }

    public void LoadData()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found!");
            LSC.InitLists();
            return;
        }

        string json = File.ReadAllText(SavePath);
        var saveData = JsonUtility.FromJson<PlayerSaveData>(json);

        LSC.ClearAll();

        foreach (int id in saveData.availableLifestyleIds)
        {
            LifeStyles ls = System.Array.Find(LSC.GetAllLifeStyles(), x => x.id == id);
            if (ls != null && !LSC.lifeStylesAvailable.Contains(ls))
                LSC.lifeStylesAvailable.Add(ls);
        }

        foreach (int id in saveData.activeLifestyleIds)
        {
            LifeStyles ls = System.Array.Find(LSC.GetAllLifeStyles(), x => x.id == id);
            if (ls != null)
                LSC.MakeLifestylesActive(ls);
        }

        // Apply settings
        player.SetCrouchToggle(saveData.crouchToggle);
        SM.SetSoundMusicMod(saveData.musicVolume);
        SM.SetSoundMod(saveData.soundVolume);

        Debug.Log("Game loaded from save file.");
    }

    private void SetDay()
    {
        enemyController.SetIsDay(true);
        SM.FadeOutSound("nightAmbiance");
        SM.FadeInSound("dayAmbiance");
        SM.FadeOutCurrentMusic();
        SetEnvMusic(1, false);
    }

    private void SetNight()
    {
        enemyController.SetIsDay(false);
        SM.FadeOutSound("dayAmbiance");
        SM.FadeInSound("nightAmbiance");
        SM.FadeOutCurrentMusic();
        SetEnvMusic(1, false);
    }

    private void SetAmbiance()
    {
        if (isDay)
            SetDay();
        else
            SetNight();
    }
    public void SetEnvMusic(float fade, bool startNow = true)
    {
        if (!startNow)
            StartCoroutine(PlayLate(Random.Range(minNoMusic, maxNoMusic), fade));
        else
            PlayRandomEnvMusic(fade);
    }

    private IEnumerator PlayLate(float time, float fade)
    {
        yield return new WaitForSeconds(time);
        PlayRandomEnvMusic(fade);
    }

    private void PlayRandomEnvMusic(float fade)
    {
        if (isDay && !inMenu)
            SM.PlayRandomMusic(daySongs, fade);
        else if (!isDay && !inMenu)
            SM.PlayRandomMusic(nightSongs, fade);
        else
            SM.PlayRandomMusic(mainSongs, fade);
    }
}
