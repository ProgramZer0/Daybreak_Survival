using UnityEngine;

public class pickup : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private bool isAmmo = false;
    [SerializeField] private WeaponAmmoType weaponAmmoType;
    [SerializeField] private int ammoAmmount = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.gameObject.TryGetComponent<PlayerInterface>(out PlayerInterface player);
        if (player != null)
        {
            if (!isAmmo)
            {
                Debug.Log("sending pickup");
                player.SetCurrentPickup(this);
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

    public void addPickup(GameObject playerobj)
    {
        playerobj.TryGetComponent<PlayerInterface>(out PlayerInterface player);
        if (player != null)
        {
            if (player.AddWeapon(weapon)) 
            { 
            
                Destroy(transform.gameObject);
            }
        }
    }
}
