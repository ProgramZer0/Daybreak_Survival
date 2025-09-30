using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "LifeStyles", menuName = "LifeStyles")]
public class LifeStyles : ScriptableObject
{
    [Header("Main Settings")]
    public string lifestyleName;
    public int id;
    public string scriptName;
    public Rarity rarity = Rarity.common;
    public GameObject lifestyleObj;
    public Sprite displaySpirte;
    [TextArea]
    public string description;

    [Header("Complex Lifestyle Settings")]
    public bool lifestyleTriggeredEveryFrame = false;
    public bool hasLimit = false;
    public int triggerLimit = 1;
    public float eachTriggerTime = 0;

    [Header("Basic Lifestyle Settings")]
    public float ModNormSpeed = 0f;
    public float ModSprintSpeed = 0f;
    public float ModCrouchSpeed = 0f;
    public float ModDashCooldown = 0f;
    public float ModDashRange = 0f;
    public float ModDashOffset = 0f;
    public float ModSprintTime = 0f;
    public float ModSprintCooldown = 0f;
    public float ModMaxSprintDebuffTime = 0f;
    public float ModInteractRange = 0f;
    public float ModPickupRange = 0f;
    public float ModHordeForgetTime = 0f;
    public float ModMaxHP = 0f;
    public float ModSeeDistance = 0f;
    public float ModlightDistance = 0f;
    public bool hasNightVison = false;
    public float ModEnemySeeRange = 0f;
    public float ModLoudness = 0f;
}
