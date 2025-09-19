using UnityEngine;

public class hitboxScript : MonoBehaviour
{

    [SerializeField] private GameObject enemyHit;

    public GameObject getIEnemy()
    {
        return enemyHit;
    }
}
