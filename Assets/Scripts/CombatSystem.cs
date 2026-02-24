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


// Add these events at the top of your CombatSystem class
public class CombatSystem : MonoBehaviour
{
    [Header("Combatants")]
    public List<CharacterData> partyMembers = new List<CharacterData>();
    public List<CharacterData> enemies = new List<CharacterData>();

    [Header("Combat State")]
    public CombatState currentState = CombatState.STARTING;

    // Events for UI
    public System.Action<CharacterData> onTurnStarted;
    public System.Action<CharacterData, AttackFile, List<CharacterData>> onActionExecuted;
    public System.Action<CombatState> onCombatEnded;

    private Queue<CharacterData> turnQueue = new Queue<CharacterData>();
    private CharacterData currentCharacter;
    private bool isExecutingActions = false;
    private bool isAnimating = false;

    // Store actions with their targets
    private Dictionary<AttackFile, List<CharacterData>> pendingActions = new Dictionary<AttackFile, List<CharacterData>>();

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

        // Process status effects at start of turn
        currentCharacter.ProcessStatusEffectsOnTurnStart();

        if (!currentCharacter.CanAct())
        {
            Debug.Log($"{currentCharacter.characterName} is unable to act!");
            StartNextTurn();
            return;
        }

        // Invoke turn started event
        onTurnStarted?.Invoke(currentCharacter);

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
        // Wait for player actions
        while (currentCharacter.currentAP > 0 && !isExecutingActions && !isAnimating)
        {
            yield return null; // Player selects actions via UI
        }

        // Process selected actions in order (highest AP cost first)
        if (pendingActions.Count > 0)
        {
            ExecuteActionsInOrder();
        }
        else
        {
            // End turn if no actions selected
            EndPlayerTurn();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        // Use ComplexAI if available, otherwise fall back to simple AI
        ComplexAI ai = GetComponent<ComplexAI>();
        AttackFile selectedAction = null;
        List<CharacterData> targets = new List<CharacterData>();

        if (ai != null)
        {
            var decision = ai.MakeDecision(partyMembers, enemies);
            selectedAction = decision.selectedAction;
            targets = decision.targets;
        }
        else
        {
            // Simple AI: select highest damage action
            selectedAction = GetHighestDamageAction(currentCharacter);
            if (selectedAction != null)
            {
                targets = GetTargets(selectedAction.effects[0].targetType,
                                     selectedAction.effects[0].numberOfTargets);
            }
        }

        if (selectedAction != null && currentCharacter.currentAP >= selectedAction.actionPointCost)
        {
            // Execute enemy action
            yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, selectedAction, targets));
            currentCharacter.currentAP -= selectedAction.actionPointCost;
        }

        yield return new WaitForSeconds(0.5f); // Brief pause between turns

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

    // NEW: Public method for UI to end turn
    public void EndPlayerTurn()
    {
        if (currentState == CombatState.PLAYER_TURN && !isExecutingActions && !isAnimating)
        {
            StartNextTurn();
        }
    }

    // NEW: Public method for UI to select action with targets
    public void SelectPlayerAction(AttackFile action, List<CharacterData> targets)
    {
        if (currentState == CombatState.PLAYER_TURN &&
            currentCharacter.currentAP >= action.actionPointCost &&
            !isExecutingActions && !isAnimating)
        {
            // Store the action with its targets
            pendingActions[action] = targets;
            Debug.Log($"Selected: {action.attackName} with {targets.Count} targets");
        }
    }

    private void ExecuteActionsInOrder()
    {
        isExecutingActions = true;

        // Sort selected actions by AP cost (highest first)
        var orderedActions = pendingActions.Keys
            .OrderByDescending(a => a.actionPointCost)
            .ToList();

        StartCoroutine(ExecuteActionQueue(orderedActions));
    }

    private IEnumerator ExecuteActionQueue(List<AttackFile> actions)
    {
        foreach (var action in actions)
        {
            if (currentCharacter.currentAP >= action.actionPointCost && pendingActions.ContainsKey(action))
            {
                List<CharacterData> targets = pendingActions[action];
                yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, action, targets));
                currentCharacter.currentAP -= action.actionPointCost;

                // Invoke action executed event
                onActionExecuted?.Invoke(currentCharacter, action, targets);
            }

            yield return new WaitForSeconds(0.2f);
        }

        pendingActions.Clear();
        isExecutingActions = false;

        // Check if character still has AP and can act
        if (currentCharacter.currentAP > 0 && currentCharacter.CanAct())
        {
            // Still have AP, let them choose more actions
            currentState = CombatState.PLAYER_TURN;
            onTurnStarted?.Invoke(currentCharacter);
        }
        else
        {
            // End turn
            EndPlayerTurn();
        }
    }

    // NEW: Method to execute attack with animation
    private IEnumerator ExecuteAttackWithAnimation(CharacterData user, AttackFile attack, List<CharacterData> targets)
    {
        isAnimating = true;

        Debug.Log($"{user.characterName} uses {attack.attackName}!");

        // Play animation if available
        if (attack.battleAnimation != null)
        {
            yield return attack.battleAnimation.PlayAnimation(user, targets, () => {
                // Apply effects after animation
                ApplyAttackEffects(user, attack, targets);
            });
        }
        else
        {
            // No animation, apply effects immediately
            ApplyAttackEffects(user, attack, targets);
            yield return null;
        }

        isAnimating = false;
    }

    private void ApplyAttackEffects(CharacterData user, AttackFile attack, List<CharacterData> targets)
    {
        foreach (var effect in attack.effects)
        {
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
            onCombatEnded?.Invoke(CombatState.VICTORY);
            AwardExperience();
            Debug.Log("Victory! All enemies defeated!");
            return true;
        }
        else if (allPartyDowned)
        {
            currentState = CombatState.DEFEAT;
            onCombatEnded?.Invoke(CombatState.DEFEAT);
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
}