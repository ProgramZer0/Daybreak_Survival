using UnityEngine;

public class BasicLifeStyle : lifestyleScript
{
    private PlayerInterface player;
    private LifeStyles LS;

    public override void Initialize(PlayerInterface _player, LifeStyles lifestlye)
    {
        player = _player;
        LS = lifestlye;

        SetBasicValues();

        player.ChangedModValues();
    }

    private void SetBasicValues()
    {
        player.ModCrouchSpeed += LS.ModCrouchSpeed;
        player.ModDashCooldown += LS.ModDashCooldown;
        player.ModNormSpeed += LS.ModNormSpeed;
        player.ModSprintSpeed += LS.ModSprintSpeed;
        player.ModlightDistance += LS.ModlightDistance;
        player.ModSeeDistance += LS.ModSeeDistance;
        player.ModDashRange += LS.ModDashRange;
        player.ModDashOffset += LS.ModDashOffset;
        player.ModSprintTime += LS.ModSprintTime;
        player.ModSprintCooldown += LS.ModSprintCooldown;
        player.ModMaxSprintDebuffTime += LS.ModMaxSprintDebuffTime;
        player.ModInteractRange += LS.ModInteractRange;
        player.ModPickupRange += LS.ModPickupRange;
        player.ModHordeForgetTime += LS.ModHordeForgetTime;
        player.ModMaxHP += LS.ModMaxHP;
        player.ModLoudness += LS.ModLoudness;
        player.ModEnemySeeRange += LS.ModEnemySeeRange;
        player.ModMeleeAttackDamage += LS.ModMeleeAttackDamage;
        player.ModMeleeAttackRange += LS.ModMeleeAttackRange;
        player.ModDeathTimeAdd += LS.ModDeathTimeAdd;
        player.ModMaxAmmo += LS.ModMaxAmmo;
        player.ModWeaponDamage += LS.ModWeaponDamage;
    }

    protected override void Tick()
    {
        //nothing
    }
}
