using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public List<PartyMemberState> partyMembers = new List<PartyMemberState>();
    public List<PartyMemberState> enemies = new List<PartyMemberState>();

    [Header("Spawn Points")]
    public List<Transform> partySpawnPoints;
    public List<Transform> enemySpawnPoints;

    [Header("Prefabs")]
    public GameObject partyMemberVisualPrefab;
    public GameObject enemyVisualPrefab;

    [Header("Combat State")]
    public CombatState currentState = CombatState.STARTING;

    // Events for UI
    public System.Action<PartyMemberState> onTurnStarted;
    public System.Action<PartyMemberState> onCharacterUpdated;
    public System.Action<PartyMemberState, AttackFile, List<PartyMemberState>> onActionExecuted;
    public System.Action<CombatState> onCombatEnded;

    private Queue<PartyMemberState> turnQueue = new Queue<PartyMemberState>();
    private PartyMemberState currentCharacter;
    private bool isExecutingActions = false;
    private bool isAnimating = false;

    // Changed from Dictionary to List to allow multiple of the same attack
    private List<QueuedAction> pendingActions = new List<QueuedAction>();

    // Track the last action for undo functionality
    private QueuedAction lastAction = null;

    // Track defending characters
    private Dictionary<PartyMemberState, int> defendingCharacters = new Dictionary<PartyMemberState, int>();
    private Dictionary<PartyMemberState, int> originalDefenseValues = new Dictionary<PartyMemberState, int>();

    // Helper class to store queued actions
    [System.Serializable]
    private class QueuedAction
    {
        public AttackFile attack;
        public List<PartyMemberState> targets;

        public QueuedAction(AttackFile attack, List<PartyMemberState> targets)
        {
            this.attack = attack;
            this.targets = new List<PartyMemberState>(targets);
        }
    }

    private void Start()
    {
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData != null)
        {
            InitializeCombatWithData(encounterData);
        }
        else
        {
            Debug.LogError("No EncounterData found! Cannot start combat.");
        }
    }

    public void InitializeCombatWithData(EncounterData encounterData)
    {
        partyMembers.Clear();
        enemies.Clear();

        // Set up party members from player data
        if (encounterData.playerPartyMembers != null && encounterData.playerPartyMembers.Count > 0)
        {
            for (int i = 0; i < encounterData.playerPartyMembers.Count; i++)
            {
                PartyMemberState memberData = encounterData.playerPartyMembers[i];
                if (memberData == null) continue;

                if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && partyMemberVisualPrefab != null)
                {
                    GameObject memberObj = Instantiate(partyMemberVisualPrefab, partySpawnPoints[i].position, Quaternion.identity);
                    memberObj.name = $"Party_{memberData.CharacterName}";

                    CharacterComponent comp = memberObj.GetComponent<CharacterComponent>();
                    if (comp == null) comp = memberObj.AddComponent<CharacterComponent>();
                    comp.partyMemberState = memberData;

                    // Store transform reference for animations
                    memberData.transform = memberObj.transform;
                }

                partyMembers.Add(memberData);
            }
        }

        // Set up enemies from encounter data
        if (encounterData.enemyPartyMembers != null && encounterData.enemyPartyMembers.Count > 0)
        {
            for (int i = 0; i < encounterData.enemyPartyMembers.Count; i++)
            {
                PartyMemberState enemyData = encounterData.enemyPartyMembers[i];
                GameObject enemyPrefab = i < encounterData.enemyPrefabs.Count ? encounterData.enemyPrefabs[i] : null;

                if (enemyData == null) continue;

                GameObject prefabToUse = enemyPrefab != null ? enemyPrefab : enemyVisualPrefab;

                if (prefabToUse != null && i < enemySpawnPoints.Count && enemySpawnPoints[i] != null)
                {
                    GameObject enemyObj = Instantiate(prefabToUse, enemySpawnPoints[i].position, Quaternion.identity);
                    enemyObj.name = $"Enemy_{enemyData.CharacterName}";

                    CharacterComponent comp = enemyObj.GetComponent<CharacterComponent>();
                    if (comp == null) comp = enemyObj.AddComponent<CharacterComponent>();
                    comp.partyMemberState = enemyData;

                    enemyData.transform = enemyObj.transform;

                    if (enemyObj.GetComponent<ComplexAI>() == null)
                    {
                        enemyObj.AddComponent<ComplexAI>();
                    }
                }

                enemies.Add(enemyData);
            }
        }

        InitializeCombat();
    }

    private void InitializeCombat()
    {
        // Filter out any DOWNED characters
        partyMembers = partyMembers.Where(p => p.currentHP > 0).ToList();
        enemies = enemies.Where(e => e.currentHP > 0).ToList();

        currentState = CombatState.PLAYER_TURN;
        BuildTurnQueue();
        StartNextTurn();
    }

    private void BuildTurnQueue()
    {
        turnQueue.Clear();

        // Party members go first (all of them)
        foreach (var member in partyMembers)
        {
            if (member.currentHP > 0)
            {
                member.ResetAP();
                turnQueue.Enqueue(member);
            }
        }

        // Enemies go after ALL party members
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
            // All characters have taken turns, start a new round
            BuildTurnQueue();
        }

        // Remove defend bonus from previous character when their turn ends
        if (currentCharacter != null && defendingCharacters.ContainsKey(currentCharacter))
        {
            RemoveDefendBonus(currentCharacter);
        }

        currentCharacter = turnQueue.Dequeue();

        // Skip DOWNED characters
        if (currentCharacter.currentHP <= 0)
        {
            StartNextTurn();
            return;
        }

        // Process status effects at start of turn
        currentCharacter.ProcessStatusEffectsOnTurnStart();

        if (!currentCharacter.CanAct())
        {
            onCharacterUpdated?.Invoke(currentCharacter);
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
        // Wait for player to end turn (either by using all AP or clicking Wait)
        while (currentCharacter.currentAP > 0 && !isExecutingActions && !isAnimating)
        {
            yield return null;
        }

        // If there are pending actions when the turn ends, execute them
        if (pendingActions.Count > 0 && !isExecutingActions)
        {
            ExecuteActionsInOrder();
        }
        else
        {
            // No actions, just end turn
            StartNextTurn();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        // Enemies should only perform ONE action per turn
        ComplexAI ai = currentCharacter.transform?.GetComponent<ComplexAI>();
        AttackFile selectedAction = null;
        List<PartyMemberState> targets = new List<PartyMemberState>();

        // Small delay for dramatic effect
        yield return new WaitForSeconds(0.5f);

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
            // Enemies execute ONE action immediately, not queued
            currentCharacter.currentAP -= selectedAction.actionPointCost;
            onCharacterUpdated?.Invoke(currentCharacter);

            yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, selectedAction, targets));
        }
        else
        {
            // Enemy couldn't act (no AP or no valid action)
            Debug.Log($"{currentCharacter.CharacterName} couldn't act!");
        }

        // End enemy turn immediately after one action
        StartNextTurn();
    }

    private AttackFile GetHighestDamageAction(PartyMemberState character)
    {
        AttackFile highestDamageAction = null;
        int highestDamage = 0;

        foreach (var attack in character.learnedAttacks)
        {
            if (attack.partyMemberOnly && enemies.Contains(character))
                continue;

            if (character.currentAP < attack.actionPointCost)
                continue;

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

    public void EndTurnAndExecuteActions()
    {
        if (currentState == CombatState.PLAYER_TURN && !isExecutingActions && !isAnimating)
        {
            if (pendingActions.Count > 0)
            {
                ExecuteActionsInOrder();
            }
            else
            {
                StartNextTurn();
            }
        }
    }

    public void EndPlayerTurn()
    {
        EndTurnAndExecuteActions();
    }

    public void SelectPlayerAction(AttackFile action, List<PartyMemberState> targets)
    {
        // Only players should use this method
        if (!partyMembers.Contains(currentCharacter))
        {
            Debug.LogWarning("SelectPlayerAction called for non-player character - ignoring");
            return;
        }

        if (currentState == CombatState.PLAYER_TURN &&
            currentCharacter.currentAP >= action.actionPointCost &&
            !isExecutingActions && !isAnimating)
        {
            // Create new queued action
            QueuedAction newAction = new QueuedAction(action, targets);
            pendingActions.Add(newAction);

            // Store last action for undo
            lastAction = newAction;

            // Deduct AP immediately
            currentCharacter.currentAP -= action.actionPointCost;
            onCharacterUpdated?.Invoke(currentCharacter);

            Debug.Log($"Action queued: {action.attackName}. Total queued: {pendingActions.Count}");
        }
    }

    public void UndoLastAction()
    {
        if (!partyMembers.Contains(currentCharacter) || currentState != CombatState.PLAYER_TURN)
            return;

        if (lastAction != null && pendingActions.Contains(lastAction))
        {
            // Remove from pending actions
            pendingActions.Remove(lastAction);

            // Refund AP
            currentCharacter.currentAP += lastAction.attack.actionPointCost;

            // Clear last action
            lastAction = null;

            onCharacterUpdated?.Invoke(currentCharacter);
            Debug.Log($"Undid last action. Remaining queued: {pendingActions.Count}");
        }
    }

    public bool HasPendingActions()
    {
        return pendingActions.Count > 0;
    }

    private void ExecuteActionsInOrder()
    {
        if (isExecutingActions)
            return;

        isExecutingActions = true;

        // Sort actions by AP cost (highest first)
        var orderedActions = pendingActions
            .OrderByDescending(a => a.attack.actionPointCost)
            .ToList();

        StartCoroutine(ExecuteActionQueue(orderedActions));
    }

    private IEnumerator ExecuteActionQueue(List<QueuedAction> actions)
    {
        var actionsToProcess = new List<QueuedAction>(actions);

        foreach (var queuedAction in actionsToProcess)
        {
            if (!pendingActions.Contains(queuedAction))
                continue;

            AttackFile action = queuedAction.attack;
            List<PartyMemberState> targets = queuedAction.targets;

            if (targets != null && targets.Count > 0)
            {
                pendingActions.Remove(queuedAction);

                // Clear last action if this is the one being executed
                if (lastAction == queuedAction)
                {
                    lastAction = null;
                }

                yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, action, targets));

                onActionExecuted?.Invoke(currentCharacter, action, targets);
                onCharacterUpdated?.Invoke(currentCharacter);
            }
            else
            {
                pendingActions.Remove(queuedAction);
            }

            yield return new WaitForSeconds(0.2f);
        }

        pendingActions.Clear();
        isExecutingActions = false;

        // After executing all actions, end the turn
        StartNextTurn();
    }

    private IEnumerator ExecuteAttackWithAnimation(PartyMemberState user, AttackFile attack, List<PartyMemberState> targets)
    {
        isAnimating = true;

        if (attack.battleAnimation != null)
        {
            yield return attack.battleAnimation.PlayAnimation(user, targets, () => {
                ApplyAttackEffects(user, attack, targets);
            });
        }
        else
        {
            ApplyAttackEffects(user, attack, targets);
            yield return null;
        }

        isAnimating = false;
    }

    private void ApplyAttackEffects(PartyMemberState user, AttackFile attack, List<PartyMemberState> targets)
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

    private void ApplyEffect(PartyMemberState user, List<PartyMemberState> targets, EffectData effect)
    {
        foreach (var target in targets)
        {
            if (target == null || target.currentHP <= 0)
                continue;

            switch (effect.effectType)
            {
                case EffectType.Damage:
                    // Calculate damage (base damage + user attack - target defense)
                    int damage = Mathf.Max(1, effect.value + user.Attack - target.Defense);
                    target.TakeDamage(damage);
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Heal:
                case EffectType.HP_Restore:
                    target.Heal(effect.value);
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Attack:
                    // Attack based on user's attack stat plus effect value
                    int attackDamage = Mathf.Max(1, user.Attack + effect.value - target.Defense);
                    target.TakeDamage(attackDamage);
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.ManaRestore:
                    target.currentAP = Mathf.Min(target.MaxAP, target.currentAP + effect.value);
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Revive:
                    if (target.currentHP <= 0)
                    {
                        target.currentHP = effect.value;
                        onCharacterUpdated?.Invoke(target);
                    }
                    break;

                case EffectType.StatusEffect:
                    if (effect.statusEffect != null)
                    {
                        target.AddStatusEffect(effect.statusEffect, user);
                        onCharacterUpdated?.Invoke(target);
                        Debug.Log($"{target.CharacterName} afflicted with {effect.statusEffect.effectName}!");
                    }
                    break;

                case EffectType.Buff:
                    if (effect.statModifiers != null && effect.statModifiers.Count > 0)
                    {
                        foreach (var modifier in effect.statModifiers)
                        {
                            switch (modifier.statType)
                            {
                                case StatType.Attack:
                                    target.ModifyAttack(modifier.valorModificador);
                                    break;
                                case StatType.Defense:
                                    target.ModifyDefense(modifier.valorModificador);
                                    break;
                            }
                        }
                        onCharacterUpdated?.Invoke(target);
                    }
                    break;

                case EffectType.Debuff:
                    if (effect.statModifiers != null && effect.statModifiers.Count > 0)
                    {
                        foreach (var modifier in effect.statModifiers)
                        {
                            switch (modifier.statType)
                            {
                                case StatType.Attack:
                                    target.ModifyAttack(-modifier.valorModificador);
                                    break;
                                case StatType.Defense:
                                    target.ModifyDefense(-modifier.valorModificador);
                                    break;
                            }
                        }
                        onCharacterUpdated?.Invoke(target);
                    }
                    break;

                case EffectType.MultiHit:
                    for (int i = 0; i < effect.hitCount; i++)
                    {
                        int multiDamage = Mathf.Max(1, (effect.value + user.Attack - target.Defense) / effect.hitCount);
                        target.TakeDamage(multiDamage);

                        if (target.currentHP <= 0)
                            break;
                    }
                    onCharacterUpdated?.Invoke(target);
                    break;
            }
        }

        CheckBattleEnd();
    }

    private List<PartyMemberState> GetTargets(TargetType targetType, int numberOfTargets)
    {
        List<PartyMemberState> potentialTargets = targetType == TargetType.Ally ? partyMembers : enemies;
        potentialTargets = potentialTargets.Where(t => t.currentHP > 0).ToList();

        if (potentialTargets.Count == 0)
            return new List<PartyMemberState>();

        // Random selection for now
        List<PartyMemberState> selectedTargets = new List<PartyMemberState>();
        int targetsToSelect = Mathf.Min(numberOfTargets, potentialTargets.Count);

        for (int i = 0; i < targetsToSelect; i++)
        {
            int randomIndex = Random.Range(0, potentialTargets.Count);
            selectedTargets.Add(potentialTargets[randomIndex]);
            potentialTargets.RemoveAt(randomIndex);
        }

        return selectedTargets;
    }

    // Defend methods
    public void ApplyDefendBonus(PartyMemberState character)
    {
        if (character == null) return;

        // Store original defense
        if (!originalDefenseValues.ContainsKey(character))
        {
            originalDefenseValues[character] = character.Defense;
        }

        // Triple defense
        defendingCharacters[character] = character.Defense * 3;

        onCharacterUpdated?.Invoke(character);
    }

    public void RemoveDefendBonus(PartyMemberState character)
    {
        if (character == null) return;

        if (originalDefenseValues.ContainsKey(character))
        {
            originalDefenseValues.Remove(character);
        }

        if (defendingCharacters.ContainsKey(character))
        {
            defendingCharacters.Remove(character);
        }

        onCharacterUpdated?.Invoke(character);
    }

    public bool IsDefending(PartyMemberState character)
    {
        return defendingCharacters.ContainsKey(character);
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
            StartCoroutine(ReturnToMapAfterDelay(2f));
            return true;
        }
        else if (allPartyDowned)
        {
            currentState = CombatState.DEFEAT;
            onCombatEnded?.Invoke(CombatState.DEFEAT);
            StartCoroutine(ReturnToMapAfterDelay(2f));
            return true;
        }

        return false;
    }

    private IEnumerator ReturnToMapAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        PreviousScene previousScene = FindFirstObjectByType<PreviousScene>();

        if (previousScene != null)
        {
            // If victory, apply rewards before returning
            if (currentState == CombatState.VICTORY && encounterData != null)
            {
                encounterData.combatVictory = true;
            }

            // Return to map scene
            previousScene.LoadScene();

            // Unload combat scene
            SceneManager.UnloadSceneAsync("Combat");
        }
    }

    private void AwardExperience()
    {
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData != null)
        {
            encounterData.combatVictory = true;
            encounterData.ApplyCombatRewards();
        }

        int totalExp = enemies.Sum(e => e.GetExpValue());
        foreach (var member in partyMembers.Where(p => p.currentHP > 0))
        {
            member.GainExperience(totalExp);
        }
    }

    // Public methods for UI
    public PartyMemberState GetCurrentCharacter()
    {
        return currentCharacter;
    }

    public bool IsPlayerTurn()
    {
        return currentState == CombatState.PLAYER_TURN;
    }
}