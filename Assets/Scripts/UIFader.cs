using UnityEngine;
using System.Collections;

public class UIFader : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.15f; 
    public Vector3 startScale = new Vector3(0.95f, 0.95f, 0.95f); 
    public Vector3 endScale = Vector3.one; 

    private Coroutine currentFade;

    public void FadeIn()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeAndScale(1f, startScale, endScale));
    }

    public void FadeOut()
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeAndScale(0f, endScale, startScale));
    }

    private IEnumerator FadeAndScale(float targetAlpha, Vector3 scaleFrom, Vector3 scaleTo)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        transform.localScale = scaleFrom;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / fadeDuration;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, t);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        transform.localScale = scaleTo;

        bool visible = targetAlpha > 0.99f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
