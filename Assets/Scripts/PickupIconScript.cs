using UnityEngine;

public class PickupIconScript : MonoBehaviour
{
    private float destroyTime = 999f;
    public void SetPickupTime(float time)
    {
        destroyTime = time;
    }

    private void Update()
    {
        
    }
}
