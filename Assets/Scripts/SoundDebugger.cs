using UnityEngine;

public class SoundDebugger : MonoBehaviour
{

    AudioSource[] sources;

    public void FindAll()
    {

        //Get every single audio sources in the scene.
        sources = GameObject.FindSceneObjectsOfType(typeof(AudioSource)) as AudioSource[];

    }

    [ContextMenu("run Debug")]
    public void RunDebugs()
    {
            FindAll();
            foreach (AudioSource audioSource in sources)
            {
                if (audioSource.isPlaying) Debug.Log(audioSource.name + " is playing " + audioSource.clip.name);
            }
            Debug.Log("---------------------------"); //to avoid confusion next time
            Debug.Break(); //pause the editor

    }
}
