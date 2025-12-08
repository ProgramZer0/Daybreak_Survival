using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class IndoorBlackoutFader : MonoBehaviour
{
    private SpriteRenderer sr;
    private Coroutine fadeRoutine;

    [Header("Settings")]
    public float fadeDuration = 0.35f;  
    public float indoorAlpha = .95f;     
    public float outdoorAlpha = 0f;    

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void FadeIn()
    {
        StartFade(indoorAlpha);
    }

    public void FadeOut()
    {
        StartFade(outdoorAlpha);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float start = sr.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            float a = Mathf.Lerp(start, targetAlpha, t);
            sr.color = new Color(0, 0, 0, a);

            time += Time.deltaTime;
            yield return null;
        }

        sr.color = new Color(0, 0, 0, targetAlpha);
    }
}