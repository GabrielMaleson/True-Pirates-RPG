using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName;
    public int level = 1;
    public int currentHP;
    public int expValue = 50;

    [Header("Stats")]
    public int baseHP = 20;
    public int baseAttack = 5;
    public int baseDefense = 3;

    [Header("Level Progression")]
    public AnimationCurve hpGrowth = AnimationCurve.Linear(1, 1, 10, 5);
    public AnimationCurve attackGrowth = AnimationCurve.Linear(1, 1, 10, 3);
    public AnimationCurve defenseGrowth = AnimationCurve.Linear(1, 1, 10, 2);

    [Header("AP Settings")]
    public int maxAP = 10;

    [Header("Attacks")]
    public List<UnlockableAttack> unlockableAttacks = new List<UnlockableAttack>();

    [Header("Status Effects")]
    public List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();

    [Header("Equipment")]
    public List<DadosItem> equippedItems = new List<DadosItem>();

    // Runtime variables
    public int currentAP { get; set; }
    public int hp { get; set; }
    public int attack { get; set; }
    public int defense { get; set; }
    public int currentExperience { get; set; }
    public List<AttackFile> availableAttacks { get; set; } = new List<AttackFile>();
    public List<AttackFile> selectedActions { get; set; } = new List<AttackFile>();

    // Transform reference for animations (set at runtime)
    public Transform transform { get; set; }

    private void OnEnable()
    {
        CalculateStatsForLevel();
        ResetAP();

        // Populate availableAttacks from unlockableAttacks based on current level
        PopulateAvailableAttacks();
    }

    private void PopulateAvailableAttacks()
    {
        availableAttacks.Clear();

        foreach (var unlockable in unlockableAttacks)
        {
            if (unlockable.unlockLevel <= level && unlockable.attack != null)
            {
                availableAttacks.Add(unlockable.attack);
                Debug.Log($"Added {unlockable.attack.attackName} to available attacks for {characterName}");
            }
        }
    }
    public void ResetAP()
    {
        currentAP = maxAP;
    }

    public void CalculateStatsForLevel()
    {
        hp = baseHP + Mathf.RoundToInt(hpGrowth.Evaluate(level));
        attack = baseAttack + Mathf.RoundToInt(attackGrowth.Evaluate(level));
        defense = baseDefense + Mathf.RoundToInt(defenseGrowth.Evaluate(level));

        // Apply equipment modifiers
        ApplyEquipmentModifiers();

        if (currentHP == 0) // Initialize HP if not set
        {
            currentHP = hp;
        }

        // Make sure attacks are populated
        PopulateAvailableAttacks();
    }
    public void ModifyAttack(int amount)
    {
        attack += amount;
    }

    public void ModifyDefense(int amount)
    {
        defense += amount;
    }
    public void RemoveAllBuffs()
    {
        // Reset stats to base + equipment
        CalculateStatsForLevel();
    }

    private void ApplyEquipmentModifiers()
    {
        foreach (var item in equippedItems)
        {
            if (item != null && item.ehEquipavel)
            {
                foreach (var modifier in item.modificadoresStats)
                {
                    switch (modifier.statType)
                    {
                        case StatType.HP:
                            if (modifier.tipoModificador == ModifierType.Fixo)
                                hp += modifier.valorModificador;
                            else
                                hp += Mathf.RoundToInt(baseHP * (modifier.valorModificador / 100f));
                            break;
                        case StatType.Attack:
                            if (modifier.tipoModificador == ModifierType.Fixo)
                                attack += modifier.valorModificador;
                            else
                                attack += Mathf.RoundToInt(baseAttack * (modifier.valorModificador / 100f));
                            break;
                        case StatType.Defense:
                            if (modifier.tipoModificador == ModifierType.Fixo)
                                defense += modifier.valorModificador;
                            else
                                defense += Mathf.RoundToInt(baseDefense * (modifier.valorModificador / 100f));
                            break;
                    }
                }
            }
        }
    }

    public void LevelUp()
    {
        level++;
        CalculateStatsForLevel();
        PopulateAvailableAttacks(); // Re-populate when leveling up
        CheckForNewAttacks(); // Keep this for any special case logic
    }

    private void CheckForNewAttacks()
    {
        foreach (var unlockable in unlockableAttacks)
        {
            if (unlockable.unlockLevel == level &&
                !availableAttacks.Contains(unlockable.attack))
            {
                availableAttacks.Add(unlockable.attack);
                Debug.Log($"{characterName} learned {unlockable.attack.attackName}!");
            }
        }
    }

    public void GainExperience(int amount)
    {
        currentExperience += amount;

        // Simple level up check (could be expanded)
        int expNeeded = level * 100;
        while (currentExperience >= expNeeded)
        {
            currentExperience -= expNeeded;
            LevelUp();
            expNeeded = level * 100;
        }
    }

    public void TakeDamage(int damage)
    {
        int oldHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        Debug.Log($"TakeDamage: {characterName} took {damage} damage. HP: {oldHP} -> {currentHP}");
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(hp, currentHP + amount);
    }

    // Status Effect Methods
    public void AddStatusEffect(StatusEffectData effect, CharacterData source)
    {
        // Check if effect already exists
        var existing = activeStatusEffects.Find(e => e.effectData == effect);
        if (existing != null)
        {
            existing.remainingDuration = effect.duracaoBase;
            return;
        }

        // Add new effect
        var newEffect = new ActiveStatusEffect(effect, source, this);
        activeStatusEffects.Add(newEffect);

        // Apply immediate stat modifiers
        ApplyStatusModifiers(newEffect);
    }

    private void ApplyStatusModifiers(ActiveStatusEffect effect)
    {
        foreach (var modifier in effect.effectData.modificadoresStats)
        {
            switch (modifier.statType)
            {
                case StatType.Attack:
                    if (modifier.tipoModificador == ModifierType.Fixo)
                        attack += modifier.valorModificador;
                    break;
                case StatType.Defense:
                    if (modifier.tipoModificador == ModifierType.Fixo)
                        defense += modifier.valorModificador;
                    break;
                    // Add other stat types as needed
            }
        }
    }

    private void RemoveStatusModifiers(ActiveStatusEffect effect)
    {
        foreach (var modifier in effect.effectData.modificadoresStats)
        {
            switch (modifier.statType)
            {
                case StatType.Attack:
                    if (modifier.tipoModificador == ModifierType.Fixo)
                        attack -= modifier.valorModificador;
                    break;
                case StatType.Defense:
                    if (modifier.tipoModificador == ModifierType.Fixo)
                        defense -= modifier.valorModificador;
                    break;
            }
        }
    }

    public void ProcessStatusEffectsOnTurnStart()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            activeStatusEffects[i].OnTurnStart();
            if (activeStatusEffects[i].isExpired)
            {
                RemoveStatusModifiers(activeStatusEffects[i]);
                activeStatusEffects.RemoveAt(i);
            }
        }
    }

    public bool CanAct()
    {
        // Check if any status effect prevents actions
        foreach (var effect in activeStatusEffects)
        {
            if (effect.effectData.impedeAcoes)
                return false;
        }
        return currentHP > 0;
    }

    // Equipment Methods
    public bool EquipItem(DadosItem item)
    {
        if (!item.ehEquipavel) return false;

        // Check level requirement
        if (item.nivelRequerido > level) return false;

        // Check if slot is already occupied
        var existingItem = equippedItems.Find(i => i != null && i.slotEquipamento == item.slotEquipamento);
        if (existingItem != null)
        {
            UnequipItem(existingItem);
        }

        equippedItems.Add(item);
        CalculateStatsForLevel(); // Recalculate stats with new equipment
        return true;
    }

    public void UnequipItem(DadosItem item)
    {
        equippedItems.Remove(item);
        CalculateStatsForLevel(); // Recalculate stats without the item
    }

    // Consumable Methods
    public bool UseConsumable(DadosItem item)
    {
        if (!item.ehConsumivel || !item.usavelEmBatalha) return false;

        foreach (var effect in item.efeitos)
        {
            switch (effect.tipoEfeito)
            {
                case EffectType.Heal:
                    Heal(effect.valor);
                    break;
                    // Add other consumable effect types as needed
            }
        }

        return true;
    }
}

[System.Serializable]
public class UnlockableAttack
{
    public AttackFile attack;
    public int unlockLevel;
}