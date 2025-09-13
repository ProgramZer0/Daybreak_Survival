using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DashIndicator : MonoBehaviour
{
    [SerializeField] private Sprite[] dashSprites;
    [SerializeField] private Image rend;

    private float dashCooldown = 10f;
    public void SetCooldown(float cooldown)
    {
        //Debug.Log("called");
        dashCooldown = cooldown;
        rend.sprite = dashSprites[dashSprites.Length - 1];
    }

    public void TriggerCooldown()
    {
        float eachSpriteTime = dashCooldown / (dashSprites.Length - 1);
        //timse = eachSpriteTime;
        rend.sprite = dashSprites[0];
        StartCoroutine(DoAnimation(eachSpriteTime));
    }

    private IEnumerator DoAnimation(float time)
    {
        for (int i = 1; i < dashSprites.Length; i++)
        {
            yield return new WaitForSeconds(time);
            rend.sprite = dashSprites[i];
            //Debug.Log("waited for " + time + " set " + i);
        }
    }    
}
