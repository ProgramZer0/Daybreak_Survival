using UnityEngine;

[CreateAssetMenu(fileName = "Ammo", menuName = "Scriptable Objects/Ammo")]
public class Ammo : ScriptableObject
{
    public GameObject prefab;
    public int ammoAmount;
    public WeaponAmmoType weaponAmmoType;
}
