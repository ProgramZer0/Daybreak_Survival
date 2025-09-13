using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [SerializeField] private Sprite[] hpSprites;
    [SerializeField] private Image rend;
    private float HP = 0f;
    private float maxHP = 0f;

    public void SetMaxHP(float _maxHP)
    {
        maxHP = _maxHP;
        rend.sprite = hpSprites[hpSprites.Length - 1];
    }

    public void SetHP(float _hp)
    {
        HP = _hp;
        updateHP();
    }

    public void updateHP()
    {
        int spriteNumba = (int)((HP / maxHP) * hpSprites.Length);
        if(HP == maxHP) spriteNumba -= 1;
        if (spriteNumba >= 0 && spriteNumba <= hpSprites.Length - 1)
            rend.sprite = hpSprites[spriteNumba];
        else
            rend.sprite = hpSprites[0];
    }
}
