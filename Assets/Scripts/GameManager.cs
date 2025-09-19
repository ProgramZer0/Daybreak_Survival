using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    [Header("Used Game objects")]
    [SerializeField] private MainGUIController GUI;
    [SerializeField] private GroundBuilder builder;
    [SerializeField] private PlayerInterface player;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private SoundManager SM;
    [SerializeField] private Light2D daylight;

    [Header("night/day cycle")]
    [SerializeField] private float daylightTimeSec = 800f;
    [SerializeField] private float nightTimeSec = 500f;
    [SerializeField] private float debugTimemMult = 10;
    [SerializeField] private float daylightMax = 0.75f;
    [SerializeField] private float nightDarkness = 0f;
    [SerializeField] private float dawnSpeed = 2.5f;
    [SerializeField] private bool cycleEnabled = false;

    private float cycleTimer = 0;
    private bool isDay = true;


    private void Start()
    {
        ResetDNCycle();
    }

    private void ResetDNCycle()
    {
        daylight.intensity = daylightMax;
        cycleTimer = daylightTimeSec / 2;
        isDay = true;
        cycleEnabled = false;
    }

    private void Update()
    {
        if(cycleEnabled)
            cycleTimer += Time.deltaTime * debugTimemMult;
    }

    private void FixedUpdate()
    {
        if (!cycleEnabled) return;

        float cycleDuration = isDay ? daylightTimeSec : nightTimeSec;
        cycleTimer += Time.fixedDeltaTime * debugTimemMult;

        float progress = cycleTimer / cycleDuration;

        if (progress >= 1f)
        {
            cycleTimer = 0;
            progress = 0;

            if (isDay)
            {
                setNight();
                isDay = false;
            }
            else
            {
                setDay();
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

    public void ResetGame()
    {

    }

    public void StartGame()
    {

    }

    public void EndGameFail()
    {

    }

    private void setDay()
    {
        enemyController.SetIsDay(true);
    }

    private void setNight()
    {
        enemyController.SetIsDay(false);
    }
}
