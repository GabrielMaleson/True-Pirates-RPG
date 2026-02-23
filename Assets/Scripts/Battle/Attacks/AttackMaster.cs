using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
    Damage,
    Heal,
    Attack,
    // Add more effect types as needed
}
[CreateAssetMenu(fileName = "NewAttack", menuName = "RPG/Attack File")]
public class AttackFile : ScriptableObject
{
    public string attackName;
    public bool partyMemberOnly = true;
    public int actionPointCost = 2;
    public List<EffectData> effects = new List<EffectData>();

    [Header("Animation")]
    public BattleAnimationData battleAnimation; // Add this directly to AttackFile
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
}

// Add TargetingMode here since it wasn't defined globally
public enum TargetingMode
{
    Single,
    AllEnemies,
    AllAllies,
    Self
}
