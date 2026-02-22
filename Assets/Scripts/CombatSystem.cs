using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CombatState
{
    STARTING,
    PLAYER_TURN,
    ENEMY_TURN,
    VICTORY,
    DEFEAT
}

public enum TargetType
{
    Ally,
    Enemy
}

public enum EffectTrigger
{
    OnSuccess,
    OnMiss
}

public class CombatSystem : MonoBehaviour
{
    [Header("Combatants")]
    public List<CharacterData> partyMembers = new List<CharacterData>();
    public List<CharacterData> enemies = new List<CharacterData>();

    [Header("Combat State")]
    public CombatState currentState = CombatState.STARTING;

    private Queue<CharacterData> turnQueue = new Queue<CharacterData>();
    private CharacterData currentCharacter;
    private bool isExecutingActions = false;

    private void Start()
    {
        InitializeCombat();
    }

    private void InitializeCombat()
    {
        // Filter out any DOWNED characters at start
        partyMembers = partyMembers.Where(p => p.currentHP > 0).ToList();
        enemies = enemies.Where(e => e.currentHP > 0).ToList();

        currentState = CombatState.PLAYER_TURN;
        BuildTurnQueue();
        StartNextTurn();
    }

    private void BuildTurnQueue()
    {
        turnQueue.Clear();

        // Party members go first in their current order
        foreach (var member in partyMembers)
        {
            if (member.currentHP > 0)
            {
                member.ResetAP();
                turnQueue.Enqueue(member);
            }
        }

        // Enemies go after
        foreach (var enemy in enemies)
        {
            if (enemy.currentHP > 0)
            {
                enemy.ResetAP();
                turnQueue.Enqueue(enemy);
            }
        }
    }

