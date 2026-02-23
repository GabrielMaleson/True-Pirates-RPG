using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Novo Item", menuName = "RPG/Item")]
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

    // New additions - Equipment Properties
    [Header("Tipo de Equipamento (Opcional)")]
    public bool ehEquipavel;
    public EquipmentSlot slotEquipamento;
    public bool ehDuasMaos;

    [Header("Modificadores de Stat (para Equipamentos)")]
    public List<StatModifier> modificadoresStats;

    [Header("Requisitos")]
    public int nivelRequerido;
    public string classeRequerida;

    // New additions - Consumable Properties
    [Header("Tipo de Consumível (Opcional)")]
    public bool ehConsumivel;
    public ConsumableType tipoConsumivel;
    public bool usavelEmBatalha = true;
    public bool usavelNoMapa = true;

    [Header("Efeitos (para Consumíveis)")]
    public List<EfeitoItem> efeitos;
}

// Supporting enums and classes
public enum EquipmentSlot
{
    Arma,
    Escudo,
    Capacete,
    Armadura,
    Luvas,
    Botas,
    Acessorio1,
    Acessorio2
}

public enum ConsumableType
{
    Cura,
    RestauracaoMana,
    CuraStatus,
    Buff,
    Debuff,
    Reviver,
    Especial
}

public enum StatType
{
    HP,
    Attack,
    Defense,
    Speed,
    Magic,
    MagicDefense
}

[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public int valorModificador;
    public ModifierType tipoModificador;
}

public enum ModifierType
{
    Fixo,        // +5 Attack
    Percentual   // +10% Attack
}

[System.Serializable]
public class EfeitoItem
{
    public EffectType tipoEfeito;
    public int valor;
    public int duracao; // Para efeitos de status
}