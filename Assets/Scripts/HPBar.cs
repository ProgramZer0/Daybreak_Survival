using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [SerializeField] private Slider HPFill;
    private float HP = 0f;
    private float maxHP = 0f;

    public void SetMaxHP(float _maxHP)
    {
        maxHP = _maxHP;
        HPFill.value = HPFill.maxValue;
    }

    public void SetHP(float _hp)
    {
        HP = _hp;
        updateHP();
    }

    public void updateHP()
    {
        if (HP <= 0 || maxHP <= 0)
            return;
        float HPFillerNum = (HP / maxHP) * HPFill.maxValue;
        HPFill.value = HPFillerNum;
    }
}