    private void StartNextTurn()
    {
        if (CheckBattleEnd())
            return;

        if (turnQueue.Count == 0)
        {
            // All characters have taken turns, rebuild queue for next round
            BuildTurnQueue();
        }

        currentCharacter = turnQueue.Dequeue();

        // Skip DOWNED characters
        if (currentCharacter.currentHP <= 0)
        {
            StartNextTurn();
            return;
        }

        Debug.Log($"Starting turn for: {currentCharacter.characterName}");

        // Determine turn type
        if (partyMembers.Contains(currentCharacter))
        {
            currentState = CombatState.PLAYER_TURN;
            StartCoroutine(PlayerTurnRoutine());
        }
        else
        {
            currentState = CombatState.ENEMY_TURN;
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    private IEnumerator PlayerTurnRoutine()
    {
        // Process status effects at start of turn
        currentCharacter.ProcessStatusEffectsOnTurnStart();

        if (!currentCharacter.CanAct())
        {
            Debug.Log($"{currentCharacter.characterName} is unable to act!");
            yield return new WaitForSeconds(1f);
            StartNextTurn();
            yield break;
        }
        while (currentCharacter.currentAP > 0 && !isExecutingActions)
        {
            yield return null; // Player selects actions via UI
        }

        // Process selected actions in order (highest AP cost first)
        ExecuteActionsInOrder();
    }

    private IEnumerator EnemyTurnRoutine()
    {
        // Process status effects at start of turn
        currentCharacter.ProcessStatusEffectsOnTurnStart();

        if (!currentCharacter.CanAct())
        {
            Debug.Log($"{currentCharacter.characterName} is unable to act!");
            yield return new WaitForSeconds(1f);
            StartNextTurn();
            yield break;
        }
        // Simple AI: select highest damage action
        AttackFile selectedAction = GetHighestDamageAction(currentCharacter);

        if (selectedAction != null && currentCharacter.currentAP >= selectedAction.actionPointCost)
        {
            // Execute enemy action
            ExecuteAttack(currentCharacter, selectedAction);
            currentCharacter.currentAP -= selectedAction.actionPointCost;
        }

        yield return new WaitForSeconds(1f); // Brief pause between turns

        // End enemy turn
        StartNextTurn();
    }

    private AttackFile GetHighestDamageAction(CharacterData character)
    {
        AttackFile highestDamageAction = null;
        int highestDamage = 0;

        foreach (var attack in character.availableAttacks)
        {
            if (attack.partyMemberOnly)
                continue; // Skip party-only attacks for enemies

            if (character.currentAP < attack.actionPointCost)
                continue; // Skip if not enough AP

            // Calculate total potential damage from all effects
            int totalDamage = 0;
            foreach (var effect in attack.effects)
            {
                if (effect.targetType == TargetType.Enemy &&
                    (effect.effectType == EffectType.Damage || effect.effectType == EffectType.Attack))
                {
                    totalDamage += effect.value;
                }
            }

            if (totalDamage > highestDamage)
            {
                highestDamage = totalDamage;
                highestDamageAction = attack;
            }
        }

        return highestDamageAction;
    }

    private void ExecuteActionsInOrder()
    {
        isExecutingActions = true;

        // Sort selected actions by AP cost (highest first)
        var orderedActions = currentCharacter.selectedActions
            .OrderByDescending(a => a.actionPointCost)
            .ToList();

        foreach (var action in orderedActions)
        {
            if (currentCharacter.currentAP >= action.actionPointCost)
            {
                ExecuteAttack(currentCharacter, action);
                currentCharacter.currentAP -= action.actionPointCost;
            }
        }

        currentCharacter.selectedActions.Clear();
        isExecutingActions = false;

        // End turn
        StartNextTurn();
    }

    private void ExecuteAttack(CharacterData user, AttackFile attack)
    {
        Debug.Log($"{user.characterName} uses {attack.attackName}!");

        // Play animation if available
        if (attack.battleAnimation != null)
        {
            StartCoroutine(attack.battleAnimation.PlayAnimation(user, new List<CharacterData> { /* targets */ }));
        }
        Debug.Log($"{user.characterName} uses {attack.attackName}!");

        foreach (var effect in attack.effects)
        {
            // Determine targets
            List<CharacterData> targets = GetTargets(effect.targetType, effect.numberOfTargets);

            // Roll for accuracy
            int accuracyRoll = Random.Range(0, 101);
            bool isSuccess = accuracyRoll <= effect.accuracy;

            // Check if effect should trigger
            if ((isSuccess && effect.triggersOn == EffectTrigger.OnSuccess) ||
                (!isSuccess && effect.triggersOn == EffectTrigger.OnMiss))
            {
                ApplyEffect(user, targets, effect);
            }
        }

        CheckBattleEnd();
    }

    private List<CharacterData> GetTargets(TargetType targetType, int numberOfTargets)
    {
        List<CharacterData> potentialTargets = targetType == TargetType.Ally ? partyMembers : enemies;
        potentialTargets = potentialTargets.Where(t => t.currentHP > 0).ToList();

        if (potentialTargets.Count == 0)
            return new List<CharacterData>();

        // Random selection for now (could be enhanced with targeting logic)
        List<CharacterData> selectedTargets = new List<CharacterData>();
        int targetsToSelect = Mathf.Min(numberOfTargets, potentialTargets.Count);

        for (int i = 0; i < targetsToSelect; i++)
        {
            int randomIndex = Random.Range(0, potentialTargets.Count);
            selectedTargets.Add(potentialTargets[randomIndex]);
            potentialTargets.RemoveAt(randomIndex);
        }

        return selectedTargets;
    }

    private void ApplyEffect(CharacterData user, List<CharacterData> targets, EffectData effect)
    {
        foreach (var target in targets)
        {
            switch (effect.effectType)
            {
                case EffectType.Damage:
                    int damage = Mathf.Max(1, effect.value - target.defense);
                    target.TakeDamage(damage);
                    Debug.Log($"{target.characterName} takes {damage} damage!");
                    break;

                case EffectType.Heal:
                    target.Heal(effect.value);
                    Debug.Log($"{target.characterName} heals for {effect.value} HP!");
                    break;

                case EffectType.Attack:
                    // Attack based on user's attack stat
                    int attackDamage = Mathf.Max(1, user.attack + effect.value - target.defense);
                    target.TakeDamage(attackDamage);
                    Debug.Log($"{target.characterName} takes {attackDamage} damage!");
                    break;
            }
        }
    }

    private bool CheckBattleEnd()
    {
        bool allPartyDowned = partyMembers.All(p => p.currentHP <= 0);
        bool allEnemiesDowned = enemies.All(e => e.currentHP <= 0);

        if (allEnemiesDowned)
        {
            currentState = CombatState.VICTORY;
            AwardExperience();
            Debug.Log("Victory! All enemies defeated!");
            return true;
        }
        else if (allPartyDowned)
        {
            currentState = CombatState.DEFEAT;
            Debug.Log("Defeat! All party members are DOWNED!");
            return true;
        }

        return false;
    }

    private void AwardExperience()
    {
        int totalExp = enemies.Sum(e => e.expValue);
        foreach (var member in partyMembers.Where(p => p.currentHP > 0))
        {
            member.GainExperience(totalExp);
        }
        Debug.Log($"Party gained {totalExp} experience!");
    }

    // Public method for UI to call when player selects an action
    public void SelectPlayerAction(AttackFile action)
    {
        if (currentState == CombatState.PLAYER_TURN &&
            currentCharacter.currentAP >= action.actionPointCost)
        {
            currentCharacter.selectedActions.Add(action);
            Debug.Log($"Selected: {action.attackName}");
        }
    }
}