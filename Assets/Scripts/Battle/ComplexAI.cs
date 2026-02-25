using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class ComplexAI : MonoBehaviour
{
    public CharacterComponent comp;
    [Header("AI Configuration")]
    private CharacterData controlledCharacter;
    public AIStrategy currentStrategy = AIStrategy.Balanced;

    [Header("Behavior Weights")]
    [Range(0, 1)] public float offensiveWeight = 0.5f;
    [Range(0, 1)] public float defensiveWeight = 0.3f;
    [Range(0, 1)] public float supportiveWeight = 0.2f;

    [Header("Targeting Preferences")]
    public bool focusLowestHP = true;
    public bool focusHighestThreat = false;

    private Dictionary<CharacterData, float> threatLevels = new Dictionary<CharacterData, float>();

    private void Start()
    {
        comp = GetComponent<CharacterComponent>();
        controlledCharacter = comp.characterData;
    }

    public AIDecision MakeDecision(List<CharacterData> partyMembers, List<CharacterData> enemies)
    {
        var decision = new AIDecision();

        // Evaluate current situation
        var evaluation = EvaluateBattleState(partyMembers, enemies);

        // Adjust strategy based on evaluation
        AdjustStrategy(evaluation);

        // Select action based on current strategy
        decision.selectedAction = SelectAction(enemies);

        // Select targets based on action
        decision.targets = SelectTargets(decision.selectedAction, partyMembers, enemies);

        return decision;
    }

    private BattleEvaluation EvaluateBattleState(List<CharacterData> partyMembers, List<CharacterData> enemies)
    {
        var evaluation = new BattleEvaluation();

        // Calculate party health percentage
        evaluation.partyHealthPercentage = partyMembers
            .Average(p => (float)p.currentHP / p.hp);

        // Calculate enemy health percentage
        evaluation.enemyHealthPercentage = enemies
            .Where(e => e != null)
            .Average(e => (float)e.currentHP / e.hp);

        // Count active enemies
        evaluation.activeEnemyCount = enemies.Count(e => e.currentHP > 0);

        // Check for low HP allies
        evaluation.lowHPAllies = partyMembers
            .Where(p => (float)p.currentHP / p.hp < 0.3f)
            .ToList();

        return evaluation;
    }

    private void AdjustStrategy(BattleEvaluation evaluation)
    {
        // Emergency healing needed
        if (evaluation.lowHPAllies.Count > 0)
        {
            currentStrategy = AIStrategy.Defensive;
            supportiveWeight = 0.7f;
            offensiveWeight = 0.2f;
            defensiveWeight = 0.1f;
        }
        // Strong advantage
        else if (evaluation.enemyHealthPercentage < 0.2f)
        {
            currentStrategy = AIStrategy.Aggressive;
            offensiveWeight = 0.8f;
            supportiveWeight = 0.1f;
            defensiveWeight = 0.1f;
        }
        // Default to balanced
        else
        {
            currentStrategy = AIStrategy.Balanced;
            offensiveWeight = 0.5f;
            defensiveWeight = 0.3f;
            supportiveWeight = 0.2f;
        }
    }

    private AttackFile SelectAction(List<CharacterData> enemies)
    {
        if (controlledCharacter == null || controlledCharacter.availableAttacks == null)
            return null;

        List<AttackFile> availableActions = controlledCharacter.availableAttacks
            .Where(a => a != null && a.actionPointCost <= controlledCharacter.currentAP)
            .ToList();

        if (availableActions.Count == 0)
            return null;

        // Score each action based on current weights
        Dictionary<AttackFile, float> actionScores = new Dictionary<AttackFile, float>();

        foreach (var action in availableActions)
        {
            float score = 0f;

            // Calculate offensive score
            if (action.effects.Any(e => e.targetType == TargetType.Enemy))
            {
                float damagePotential = action.effects
                    .Where(e => e.targetType == TargetType.Enemy)
                    .Sum(e => e.value);

                score += damagePotential * offensiveWeight;
            }

            // Calculate defensive/supportive score
            if (action.effects.Any(e => e.targetType == TargetType.Ally))
            {
                float healPotential = action.effects
                    .Where(e => e.effectType == EffectType.Heal)
                    .Sum(e => e.value);

                score += healPotential * (defensiveWeight + supportiveWeight);
            }

            // Adjust for AP efficiency
            if (action.actionPointCost > 0)
                score /= action.actionPointCost;

            actionScores[action] = score;
        }

        // Select best action
        return actionScores.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    private List<CharacterData> SelectTargets(AttackFile action, List<CharacterData> partyMembers, List<CharacterData> enemies)
    {
        List<CharacterData> targets = new List<CharacterData>();

        if (action == null || action.effects.Count == 0)
            return targets;

        foreach (var effect in action.effects)
        {
            List<CharacterData> potentialTargets = effect.targetType == TargetType.Ally
                ? partyMembers.Where(p => p.currentHP > 0).ToList()
                : enemies.Where(e => e.currentHP > 0).ToList();

            if (potentialTargets.Count == 0)
                continue;

            // Smart targeting based on effect type
            switch (effect.effectType)
            {
                case EffectType.Damage:
                case EffectType.Attack:
                    if (focusLowestHP)
                    {
                        // Target lowest HP enemy
                        var target = potentialTargets
                            .OrderBy(t => t.currentHP)
                            .FirstOrDefault();
                        if (target != null) targets.Add(target);
                    }
                    else
                    {
                        // Random target
                        int randomIndex = Random.Range(0, potentialTargets.Count);
                        targets.Add(potentialTargets[randomIndex]);
                    }
                    break;

                case EffectType.Heal:
                    // Target lowest HP ally
                    var healTarget = potentialTargets
                        .OrderBy(t => (float)t.currentHP / t.hp)
                        .FirstOrDefault();
                    if (healTarget != null) targets.Add(healTarget);
                    break;

                default:
                    // Random target for other effects
                    if (potentialTargets.Count > 0)
                    {
                        int randomIndex = Random.Range(0, potentialTargets.Count);
                        targets.Add(potentialTargets[randomIndex]);
                    }
                    break;
            }
        }

        return targets;
    }
}

// Supporting classes
public class BattleEvaluation
{
    public float partyHealthPercentage;
    public float enemyHealthPercentage;
    public int activeEnemyCount;
    public List<CharacterData> lowHPAllies = new List<CharacterData>();
}

public class AIDecision
{
    public AttackFile selectedAction;
    public List<CharacterData> targets = new List<CharacterData>();
}

public enum AIStrategy
{
    Aggressive,
    Defensive,
    Supportive,
    Balanced
}