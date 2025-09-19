using UnityEngine;

internal interface IEnemy
{
    void Initialize(GameObject player);
    void Initialize(GameObject player, bool _isDay);
    void TakeDamage(float damage);

    GameObject GetGameObj();
}