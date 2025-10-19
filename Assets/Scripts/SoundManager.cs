using System;
using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] GameManager GM;
    public Sound[] sounds;
    

    private float modSound = 0.6f;
    private float modMusicSound = 0.3f;

    private Sound currentMusic = null;   
    private Coroutine musicFadeCoroutine;

    private void Update()
    {
        if (currentMusic != null && currentMusic.source != null)
            if (!currentMusic.source.isPlaying) GM.SetEnvMusic(1, false);
    }

    private void Awake()
    {
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.loop = s.loop;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
        }
        //Debug.Log("finished SM init");
    }

    public float GetSoundMod() { return modSound; }

    public void PlayIfAlreadyNotPlaying(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (s == null)
        {
            Debug.LogWarning($"Sound '{name}' not found!");
            return;
        }

        if (s.source.isPlaying)
            return;

        float finalVolume = s.volume * (s.isMusic ? modMusicSound : modSound);

        s.source.volume = finalVolume;
        s.source.Play();
    }
    public void PlayRandomSound(string[] soundNames)
    {
        if (soundNames == null || soundNames.Length == 0)
        {
            Debug.LogWarning("No sound names provided for random selection!");
            return;
        }

        string chosen = soundNames[UnityEngine.Random.Range(0, soundNames.Length)];
        Play(chosen);
    }
    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (s == null)
        {
            Debug.LogWarning($"Sound '{name}' not found!");
            return;
        }

        float finalVolume = s.volume * (s.isMusic ? modMusicSound : modSound);

        s.source.volume = finalVolume;
        s.source.Play();
    }

    public void PlayrRandomPitch(string name, float plus_minusP)
    {
        Sound s = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (s == null)
        {
            Debug.LogWarning($"Sound '{name}' not found!");
            return;
        }
        
        float finalVolume = s.volume * (s.isMusic ? modMusicSound : modSound);
        float finalPitch = s.pitch + UnityEngine.Random.Range((0-plus_minusP), plus_minusP);
        
        s.source.volume = finalVolume;
        s.source.pitch = finalPitch;
        s.source.Play();
    }

    public void FadeInSoundIfNotPlaying(string name, float fadeTime = 1f)
    {
        Sound s = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (s == null)
        {
            Debug.LogWarning($"Sound '{name}' not found!");
            return;
        }

        if (s.source.isPlaying)
            return;

        StartCoroutine(FadeInCoroutine(s, fadeTime));   
    }

    public void FadeInSound(string name, float fadeTime = 1f)
    {
        Sound s = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (s == null)
        {
            Debug.LogWarning($"Sound '{name}' not found!");
            return;
        }

        StartCoroutine(FadeInCoroutine(s, fadeTime));
    }
    public void FadeOutSound(string name, float fadeTime = 1f)
    {
        Sound s = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (s == null)
        {
            Debug.LogWarning($"Sound '{name}' not found!");
            return;
        }

        StartCoroutine(FadeOutCoroutine(s, fadeTime));
    }

    private IEnumerator FadeOutCoroutine(Sound s, float fadeTime)
    {
        float startVolume = s.source.volume;

        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime;
            s.source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        s.source.Stop();
        s.source.volume = s.volume * (s.isMusic ? modMusicSound : modSound); 
        if (s == currentMusic)
            currentMusic = null;
    }
    private IEnumerator FadeInCoroutine(Sound s, float fadeTime)
    {
        float targetVolume = s.volume * (s.isMusic ? modMusicSound : modSound);

        s.source.volume = 0f;
        s.source.Play();

        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime;

            s.source.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        s.source.volume = targetVolume; 
    }
    public void PlayMusic(string name, float fadeTime = 1f)
    {
        Sound newMusic = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase) && sound.isMusic);

        if (newMusic == null)
        {
            Debug.LogWarning($"Music '{name}' not found!");
            return;
        }

        if (newMusic == currentMusic) return; 

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(CrossfadeMusic(newMusic, fadeTime));
    }
    public void PlayRandomMusic(string[] musicNames, float fadeTime = 1f)
    {
        if (musicNames == null || musicNames.Length == 0)
        {
            Debug.LogWarning("No music names provided for random selection!");
            return;
        }

        string chosen = musicNames[UnityEngine.Random.Range(0, musicNames.Length)];
        PlayMusic(chosen, fadeTime);
    }
    private IEnumerator CrossfadeMusic(Sound newMusic, float fadeTime)
    {
        Sound oldMusic = currentMusic;
        currentMusic = newMusic;

        if (newMusic != null)
        {
            newMusic.source.volume = 0f;
            newMusic.source.Play();
        }

        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime;

            if (oldMusic.source != null)
                oldMusic.source.volume = Mathf.Lerp(oldMusic.volume * modMusicSound, 0f, t);

            if (newMusic.source != null)
                newMusic.source.volume = Mathf.Lerp(0f, newMusic.volume * modMusicSound, t);

            yield return null;
        }

        if (oldMusic.source != null)
        {
            oldMusic.source.Stop();
            oldMusic.source.volume = oldMusic.volume * modMusicSound; 
        }

        if (newMusic.source != null)
            newMusic.source.volume = newMusic.volume * modMusicSound;

        musicFadeCoroutine = null;
    }
    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (s == null)
        {
            Debug.LogWarning($"Sound '{name}' not found!");
            return;
        }

        s.source.Stop();

        if (s == currentMusic)
            currentMusic = null;
    }

    public void StopAll()
    {
        foreach (Sound s in sounds) 
        {
            if(s != currentMusic)
                s.source.Stop();
        }
    }

    public void FadeOutCurrentMusic()
    {
        if(currentMusic != null)
        {
            StartCoroutine(FadeOutCoroutine(currentMusic, 1));
            currentMusic = null;
        }
    }
    public string GetCurrentMusicName()
    {
        return currentMusic != null ? currentMusic.name : null;
    }

    public void SetSoundMod(float vol)
    {
        Debug.Log("setting sound");

        modSound = vol;
        foreach (Sound s in sounds)
        {
            if (s != currentMusic)
                s.source.volume = s.volume * modSound;
        }
    }
    public void SetSoundMusicMod(float vol)
    {
        Debug.Log("setting music");
        modMusicSound = vol;
        if(currentMusic.source != null)
            currentMusic.source.volume = currentMusic.volume * modMusicSound;
    }
    public float getSoundMusicMod() { return modMusicSound; }

}