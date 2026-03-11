using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Novo Item", menuName = "Sistema RPG/Item")]
public class DadosItem : ScriptableObject
{
    [Header("Identificação")]
    public string nomeDoItem;
    public string id;

    [Header("Visual")]
    public Sprite icone;

    [TextArea]
    public string descricao;

    [Header("Economia e Logistica")]
    public int valorEmOuro;
    public bool ehEmpilhavel;

    // Equipment Properties
    [Header("Tipo de Equipamento (Opcional)")]
    public bool ehEquipavel;
    public EquipmentSlot slotEquipamento; // Will be Arma or Armadura only

    [Header("Modificadores de Stat (para Equipamentos)")]
    public List<StatModifier> modificadoresStats;

    [Header("Requisitos")]
    public int nivelRequerido;
    // Removed classeRequerida as it's not used in current system

    // Consumable Properties
    [Header("Tipo de Consumível (Opcional)")]
    public bool ehConsumivel;
    public ConsumableType tipoConsumivel;
    public bool usavelEmBatalha = true;
    public bool usavelNoMapa = true;

    [Header("Efeitos (para Consumíveis)")]
    public List<ItemEffectData> efeitos;
}

// Item effect data that matches combat system
[System.Serializable]
public class ItemEffectData
{
    public EffectType tipoEfeito; // Uses the same EffectType as attacks
    public int valor;
    public int duracao; // For status effects

    // Targeting properties (matches combat system)
    public TargetType targetType = TargetType.Ally;
    public int numberOfTargets = 1;
    [Range(0, 100)]
    public int accuracy = 100;
    public EffectTrigger triggersOn = EffectTrigger.OnSuccess;

    // Status effect reference
    public StatusEffectData statusEffect;
}

// Simplified Equipment Slots - Only what we use
public enum EquipmentSlot
{
    Arma,      // Weapon slot
    Armadura   // Armor slot
}

// Consumable types
public enum ConsumableType
{
    Cura,              // Healing
    RestauracaoMana,   // AP Restore
    CuraStatus,        // Status Cure
    Buff,              // Temporary stat boost
    Debuff,            // Temporary stat reduction
    Reviver,           // Revive fallen character
    Especial           // Special effects
}

// Stat types that match PartyMemberState properties
public enum StatType
{
    HP,        // MaxHP
    Attack,    // Attack
    Defense,   // Defense
    AP         // MaxAP (for mana/AP potions)
    // Removed Speed, Magic, MagicDefense as they're not in current combat system
}

// Stat modifier structure
[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public int valorModificador;
    public ModifierType tipoModificador;
}

// Modifier type
public enum ModifierType
{
    Fixo,        // +5 Attack (adds directly)
    Percentual   // +10% Attack (percentage of base stat)
}