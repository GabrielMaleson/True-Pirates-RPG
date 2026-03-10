using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PartyMemberState
{
    [Header("Template Reference")]
    public CharacterData template; // The base character data (read-only)

    [Header("Current Stats")]
    public int level = 1;
    public int currentHP;
    public int currentAP;
    public int currentExperience;

    [Header("Equipment")]
    public DadosItem weapon;
    public DadosItem armor;

    [Header("Learned Attacks")]
    public List<AttackFile> learnedAttacks = new List<AttackFile>();

    [Header("Status Effects")]
    public List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();

    [System.NonSerialized] // Don't save this
    public Transform transform; // For animations and position

    // Computed stats (from template + equipment)
    public int MaxHP
    {
        get
        {
            int baseHP = template != null ? template.GetHPForLevel(level) : 1;
            int weaponBonus = GetEquipmentStatBonus(weapon, StatType.HP);
            int armorBonus = GetEquipmentStatBonus(armor, StatType.HP);
            return baseHP + weaponBonus + armorBonus;
        }
    }

    public int Attack
    {
        get
        {
            int baseAttack = template != null ? template.GetAttackForLevel(level) : 1;
            int weaponBonus = GetEquipmentStatBonus(weapon, StatType.Attack);
            int armorBonus = GetEquipmentStatBonus(armor, StatType.Attack);
            return baseAttack + weaponBonus + armorBonus;
        }
    }

    public int Defense
    {
        get
        {
            int baseDefense = template != null ? template.GetDefenseForLevel(level) : 1;
            int weaponBonus = GetEquipmentStatBonus(weapon, StatType.Defense);
            int armorBonus = GetEquipmentStatBonus(armor, StatType.Defense);
            return baseDefense + weaponBonus + armorBonus;
        }
    }

    public int MaxAP
    {
        get { return template != null ? template.baseMaxAP : 10; }
    }

    public string CharacterName
    {
        get { return template != null ? template.characterName : "Unknown"; }
    }

    public Sprite PartyIcon
    {
        get { return template != null ? template.partyIcon : null; }
    }

    public Sprite BattlePortrait
    {
        get { return template != null ? template.battlePortrait : null; }
    }

    // Constructor
    public PartyMemberState(CharacterData templateData, int startLevel = 1)
    {
        template = templateData;
        level = startLevel;
        currentHP = MaxHP;
        currentAP = MaxAP;
        currentExperience = 0;

        // Learn initial attacks
        RefreshLearnedAttacks();
    }

    // Refresh learned attacks based on level
    public void RefreshLearnedAttacks()
    {
        if (template != null)
        {
            learnedAttacks = template.GetAttacksForLevel(level);
        }
    }

    // Equipment stat bonus helper
    private int GetEquipmentStatBonus(DadosItem item, StatType statType)
    {
        if (item == null || !item.ehEquipavel) return 0;

        foreach (var mod in item.modificadoresStats)
        {
            if (mod.statType == statType)
            {
                if (mod.tipoModificador == ModifierType.Fixo)
                    return mod.valorModificador;
                else // Percentual
                {
                    int baseValue = 0;
                    switch (statType)
                    {
                        case StatType.HP:
                            baseValue = template != null ? template.GetHPForLevel(level) : 1;
                            break;
                        case StatType.Attack:
                            baseValue = template != null ? template.GetAttackForLevel(level) : 1;
                            break;
                        case StatType.Defense:
                            baseValue = template != null ? template.GetDefenseForLevel(level) : 1;
                            break;
                    }
                    return Mathf.RoundToInt(baseValue * (mod.valorModificador / 100f));
                }
            }
        }
        return 0;
    }

    // Level up
    public void LevelUp()
    {
        level++;
        RefreshLearnedAttacks();
        currentHP = MaxHP; // Full heal on level up
        currentAP = MaxAP;
    }

    // Gain experience
    public void GainExperience(int amount)
    {
        currentExperience += amount;

        if (template == null) return;

        int expNeeded = template.GetExpForLevel(level);
        while (currentExperience >= expNeeded && level < 100) // Cap at level 100
        {
            currentExperience -= expNeeded;
            LevelUp();
            expNeeded = template.GetExpForLevel(level);
        }
    }

    // Combat methods
    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    public void ResetAP()
    {
        currentAP = MaxAP;
    }
    public bool CanAct()
    {
        foreach (var effect in activeStatusEffects)
        {
            if (effect.effectData != null && effect.effectData.effectStages.Count > 0)
            {
                // Check the current stage of the effect
                var currentStage = effect.effectData.effectStages[effect.currentStage];
                if (currentStage.preventsActions)
                    return false;
            }
        }
        return currentHP > 0;
    }

    // Equipment methods
    public bool EquipWeapon(DadosItem weaponItem)
    {
        if (weaponItem == null || !weaponItem.ehEquipavel || weaponItem.slotEquipamento != EquipmentSlot.Arma)
            return false;

        if (weaponItem.nivelRequerido > level)
            return false;

        // Unequip current weapon and add to inventory
        if (weapon != null)
        {
            // Add back to inventory logic will be handled by calling method
        }

        weapon = weaponItem;
        return true;
    }

    public bool EquipArmor(DadosItem armorItem)
    {
        if (armorItem == null || !armorItem.ehEquipavel || armorItem.slotEquipamento != EquipmentSlot.Armadura)
            return false;

        if (armorItem.nivelRequerido > level)
            return false;

        if (armor != null)
        {
            // Add back to inventory logic will be handled by calling method
        }

        armor = armorItem;
        return true;
    }

    public void UnequipWeapon()
    {
        weapon = null;
    }

    public void UnequipArmor()
    {
        armor = null;
    }

    public bool UseConsumable(DadosItem item)
    {
        if (!item.ehConsumivel || !item.usavelEmBatalha) return false;

        foreach (var effect in item.efeitos)
        {
            switch (effect.tipoEfeito)
            {
                case EffectType.Heal:
                case EffectType.HP_Restore:
                    Heal(effect.valor);
                    break;
                case EffectType.ManaRestore:
                    currentAP = Mathf.Min(MaxAP, currentAP + effect.valor);
                    break;
                case EffectType.Revive:
                    if (currentHP <= 0)
                    {
                        currentHP = effect.valor;
                    }
                    break;
                    // Add other consumable effect types as needed
            }
        }

        return true;
    }


    public void AddStatusEffect(StatusEffectData effect, PartyMemberState source)
    {
        // Check if effect already exists
        var existing = activeStatusEffects.Find(e => e.effectData == effect);
        if (existing != null)
        {
            existing.remainingDuration = effect.baseDuration; // Fixed: was duracaoBase
            return;
        }

        // Add new effect
        var newEffect = new ActiveStatusEffect(effect, source, this);
        activeStatusEffects.Add(newEffect);

        // Apply immediate stat modifiers
        newEffect.ApplyEffect();
    }

    public void ProcessStatusEffectsOnTurnStart()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeStatusEffects[i];

            // Process the effect (damage, heal, etc.)
            effect.OnTurnStart();

            // Check if we should advance to next stage
            if (effect.effectData.effectStages.Count > effect.currentStage + 1)
            {
                // Example: advance when half duration remaining
                if (effect.remainingDuration <= effect.effectData.baseDuration * 0.5f)
                {
                    effect.AdvanceToNextStage();
                }
            }

            if (effect.isExpired)
            {
                activeStatusEffects.RemoveAt(i);
            }
        }
    }
    // Modify stats methods (for buffs/debuffs)
    public void ModifyAttack(int amount)
    {
        // This is a temporary modification - you might want to track buffs separately
        // For now, we'll just note that this method exists
        Debug.Log($"Attack modified by {amount}");
    }

    public void ModifyDefense(int amount)
    {
        Debug.Log($"Defense modified by {amount}");
    }

    public void RecalculateStats()
    {
        // This triggers the property getters to recalculate
        // No actual logic needed since properties calculate on the fly
    }

    // EXP value for defeating this character
    public int GetExpValue()
    {
        return template != null ? template.GetExpValueForLevel(level) : 50;
    }
}