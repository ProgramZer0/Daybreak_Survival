using UnityEngine;

public class WeaponSpawner : MonoBehaviour
{
    [SerializeField] private Weapon[] weapons; 



    //now only for equipment
    /*
    public void SpawnWeapon()
    { 
        foreach (GameObject obj in groundBuilder.weaponSpawnersObjs)
        {
            GameObject weaponInit = weapons[Random.Range(0, weapons.Length)].prefab;

            obj.GetComponent<Spawner>().Spawn(weaponInit);
        }
    }*/
}
