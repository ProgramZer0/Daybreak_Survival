using UnityEngine;
using System.Collections;
using System.Collections.Generic;


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

    public WeatherType currentWeather = WeatherType.Clear;

    private Coroutine thunderRoutine;

    private void Awake()
    {
        instance = this;
    }

    private void StartWeather()
    {
        StartCoroutine(WeatherCycleRoutine());
    }

    private void StopWeather()
    {
        StopCoroutine(WeatherCycleRoutine());
    }

    private IEnumerator WeatherCycleRoutine()
    {
        while (true)
        {
            // Wait random time between weather changes
            float t = Random.Range(minWeatherDuration, maxWeatherDuration);
            yield return new WaitForSeconds(t);

            ChooseRandomWeather();
        }
    }

    private void ChooseRandomWeather()
    {
        // Weighted random weather
        int roll = Random.Range(0, 100);

        WeatherType newWeather;

        if (roll < 60) newWeather = WeatherType.Clear;               // 60%
        else if (roll < 75) newWeather = WeatherType.LightRain;      // +15%
        else if (roll < 87) newWeather = WeatherType.HeavyRain;      // +12%
        else if (roll < 95) newWeather = WeatherType.Snow;           // +8%
        else newWeather = WeatherType.Thunderstorm;                  // +5%

        SetWeather(newWeather);
    }

    public void SetWeather(WeatherType weather)
    {
        if (currentWeather == weather) return;

        currentWeather = weather;

        // Stop everything
        lightRainFX.Stop();
        heavyRainFX.Stop();
        snowFX.Stop();

        if (thunderRoutine != null)
        {
            StopCoroutine(thunderRoutine);
            thunderRoutine = null;
        }
        thunderLight.gameObject.SetActive(false);

        // Enable new effect
        switch (weather)
        {
            case WeatherType.Clear:
                break;

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

            // Flash light
            thunderLight.gameObject.SetActive(true);
            thunderLight.intensity = Random.Range(2f, 5f);

            yield return new WaitForSeconds(0.1f);
            thunderLight.gameObject.SetActive(false);

            // Play sound slightly delayed
            yield return new WaitForSeconds(Random.Range(0.2f, 1f));
            SM.Play("thunder");
        }
    }
}