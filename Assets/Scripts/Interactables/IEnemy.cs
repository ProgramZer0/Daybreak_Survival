using UnityEngine;

internal interface IEnemy
{
    void Initialize(GameObject player);
    void TakeDamage(float damage);
}