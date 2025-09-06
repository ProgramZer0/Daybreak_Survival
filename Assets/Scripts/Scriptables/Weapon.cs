using UnityEngine;

public enum WeaponAmmoType
{
    pistol,
    asualt,
    sniper,
    rocket
}

[CreateAssetMenu(fileName = "Weapon", menuName = "Weapon")]
public class Weapon : ScriptableObject
{
    public string weaponName;
    public GameObject prefab;
    public int maxAmmo;
    public float damage;
    public float attackSpeed;
    public WeaponAmmoType weaponAmmoType;
}

