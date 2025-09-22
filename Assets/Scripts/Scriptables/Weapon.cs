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

public enum Rarity
{
    common,
    uncommon,
    Rare,
    VeryRare,
    Impossible
}

[CreateAssetMenu(fileName = "Weapon", menuName = "Weapon")]
public class Weapon : ScriptableObject
{
    public string weaponName;
    public GameObject prefab; 
    public Sprite sprite;
    public int maxAmmo;
    public bool canShoot = true;
    public bool canMelee = true;
    public float meleeDamage = 1f;
    public float meleeRange = .4f;
    public float meleeFOV = 70f;
    public int meleeRays = 7;
    public float meleeCooldown = .5f;
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
    public float fadeInTime = 0.1f;
    public float soundMod = 0f;
    public float weaponZoom = 10f;
    public float weaponMinZoom = 3f;
    public float pickupTime = 2f;
    public bool hasFlash = true;
    public bool meleeStun = false;
    public float offsetSpawnProjectile = 0f;
    public string equipSoundName = "DefaultweaponEquip";
    public string projectileSoundName = "gunshot";
    public Rarity rarity = Rarity.common;
    public GameObject projectilePrefab;
    public WeaponAmmoType weaponAmmoType;
}

