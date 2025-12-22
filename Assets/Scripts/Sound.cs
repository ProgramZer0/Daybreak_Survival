using UnityEngine;


[System.Serializable]
public class Sound 
{
    public string name;
    public AudioClip clip;

    public bool loop;
    public bool isMusic = false;

    [Range(0f,1f)]
    public float volume = .2f;
    [Range(0f, 3f)]
    public float pitch = 1;

    [Range(0, 250)]
    public int priority = 128;
    public bool useIndoorDampen = false;

    [HideInInspector]
    public AudioSource source;
}
