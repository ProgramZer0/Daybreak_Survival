using UnityEngine;
using System.Collections;

public enum WeatherType
{
    Clear,
    LightRain,
    HeavyRain,
    Thunderstorm,
    Snow
}

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager instance;

    [Header("Particle Systems")]
    public ParticleSystem lightRainFX;
    public ParticleSystem heavyRainFX;
    public ParticleSystem snowFX;
    public ParticleSystem lightRainSplashFX;
    public ParticleSystem heavyRainSplashFX;

    [Header("Links")]
    public GameObject thunderLight;
    public SoundManager SM;
    public GameManager GM;
    public EnemyController EM;

    [Header("Timing")]
    public float minWeatherDuration = 60f;
    public float maxWeatherDuration = 240f;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float indoorVolumeMultiplier = 0.2f;
    [Range(0f, 1f)] public float outdoorVolumeMultiplier = 1f;

    public WeatherType currentWeather = WeatherType.Clear;

    private Coroutine thunderRoutine;
    private Coroutine weatherDurationRoutine;

    private Coroutine audioFadeRoutine = null;
    private float currentVolumeMultiplier = 1f;

    private void Awake()
    {
        instance = this;
    }

    // ----------------------------
    // PUBLIC API
    // ----------------------------

    // Start completely random weather, random duration
    public void StartWeather()
    {
        StartWeatherInternal(null, null);
    }

    // Start specific weather, random duration
    public void StartWeather(WeatherType type)
    {
        StartWeatherInternal(type, null);
    }

    public void StartWeather(float duration)
    {
        StartWeatherInternal(null, duration);
    }

    // Start specific weather, defined duration
    public void StartWeather(WeatherType type, float duration)
    {
        StartWeatherInternal(type, duration);
    }

    // Stop current weather
    public void StopWeather()
    {
        if (weatherDurationRoutine != null)
        {
            StopCoroutine(weatherDurationRoutine);
            weatherDurationRoutine = null;
        }

        GM.SetAmb();
        SetWeather(WeatherType.Clear);
    }

    // Toggle particle visibility (for indoor/outdoor)
    public void UpdateVisibility(bool inside)
    {
        bool showParticles = !inside;

        var e1 = lightRainFX.emission;
        var e2 = heavyRainFX.emission;
        var e3 = snowFX.emission;
        var e4 = heavyRainSplashFX.emission;
        var e5 = lightRainSplashFX.emission;

        e1.enabled = showParticles;
        e2.enabled = showParticles;
        e3.enabled = showParticles;
        e4.enabled = showParticles;
        e5.enabled = showParticles;


        float targetMult = showParticles ? outdoorVolumeMultiplier : indoorVolumeMultiplier;
        FadeAudio(targetMult);
    }

    // Update sound volume based on indoor/outdoor
    public void FadeAudio(float targetMultiplier, float duration = 0.5f)
    {
        // Stop any currently running fade to avoid overlapping
        if (audioFadeRoutine != null)
        {
            StopCoroutine(audioFadeRoutine);
            audioFadeRoutine = null;
        }

        audioFadeRoutine = StartCoroutine(FadeAudioRoutine(targetMultiplier, duration));
    }
    private IEnumerator FadeAudioRoutine(float targetMultiplier, float duration)
    {
        float startMultiplier = currentVolumeMultiplier;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            currentVolumeMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, t / duration);

            SM.SetSoundVolume("thunder", currentVolumeMultiplier);
            SM.SetSoundVolume("lightrain", currentVolumeMultiplier);
            SM.SetSoundVolume("heavyrain", currentVolumeMultiplier);

            yield return null;
        }

        currentVolumeMultiplier = targetMultiplier;

        SM.SetSoundVolume("thunder", currentVolumeMultiplier);
        SM.SetSoundVolume("lightrain", currentVolumeMultiplier);
        SM.SetSoundVolume("heavyrain", currentVolumeMultiplier);

        audioFadeRoutine = null;
    }

    // ----------------------------
    // INTERNAL
    // ----------------------------

    private void StartWeatherInternal(WeatherType? specificType, float? specificDuration)
    {
        if (weatherDurationRoutine != null)
        {
            StopCoroutine(weatherDurationRoutine);
            weatherDurationRoutine = null;
        }

        WeatherType chosen = specificType.HasValue ? specificType.Value : ChooseRandomWeather();
        Debug.Log("setting weather as " + chosen);
        SetWeather(chosen);

        GM.FadeOutAmb();

        float duration = specificDuration.HasValue ? specificDuration.Value : Random.Range(minWeatherDuration, maxWeatherDuration);
        weatherDurationRoutine = StartCoroutine(WeatherDurationRoutine(duration));
    }

    private IEnumerator WeatherDurationRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        StopWeather();
    }

    private WeatherType ChooseRandomWeather()
    {
        int roll = Random.Range(0, 100);

        if (roll < 45) return WeatherType.LightRain; //45%
        if (roll < 70) return WeatherType.HeavyRain; //25%
        if (roll < 90) return WeatherType.Thunderstorm; //20%
        return WeatherType.Snow; //10%
    }

    public void SetWeather(WeatherType weather)
    {
        EM.SetWeather(weather);
        if (currentWeather == weather) return;

        currentWeather = weather;

        lightRainFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        heavyRainFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        snowFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (thunderRoutine != null)
        {
            StopCoroutine(thunderRoutine);
            thunderRoutine = null;
        }

        thunderLight.gameObject.SetActive(false);

        switch (weather)
        {
            case WeatherType.LightRain:
                lightRainFX.Play();
                SM.FadeInSound("lightrain");
                SM.FadeOutSound("heavyrain");
                break;

            case WeatherType.HeavyRain:
                heavyRainFX.Play();
                SM.FadeOutSound("lightrain");
                SM.FadeInSound("heavyrain");
                break;

            case WeatherType.Snow:
                snowFX.Play();
                SM.FadeOutSound("lightrain");
                SM.FadeOutSound("heavyrain");
                break;

            case WeatherType.Thunderstorm:
                heavyRainFX.Play();
                SM.FadeOutSound("lightrain");
                SM.FadeInSound("heavyrain");
                thunderRoutine = StartCoroutine(ThunderRoutine());
                break;
            case WeatherType.Clear:
                SM.FadeOutSound("lightrain");
                SM.FadeOutSound("heavyrain");
                break;
        }
    }

    private IEnumerator ThunderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(6f, 20f));
            

            thunderLight.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            thunderLight.gameObject.SetActive(false);

            yield return new WaitForSeconds(Random.Range(0.1f, 1f));
            SM.Play("thunder");
        }
    }
}