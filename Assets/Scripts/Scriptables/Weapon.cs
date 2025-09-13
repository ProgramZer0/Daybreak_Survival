using UnityEngine;

public enum WeaponAmmoType
{
    pistol,
    asualt,
    sniper,
    rocket,
    flame_fuel,
    shells
}

[CreateAssetMenu(fileName = "Weapon", menuName = "Weapon")]
public class Weapon : ScriptableObject
{
    public string weaponName;
    public GameObject prefab;
    public Sprite sprite;
    public int maxAmmo;
    public float projectileDamage = 0f;
    public float projectileSpeed = 0f;
    public float projectileAmount = 0f;
    public float projectileTime = 7;
    public float projectileCooldown = 0f;
    public float projectileSpread = 0f;
    public float projectileFallOffMultiplier = 1;
    public float projectileFallOffMultiplierTime = 3;
    public float spawnSpread = 0f;
    public bool projectileHasAnimation = true;
    public bool splashDamage = false;
    public float splashRange = 0f;
    public float appearTime = 0f;
    public float fadeTime = 0.1f;
    public GameObject projectilePrefab;
    public WeaponAmmoType weaponAmmoType;
}

