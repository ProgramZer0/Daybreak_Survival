using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DashIndicator : MonoBehaviour
{
    [SerializeField] private Slider DashFill;

    private float dashCooldown = 10f;
    private bool dashTriggered = false;
    private float dashTimer = 0;
    public void SetCooldown(float cooldown)
    {
        //Debug.Log("called");
        dashCooldown = cooldown;
        DashFill.value = 0;
    }

    public void TriggerCooldown()
    {
        dashTriggered = true;
    }

    private void Update()
    {
        if (dashTriggered)
        {
            dashTimer += Time.deltaTime;

            if(dashTimer < 0 || dashCooldown <= 0)
            {
                dashTriggered = false;
                return;
            }

            if(dashTimer >= dashCooldown)
            {
                dashTriggered = false;
                return;
            }

            float val = (dashTimer / dashCooldown) * DashFill.maxValue;
            DashFill.value = val;
        }
        else
        {
            dashTimer = 0;
        }
    }
}
