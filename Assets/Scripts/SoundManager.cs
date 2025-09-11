using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public Sound[] sounds;
    private float modSound = 0.6f;
    private float MusicSound = 0.3f;

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
    }

    public void Play(string name)
    {
        Sound S = Array.Find(sounds, sounds => sounds.name == name);
        S.source.volume = modSound;
        S.source.Play();
    }

    public void SetSoundMod(float vol)
    {
        modSound = vol;
    }
    public void SetSoundMusicMod(float vol)
    {
        MusicSound = vol;
    }
}
