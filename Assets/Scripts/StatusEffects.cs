using UnityEngine;
using System.Collections.Generic;

// Simplified Status Effects that work with existing systems
[CreateAssetMenu(fileName = "Novo Status", menuName = "Sistema RPG/Status Effect")]
public class StatusEffectData : ScriptableObject
{
    [Header("Identificação")]
    public string nomeEfeito;
    public StatusEffectType tipoEfeito;
    public Sprite icone;

    [Header("Duração")]
    public int duracaoBase = 3;
    public bool ehPermanente;

    [Header("Efeitos")]
    public int danoPorTurno;
    public int curaPorTurno;
    public List<StatModifier> modificadoresStats;

    [Header("Efeitos Especiais")]
    public bool impedeAcoes;
    public float multiplicadorDanoRecebido = 1f;
    public float multiplicadorDanoCausado = 1f;
}

public enum StatusEffectType
{
    // Efeitos Negativos
    Veneno,
    Queimadura,
    Paralisia,
    Sono,
    Silencio,
    Medo,

    // Efeitos Positivos
    Regeneracao,
    Escudo,
    Forca,
    Agilidade,

    // Debuffs
    AtaqueReduzido,
    DefesaReduzida,
    VelocidadeReduzida
}

// Runtime status effect instance
[System.Serializable]
public class ActiveStatusEffect
{
    public StatusEffectData effectData;
    public int remainingDuration;
    public CharacterData source;
    public CharacterData target;
    public bool isExpired;

    public ActiveStatusEffect(StatusEffectData data, CharacterData source, CharacterData target)
    {
        this.effectData = data;
        this.remainingDuration = data.duracaoBase;
        this.source = source;
        this.target = target;
    }

    public void OnTurnStart()
    {
        if (remainingDuration <= 0)
        {
            isExpired = true;
            return;
        }

        // Process DOT/HOT
        if (effectData.danoPorTurno > 0)
        {
            target.TakeDamage(effectData.danoPorTurno);
            Debug.Log($"{target.characterName} takes {effectData.danoPorTurno} damage from {effectData.nomeEfeito}!");
        }

        if (effectData.curaPorTurno > 0)
        {
            target.Heal(effectData.curaPorTurno);
            Debug.Log($"{target.characterName} heals {effectData.curaPorTurno} from {effectData.nomeEfeito}!");
        }

        remainingDuration--;
    }

    public void OnTurnEnd()
    {
        // For effects that trigger at end of turn
    }

    public void Remove()
    {
        isExpired = true;
    }
}