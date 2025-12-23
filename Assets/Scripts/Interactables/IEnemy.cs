using UnityEngine;

internal interface IEnemy
{
    void Initialize(GameObject player);
    void Initialize(GameObject player, bool _isDay, SoundManager _SM, WeatherType _weatherType);
    void TakeDamage(float damage, bool _isStunned = false);

    GameObject GetGameObj();
}