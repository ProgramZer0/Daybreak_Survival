using UnityEngine;

public enum pickupType
{
    weapon,
    Ammo,
    lifestyle,
    hiddenItem
}

public class pickup : MonoBehaviour
{
    public Weapon weapon;
    [SerializeField] private pickupType pickupType = pickupType.weapon;
    [SerializeField] private LifeStyles lifeStyle;
    [SerializeField] private Ammo ammo;
    public int currentAmmo = 0;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.gameObject.TryGetComponent<PlayerInterface>(out PlayerInterface player);
        if (player != null)
        {
            if (pickupType == pickupType.weapon)
            {
                Debug.Log("sending pickup");
                player.SetCurrentPickup(this);
            }
            else if(pickupType == pickupType.Ammo)
            {
                if (player.AddAmmo(ammo.weaponAmmoType, ammo.ammoAmount))
                {
                    Destroy(transform.gameObject);
                }
                else
                {
                    //full
                }
            }
            else if (pickupType == pickupType.lifestyle)
            {
                if(player.AddNewLifestyle(lifeStyle))
                    Destroy(transform.gameObject);
                else
                {
                    //already have it
                }
            }
            else if (pickupType == pickupType.hiddenItem)
            {

            }
            else
            {

            }
        }
    }

    public void addPickup(GameObject playerobj)
    {
        playerobj.TryGetComponent<PlayerInterface>(out PlayerInterface player);
        if (player != null)
        {
            if (player.AddWeapon(weapon, currentAmmo)) 
            { 
                
                Destroy(transform.gameObject);
            }
        }
    }
}
