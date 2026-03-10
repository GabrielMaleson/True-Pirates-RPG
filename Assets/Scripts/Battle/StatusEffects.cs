using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Status Effect", menuName = "RPG/Status Effect")]
public class StatusEffectData : ScriptableObject
{
    [Header("Identification")]
    public string effectName;
    public StatusEffectType effectType;
    public Sprite icon;

    [Header("Duration")]
    public int baseDuration = 3;
    public bool isPermanent;
    public bool canBeDispelled = true;

    [Header("Effects")]
    public List<StatusEffectStage> effectStages;

    [Header("Visuals")]
    public GameObject effectVFX;
    public AudioClip applySound;
    public AudioClip tickSound;
    public AudioClip expireSound;
}

[System.Serializable]
public class StatusEffectStage
{
    public int stageNumber = 1;
    public string stageName = "Stage 1";

    [Header("Stat Modifiers")]
    public List<StatModifier> statModifiers;

    [Header("Damage Over Time")]
    public int damagePerTurn;

    [Header("Heal Over Time")]
    public int healPerTurn;

    [Header("Special Effects")]
    public bool preventsActions;
    public bool preventsMovement;
    public float damageReceivedMultiplier = 1f;
    public float damageDealtMultiplier = 1f;

    [Header("Visual")]
    public Color tintColor = Color.white;
}

public enum StatusEffectType
{
    // Negative Effects
    Poison,
    Burn,
    Frostbite,
    Stun,
    Sleep,
    Paralysis,
    Confusion,
    Silence,
    Curse,
    Bleed,

    // Positive Effects
    Regeneration,
    Haste,
    Fortify,
    Bravery,
    Faith,
    Protect,
    Shell,
    Regen,

    // Neutral/Debuffs
    AttackDown,
    DefenseDown,
    SpeedDown,
    MagicDown
}
[System.Serializable]
public class ActiveStatusEffect
{
    public StatusEffectData effectData;
    public int remainingDuration;
    public int currentStage;

    [System.NonSerialized] // Don't serialize this - prevents circular reference
    public PartyMemberState source;

    [System.NonSerialized] // Don't serialize this - prevents circular reference
    public PartyMemberState target;

    public bool isExpired;


    // Track stat modifications for removal
    private Dictionary<StatType, int> appliedModifiers = new Dictionary<StatType, int>();

    public ActiveStatusEffect(StatusEffectData data, PartyMemberState source, PartyMemberState target)
    {
        this.effectData = data;
        this.remainingDuration = data.baseDuration;
        this.currentStage = 0;
        this.source = source;
        this.target = target;
    }

    public void ApplyEffect()
    {
        if (effectData.effectStages.Count > 0 && target != null)
        {
            var stage = effectData.effectStages[currentStage];

            // Apply stat modifiers
            foreach (var modifier in stage.statModifiers)
            {
                int modifierValue = modifier.valorModificador;

                switch (modifier.statType)
                {
                    case StatType.Attack:
                        target.ModifyAttack(modifierValue);
                        appliedModifiers[StatType.Attack] = modifierValue;
                        break;
                    case StatType.Defense:
                        target.ModifyDefense(modifierValue);
                        appliedModifiers[StatType.Defense] = modifierValue;
                        break;
                        // Add other stat types as needed
                }
            }
        }
    }

    public void OnTurnStart()
    {
        if (target == null || remainingDuration <= 0)
        {
            isExpired = true;
            return;
        }

        if (effectData.effectStages.Count > 0)
        {
            var stage = effectData.effectStages[currentStage];

            // Process DOT
            if (stage.damagePerTurn > 0 && target.currentHP > 0)
            {
                int damage = stage.damagePerTurn;
                target.TakeDamage(damage);
                Debug.Log($"{target.CharacterName} takes {damage} damage from {effectData.effectName}!");
            }

            // Process HOT
            if (stage.healPerTurn > 0 && target.currentHP > 0)
            {
                int heal = stage.healPerTurn;
                target.Heal(heal);
                Debug.Log($"{target.CharacterName} heals {heal} from {effectData.effectName}!");
            }
        }

        remainingDuration--;

        // Check for stage progression (some effects get stronger over time)
        if (effectData.effectStages.Count > currentStage + 1)
        {
            // Progress to next stage when duration reaches certain thresholds
            if (remainingDuration <= effectData.baseDuration * 0.5f)
            {
                AdvanceToNextStage();
            }
        }
    }

    public void OnTurnEnd()
    {
        // For effects that trigger at end of turn
    }

    public void AdvanceToNextStage()
    {
        if (effectData.effectStages.Count > currentStage + 1)
        {
            // Remove old stage modifiers
            RemoveModifiers();

            // Advance to next stage
            currentStage++;

            // Apply new stage modifiers
            ApplyEffect();

            Debug.Log($"{effectData.effectName} advanced to stage {currentStage + 1} on {target.CharacterName}");
        }
    }

    private void RemoveModifiers()
    {
        if (target == null) return;

        foreach (var kvp in appliedModifiers)
        {
            switch (kvp.Key)
            {
                case StatType.Attack:
                    target.ModifyAttack(-kvp.Value);
                    break;
                case StatType.Defense:
                    target.ModifyDefense(-kvp.Value);
                    break;
            }
        }
        appliedModifiers.Clear();
    }

    public void Remove()
    {
        RemoveModifiers();
        isExpired = true;

        if (target != null)
        {
            Debug.Log($"{effectData.effectName} removed from {target.CharacterName}");
        }
    }
}