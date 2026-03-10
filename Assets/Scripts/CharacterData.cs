using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;

    [Header("Portrait")]
    public Sprite partyIcon;
    public Sprite battlePortrait;

    [Header("Base Stats")]
    public int baseHP = 20;
    public int baseAttack = 5;
    public int baseDefense = 3;
    public int baseMaxAP = 10;

    [Header("Level Progression")]
    public AnimationCurve hpGrowth = AnimationCurve.Linear(1, 1, 10, 5);
    public AnimationCurve attackGrowth = AnimationCurve.Linear(1, 1, 10, 3);
    public AnimationCurve defenseGrowth = AnimationCurve.Linear(1, 1, 10, 2);
    public AnimationCurve expRequired = AnimationCurve.Linear(1, 100, 10, 1000);

    [Header("Attacks")]
    public List<UnlockableAttack> unlockableAttacks = new List<UnlockableAttack>();

    [Header("Base EXP Value")]
    public int baseExpValue = 50;

    // Calculate stats for a given level
    public int GetHPForLevel(int level)
    {
        return baseHP + Mathf.RoundToInt(hpGrowth.Evaluate(level));
    }

    public int GetAttackForLevel(int level)
    {
        return baseAttack + Mathf.RoundToInt(attackGrowth.Evaluate(level));
    }

    public int GetDefenseForLevel(int level)
    {
        return baseDefense + Mathf.RoundToInt(defenseGrowth.Evaluate(level));
    }

    public int GetExpForLevel(int level)
    {
        return Mathf.RoundToInt(expRequired.Evaluate(level));
    }

    public int GetExpValueForLevel(int level)
    {
        return baseExpValue + (level * 10);
    }

    // Get attacks available at a given level
    public List<AttackFile> GetAttacksForLevel(int level)
    {
        List<AttackFile> attacks = new List<AttackFile>();
        foreach (var unlockable in unlockableAttacks)
        {
            if (unlockable.unlockLevel <= level && unlockable.attack != null)
            {
                attacks.Add(unlockable.attack);
            }
        }
        return attacks;
    }
}

[System.Serializable]
public class UnlockableAttack
{
    public AttackFile attack;
    public int unlockLevel;
}