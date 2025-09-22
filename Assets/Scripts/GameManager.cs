using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    [Header("Used Game objects")]
    [SerializeField] private MainGUIController GUI;
    [SerializeField] private GroundBuilder builder;
    [SerializeField] private PlayerInterface player;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private SoundManager SM;
    [SerializeField] private Light2D daylight;

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

    private float cycleTimer = 0;
    private float deathTimer = 0f;
    private bool isDay = true;
    private bool inMenu = true;

    private void Start()
    {
        ResetDNCycle();
        StartCoroutine(PlayLate(1,1));
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
            SM.FadeInSound("deathComming");
            

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
        ResetGame();
        inMenu = true;
        PlayRandomEnvMusic(0.3f);
    }

    public void ResetGame()
    {
        ResetDNCycle();
        ResetPlayer();
        enemyController.enableSpawning(false);
        enemyController.DespawnAllEnemies();
        SM.StopAll();
    }

    public void StartGame()
    {
        inMenu = false;
        cycleEnabled = true;
        ResetGame();
        enemyController.enableSpawning(true);
        SetEnvMusic(0.5f);
        SetAmbiance();
    }

    public void EndGameFail()
    {

    }

    public void SaveGame()
    {

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
