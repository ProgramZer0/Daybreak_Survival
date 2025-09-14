using UnityEngine;

public class dashAnim : MonoBehaviour
{
    private float destroyTime = .1f;
    private float timer = 0;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= destroyTime)
            Destroy(gameObject);
    }
}
