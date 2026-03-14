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

    private Queue<PartyMemberState> playerTurnQueue = new Queue<PartyMemberState>();
    private Queue<PartyMemberState> enemyTurnQueue = new Queue<PartyMemberState>();
    private PartyMemberState currentCharacter;
    private bool isExecutingActions = false;
    private bool isAnimating = false;
    private bool isPlayerExecuting = false;
    private GameObject menubutton;
    private GameObject objbutton2;

    // Store queued actions
    private List<QueuedAction> pendingActions = new List<QueuedAction>();

    // Store the state before the first action for undo
    private int apBeforeTurn;
    private List<QueuedAction> actionsThisTurn = new List<QueuedAction>();

    // Track defending characters
    private Dictionary<PartyMemberState, int> defendingCharacters = new Dictionary<PartyMemberState, int>();
    private Dictionary<PartyMemberState, int> originalDefenseValues = new Dictionary<PartyMemberState, int>();

    // Helper class to store queued actions
    [System.Serializable]
    private class QueuedAction
    {
        public AttackFile attack;
        public List<PartyMemberState> targets;
        public bool isItem;

        public QueuedAction(AttackFile attack, List<PartyMemberState> targets)
        {
            this.attack = attack;
            this.targets = new List<PartyMemberState>(targets);
            this.isItem = false;
        }

        public QueuedAction(bool isItem)
        {
            this.attack = null;
            this.targets = new List<PartyMemberState>();
            this.isItem = true;
        }
    }

    private void Start()
    {
        menubutton = GameObject.FindGameObjectWithTag("MenuOpener");
        objbutton2 = GameObject.FindGameObjectWithTag("ObjectiveButtwo");
        menubutton.SetActive(false);
        objbutton2.SetActive(false);
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

                    memberData.transform = memberObj.transform;
                }

                partyMembers.Add(memberData);
            }
        }

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
        partyMembers = partyMembers.Where(p => p.currentHP > 0).ToList();
        enemies = enemies.Where(e => e.currentHP > 0).ToList();

        currentState = CombatState.PLAYER_TURN;
        BuildTurnQueues();
        StartNextPlayerTurn();
    }

    private void BuildTurnQueues()
    {
        playerTurnQueue.Clear();
        enemyTurnQueue.Clear();

        foreach (var member in partyMembers)
        {
            if (member.currentHP > 0)
            {
                member.ResetAP();
                playerTurnQueue.Enqueue(member);
            }
        }

        foreach (var enemy in enemies)
        {
            if (enemy.currentHP > 0)
            {
                enemyTurnQueue.Enqueue(enemy);
            }
        }
    }

    private void StartNextPlayerTurn()
    {
        if (CheckBattleEnd())
            return;

        if (currentCharacter != null && defendingCharacters.ContainsKey(currentCharacter))
        {
            RemoveDefendBonus(currentCharacter);
        }

        if (playerTurnQueue.Count == 0)
        {
            StartNextEnemyTurn();
            return;
        }

        currentCharacter = playerTurnQueue.Dequeue();

        if (currentCharacter.currentHP <= 0)
        {
            StartNextPlayerTurn();
            return;
        }

        currentCharacter.ProcessStatusEffectsOnTurnStart();

        if (!currentCharacter.CanAct())
        {
            onCharacterUpdated?.Invoke(currentCharacter);
            StartNextPlayerTurn();
            return;
        }

        // Store initial AP for undo
        apBeforeTurn = currentCharacter.currentAP;
        actionsThisTurn.Clear();
        pendingActions.Clear(); // Clear any leftover pending actions

        currentState = CombatState.PLAYER_TURN;
        onTurnStarted?.Invoke(currentCharacter);

        StartCoroutine(PlayerTurnRoutine());
    }

    private void StartNextEnemyTurn()
    {
        // Prevent enemy turn from starting while player actions are executing
        if (isPlayerExecuting)
        {
            return;
        }

        if (CheckBattleEnd())
            return;

        if (currentCharacter != null && defendingCharacters.ContainsKey(currentCharacter))
        {
            RemoveDefendBonus(currentCharacter);
        }

        if (enemyTurnQueue.Count == 0)
        {
            BuildTurnQueues();
            StartNextPlayerTurn();
            return;
        }

        currentCharacter = enemyTurnQueue.Dequeue();

        if (currentCharacter.currentHP <= 0)
        {
            StartNextEnemyTurn();
            return;
        }

        currentCharacter.ProcessStatusEffectsOnTurnStart();

        if (!currentCharacter.CanAct())
        {
            onCharacterUpdated?.Invoke(currentCharacter);
            StartNextEnemyTurn();
            return;
        }

        currentState = CombatState.ENEMY_TURN;
        onTurnStarted?.Invoke(currentCharacter);

        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator WaitForPlayerExecution()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (isPlayerExecuting && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Try to start enemy turn again
        StartNextEnemyTurn();
    }

    private IEnumerator PlayerTurnRoutine()
    {
        while (currentCharacter.currentAP > 0 && !isExecutingActions && !isAnimating)
        {
            yield return null;
        }

        if (pendingActions.Count > 0 && !isExecutingActions)
        {
            ExecuteActionsInOrder();
        }
        else
        {
            StartNextPlayerTurn();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        ComplexAI ai = currentCharacter.transform?.GetComponent<ComplexAI>();
        AttackFile selectedAction = null;
        List<PartyMemberState> targets = new List<PartyMemberState>();

        if (ai != null)
        {
            var decision = ai.MakeDecision(partyMembers, enemies);
            selectedAction = decision.selectedAction;
            targets = decision.targets;
        }
        else
        {
            selectedAction = GetHighestDamageAction(currentCharacter);
            if (selectedAction != null)
            {
                targets = GetTargets(selectedAction.effects[0].targetType,
                                     selectedAction.effects[0].numberOfTargets);
            }
        }

        if (selectedAction != null)
        {
            yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, selectedAction, targets));

            onActionExecuted?.Invoke(currentCharacter, selectedAction, targets);
            onCharacterUpdated?.Invoke(currentCharacter);
        }

        StartNextEnemyTurn();
    }

    private AttackFile GetHighestDamageAction(PartyMemberState character)
    {
        AttackFile highestDamageAction = null;
        int highestDamage = 0;

        foreach (var attack in character.learnedAttacks)
        {
            if (attack.partyMemberOnly && enemies.Contains(character))
                continue;

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
                StartNextPlayerTurn();
            }
        }
    }

    public void EndPlayerTurn()
    {
        EndTurnAndExecuteActions();
    }

    public void SelectPlayerAction(AttackFile action, List<PartyMemberState> targets)
    {
        if (!partyMembers.Contains(currentCharacter) || currentState != CombatState.PLAYER_TURN)
            return;

        if (currentCharacter.currentAP >= action.actionPointCost && !isExecutingActions && !isAnimating)
        {
            QueuedAction newAction = new QueuedAction(action, targets);
            pendingActions.Add(newAction);
            actionsThisTurn.Add(newAction);

            currentCharacter.currentAP -= action.actionPointCost;
            onCharacterUpdated?.Invoke(currentCharacter);
        }
    }

    public void UseItem(List<PartyMemberState> targets)
    {
        if (!partyMembers.Contains(currentCharacter) || currentState != CombatState.PLAYER_TURN)
            return;

        const int itemAPCost = 5;

        if (currentCharacter.currentAP >= itemAPCost && !isExecutingActions && !isAnimating)
        {
            QueuedAction newAction = new QueuedAction(true);
            pendingActions.Add(newAction);
            actionsThisTurn.Add(newAction);

            currentCharacter.currentAP -= itemAPCost;
            onCharacterUpdated?.Invoke(currentCharacter);
        }
    }

    public void UndoLastAction()
    {
        if (!partyMembers.Contains(currentCharacter) || currentState != CombatState.PLAYER_TURN)
            return;

        if (actionsThisTurn.Count > 0)
        {
            // Remove the last action from both lists
            QueuedAction lastAction = actionsThisTurn[actionsThisTurn.Count - 1];
            actionsThisTurn.RemoveAt(actionsThisTurn.Count - 1);
            pendingActions.Remove(lastAction);

            // Restore AP to beginning of turn state
            currentCharacter.currentAP = apBeforeTurn;

            onCharacterUpdated?.Invoke(currentCharacter);

            Debug.Log($"Undid last action. AP restored to {apBeforeTurn}. Remaining queued: {pendingActions.Count}");
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
        isPlayerExecuting = true;

        var orderedActions = pendingActions
            .Where(a => !a.isItem)
            .OrderByDescending(a => a.attack != null ? a.attack.actionPointCost : 0)
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

                yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, action, targets));

                onActionExecuted?.Invoke(currentCharacter, action, targets);
                onCharacterUpdated?.Invoke(currentCharacter);
            }

            yield return new WaitForSeconds(0.2f);
        }

        pendingActions.RemoveAll(a => a.isItem);
        pendingActions.Clear();
        isExecutingActions = false;
        isPlayerExecuting = false;

        StartNextPlayerTurn();
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
            int accuracyRoll = Random.Range(0, 101);
            bool isSuccess = accuracyRoll <= effect.accuracy;

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

    public void ApplyDefendBonus(PartyMemberState character)
    {
        if (character == null) return;

        if (!originalDefenseValues.ContainsKey(character))
        {
            originalDefenseValues[character] = character.Defense;
        }

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

        menubutton.SetActive(true);
        objbutton2.SetActive(true);
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        PreviousScene previousScene = FindFirstObjectByType<PreviousScene>();

        if (previousScene != null)
        {
            if (currentState == CombatState.VICTORY && encounterData != null)
            {
                encounterData.combatVictory = true;
            }

            previousScene.LoadScene();
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

    public PartyMemberState GetCurrentCharacter()
    {
        return currentCharacter;
    }

    public bool IsPlayerTurn()
    {
        return currentState == CombatState.PLAYER_TURN;
    }
}