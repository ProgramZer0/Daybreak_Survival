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

    [Header("Thunderstorm")]
    public Light thunderLight;
    public SoundManager SM;

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

        SetWeather(WeatherType.Clear);
    }

    // Toggle particle visibility (for indoor/outdoor)
    public void UpdateVisibility(bool inside)
    {
        bool showParticles = !inside;

        var e1 = lightRainFX.emission;
        var e2 = heavyRainFX.emission;
        var e3 = snowFX.emission;

        e1.enabled = showParticles;
        e2.enabled = showParticles;
        e3.enabled = showParticles;
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



    // Optional: smooth fade of audio
    public void FadeAudio(float targetMultiplier, float duration = 0.5f)
    {
        StartCoroutine(FadeAudioRoutine(targetMultiplier, duration));
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
            SM.SetSoundVolume("rainLoop", currentVolumeMultiplier);
            SM.SetSoundVolume("snowLoop", currentVolumeMultiplier);

            yield return null;
        }

        currentVolumeMultiplier = targetMultiplier;

        SM.SetSoundVolume("thunder", currentVolumeMultiplier);
        SM.SetSoundVolume("rainLoop", currentVolumeMultiplier);
        SM.SetSoundVolume("snowLoop", currentVolumeMultiplier);

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
        SetWeather(chosen);

        float duration = specificDuration.HasValue ? specificDuration.Value : Random.Range(minWeatherDuration, maxWeatherDuration);
        weatherDurationRoutine = StartCoroutine(WeatherDurationRoutine(duration));
    }

    private IEnumerator WeatherDurationRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        SetWeather(WeatherType.Clear);

        weatherDurationRoutine = null;
    }

    private WeatherType ChooseRandomWeather()
    {
        int roll = Random.Range(0, 100);

        if (roll < 60) return WeatherType.Clear;
        if (roll < 75) return WeatherType.LightRain;
        if (roll < 87) return WeatherType.HeavyRain;
        if (roll < 95) return WeatherType.Snow;
        return WeatherType.Thunderstorm;
    }

    public void SetWeather(WeatherType weather)
    {

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
                break;

            case WeatherType.HeavyRain:
                heavyRainFX.Play();
                break;

            case WeatherType.Snow:
                snowFX.Play();
                break;

            case WeatherType.Thunderstorm:
                heavyRainFX.Play();
                thunderRoutine = StartCoroutine(ThunderRoutine());
                break;
        }
    }

    private IEnumerator ThunderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(4f, 12f));

            thunderLight.gameObject.SetActive(true);
            thunderLight.intensity = Random.Range(2f, 5f);
            yield return new WaitForSeconds(0.1f);
            thunderLight.gameObject.SetActive(false);

            yield return new WaitForSeconds(Random.Range(0.2f, 1f));
            SM.Play("thunder", currentVolumeMultiplier);
        }
    }
}