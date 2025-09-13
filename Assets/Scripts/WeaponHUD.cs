using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    [SerializeField] private Image weaponRender;
    [SerializeField] private GameObject player;

    private string ammoString = "0/0";
    private Weapon currentWeapon;

    private void Update()
    {
        ammoString = player.GetComponent<PlayerInterface>().GetAmmo();

        if (currentWeapon != null)
        {
            Color tempCol = weaponRender.color;
            tempCol.a = 1;
            weaponRender.color = tempCol;
            weaponRender.sprite = currentWeapon.sprite;
            GetComponentInChildren<TextMeshProUGUI>().text = ammoString;
        }
        else
        {
            Color tempCol = weaponRender.color;
            tempCol.a = 0;
            weaponRender.color = tempCol;
            GetComponentInChildren<TextMeshProUGUI>().text = "0/0";
        }
    }

    public void SetCurrentWeapon(Weapon wp)
    {
        currentWeapon = wp;
    }
}
