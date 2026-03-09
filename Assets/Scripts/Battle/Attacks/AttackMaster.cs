using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
    Damage,
    Heal,
    Attack,
    HP_Restore,
    StatusEffect,
    Buff,
    Debuff,
    ManaRestore,
    Revive,
    MultiHit
    // Add more as needed
}
[CreateAssetMenu(fileName = "NewAttack", menuName = "RPG/Attack File")]
public class AttackFile : ScriptableObject
{
    public string attackName;

    [TextArea(3, 5)] // Makes a multi-line text field in inspector
    public string description; // Attack description

    public Sprite icon; // NEW: Icon for the attack

    public bool partyMemberOnly = true;
    public int actionPointCost = 2;
    public List<EffectData> effects = new List<EffectData>();

    [Header("Animation")]
    public BattleAnimationData battleAnimation;
}

[System.Serializable]
public class EffectData
{
    public EffectType effectType;
    public TargetType targetType;
    public int numberOfTargets = 1;
    public int value;
    [Range(0, 100)]
    public int accuracy = 100;
    public EffectTrigger triggersOn = EffectTrigger.OnSuccess;

    // New fields for advanced effects
    public StatusEffectData statusEffect; // For status effect applications
    public List<StatModifier> statModifiers; // For buffs/debuffs
    public int hitCount = 1; // For multi-hit attacks
    public float damageMultiplier = 1f; // For scaling damage
}
public enum TargetingMode
{
    Single,
    AllEnemies,
    AllAllies,
    Self
}
