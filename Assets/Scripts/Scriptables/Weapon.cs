using UnityEngine;

public enum WeaponAmmoType
{
    pistol,
    asualt,
    sniper,
    rocket,
    flame_fuel
}

[CreateAssetMenu(fileName = "Weapon", menuName = "Weapon")]
public class Weapon : ScriptableObject
{
    public string weaponName;
    public GameObject prefab;
    public int maxAmmo;
    public float projectileDamage;
    public float projectileSpeed;
    public float projectileAmount;
    public float projectileCooldown;
    public float spread = 0f;
    public bool splashDamage = false;
    public GameObject projectilePrefab;
    public WeaponAmmoType weaponAmmoType;
}

