using UnityEngine;
using UnityEngine.UI;

public class PickupIconScript : MonoBehaviour
{
    [SerializeField] private Slider DashFill;

    private float playerPickupTime = 3f;
    private bool pickupTriggered = false;
    private float pickupTimer = 0;
    public void SetPickup(float pickupT)
    {
        //Debug.Log("called");
        playerPickupTime = pickupT;
        DashFill.value = 0;
    }

    public void triggerPickup()
    {
        pickupTriggered = true;
    }

    public void stopPickup()
    {
        pickupTriggered = false;
        pickupTimer = 0;
        DashFill.value = 0;
    }

    private void Update()
    {
        if (pickupTriggered)
        {
            pickupTimer += Time.deltaTime;

            if (pickupTimer < 0 || playerPickupTime <= 0)
            {
                pickupTriggered = false;
                return;
            }

            if (pickupTimer >= playerPickupTime)
            {
                pickupTriggered = false;
                return;
            }

            float val = (pickupTimer / playerPickupTime) * DashFill.maxValue;
            DashFill.value = val;
        }
        else
        {
            DashFill.value = 0;
            pickupTimer = 0;
        }
    }
}
