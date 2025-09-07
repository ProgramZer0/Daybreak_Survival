using UnityEngine;

public class pickup : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private bool isAmmo = false;
    [SerializeField] private WeaponAmmoType weaponAmmoType;
    [SerializeField] private int ammoAmmount = 0;

    void OnCollisionEnter2D(Collision2D collision)
    {
        collision.gameObject.TryGetComponent<PlayerInterface>(out PlayerInterface player);
        if (player != null)
        {
            if (!isAmmo)
            {
                if (player.AddWeapon(weapon))
                {

                    Destroy(transform.gameObject);
                }   
            }
            else
            {
                if (player.AddAmmo(weaponAmmoType, ammoAmmount))
                {
                    Destroy(transform.gameObject);
                }
                else
                {
                    //full
                }
            }   
        }
    }
}
