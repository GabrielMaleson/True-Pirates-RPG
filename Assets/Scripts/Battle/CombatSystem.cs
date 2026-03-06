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
    public List<CharacterData> partyMembers = new List<CharacterData>();
    public List<CharacterData> enemies = new List<CharacterData>();

    [Header("Spawn Points")]
    public List<Transform> partySpawnPoints;
    public List<Transform> enemySpawnPoints;

    [Header("Prefabs")]
    public GameObject partyMemberVisualPrefab;
    public GameObject enemyVisualPrefab;

    [Header("Combat State")]
    public CombatState currentState = CombatState.STARTING;

    public System.Action<CharacterData> onTurnStarted;
    public System.Action<CharacterData> onCharacterUpdated;
    public System.Action<CharacterData, AttackFile, List<CharacterData>> onActionExecuted;
    public System.Action<CombatState> onCombatEnded;

    private Queue<CharacterData> turnQueue = new Queue<CharacterData>();
    private CharacterData currentCharacter;
    private bool isExecutingActions = false;
    private bool isAnimating = false;
    private Dictionary<AttackFile, List<CharacterData>> pendingActions = new Dictionary<AttackFile, List<CharacterData>>();

    // Track the last action for undo functionality
    private KeyValuePair<AttackFile, List<CharacterData>>? lastAction = null;
    private int lastActionAPCost = 0;

    private void Start()
    {
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData != null)
        {
            InitializeCombatWithData(encounterData);
        }
        else
        {
            InitializeCombat();
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
                CharacterData memberData = encounterData.playerPartyMembers[i];
                if (memberData == null) continue;

                if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && partyMemberVisualPrefab != null)
                {
                    GameObject memberObj = Instantiate(partyMemberVisualPrefab, partySpawnPoints[i].position, Quaternion.identity);
                    memberObj.name = $"Party_{memberData.characterName}";

                    CharacterComponent comp = memberObj.GetComponent<CharacterComponent>();
                    if (comp == null) comp = memberObj.AddComponent<CharacterComponent>();
                    comp.characterData = memberData;

                    memberData.transform = memberObj.transform;
                }

                partyMembers.Add(memberData);
            }
        }

        if (encounterData.enemyCharacters != null && encounterData.enemyCharacters.Count > 0)
        {
            for (int i = 0; i < encounterData.enemyCharacters.Count; i++)
            {
                CharacterData enemyData = encounterData.enemyCharacters[i];
                GameObject enemyPrefab = i < encounterData.enemyPrefabs.Count ? encounterData.enemyPrefabs[i] : null;
                int enemyLevel = i < encounterData.enemyLevels.Count ? encounterData.enemyLevels[i] : 1;
                int overrideHP = i < encounterData.enemyOverrideHP.Count ? encounterData.enemyOverrideHP[i] : 0;

                if (enemyData == null) continue;

                while (enemyData.level < enemyLevel)
                {
                    enemyData.LevelUp();
                }

                if (overrideHP > 0)
                {
                    enemyData.currentHP = overrideHP;
                }

                GameObject prefabToUse = enemyPrefab != null ? enemyPrefab : enemyVisualPrefab;

                if (prefabToUse != null && i < enemySpawnPoints.Count && enemySpawnPoints[i] != null)
                {
                    GameObject enemyObj = Instantiate(prefabToUse, enemySpawnPoints[i].position, Quaternion.identity);
                    enemyObj.name = $"Enemy_{enemyData.characterName}";

                    CharacterComponent comp = enemyObj.GetComponent<CharacterComponent>();
                    if (comp == null) comp = enemyObj.AddComponent<CharacterComponent>();
                    comp.characterData = enemyData;

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

    private CharacterData CreateCharacterDataCopy(CharacterData source)
    {
        CharacterData copy = ScriptableObject.CreateInstance<CharacterData>();

        copy.characterName = source.characterName;
        copy.level = source.level;
        copy.currentHP = source.currentHP;
        copy.expValue = source.expValue;

        copy.baseHP = source.baseHP;
        copy.baseAttack = source.baseAttack;
        copy.baseDefense = source.baseDefense;
        copy.maxAP = source.maxAP;

        copy.hpGrowth = source.hpGrowth;
        copy.attackGrowth = source.attackGrowth;
        copy.defenseGrowth = source.defenseGrowth;

        copy.unlockableAttacks = new List<UnlockableAttack>(source.unlockableAttacks);
        copy.availableAttacks = new List<AttackFile>(source.availableAttacks);

        copy.CalculateStatsForLevel();

        return copy;
    }

    private void InitializeCombat()
    {
        partyMembers = partyMembers.Where(p => p.currentHP > 0).ToList();
        enemies = enemies.Where(e => e.currentHP > 0).ToList();

        currentState = CombatState.PLAYER_TURN;
        BuildTurnQueue();
        StartNextTurn();
    }

    private void BuildTurnQueue()
    {
        turnQueue.Clear();

        foreach (var member in partyMembers)
        {
            if (member.currentHP > 0)
            {
                member.ResetAP();
                turnQueue.Enqueue(member);
            }
        }

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
            BuildTurnQueue();
        }

        currentCharacter = turnQueue.Dequeue();

        if (currentCharacter.currentHP <= 0)
        {
            StartNextTurn();
            return;
        }

        currentCharacter.ProcessStatusEffectsOnTurnStart();

        if (!currentCharacter.CanAct())
        {
            onCharacterUpdated?.Invoke(currentCharacter);
            StartNextTurn();
            return;
        }

        onTurnStarted?.Invoke(currentCharacter);

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
    }

    private IEnumerator EnemyTurnRoutine()
    {
        ComplexAI ai = currentCharacter.transform?.GetComponent<ComplexAI>();
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
            selectedAction = GetHighestDamageAction(currentCharacter);
            if (selectedAction != null)
            {
                targets = GetTargets(selectedAction.effects[0].targetType,
                                     selectedAction.effects[0].numberOfTargets);
            }
        }

        if (selectedAction != null && currentCharacter.currentAP >= selectedAction.actionPointCost)
        {
            // Enemies execute immediately, not queued
            currentCharacter.currentAP -= selectedAction.actionPointCost;
            onCharacterUpdated?.Invoke(currentCharacter);

            yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, selectedAction, targets));
        }

        yield return new WaitForSeconds(0.5f);
        StartNextTurn();
    }

    private AttackFile GetHighestDamageAction(CharacterData character)
    {
        AttackFile highestDamageAction = null;
        int highestDamage = 0;

        foreach (var attack in character.availableAttacks)
        {
            if (attack.partyMemberOnly && enemies.Contains(character))
                continue;

            if (character.currentAP < attack.actionPointCost)
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
                StartNextTurn();
            }
        }
    }

    public void EndPlayerTurn()
    {
        EndTurnAndExecuteActions();
    }

    public void SelectPlayerAction(AttackFile action, List<CharacterData> targets)
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
            string uniqueKey = $"{action.name}_{pendingActions.Count}";
            pendingActions[action] = new List<CharacterData>(targets);

            // Store last action for undo
            lastAction = new KeyValuePair<AttackFile, List<CharacterData>>(action, new List<CharacterData>(targets));
            lastActionAPCost = action.actionPointCost;

            // Deduct AP immediately
            currentCharacter.currentAP -= action.actionPointCost;
            onCharacterUpdated?.Invoke(currentCharacter);

            Debug.Log($"Action queued: {action.attackName}. Remaining AP: {currentCharacter.currentAP}");
        }
    }
    public void UndoLastAction()
    {
        if (!partyMembers.Contains(currentCharacter) || currentState != CombatState.PLAYER_TURN)
            return;

        if (lastAction.HasValue && pendingActions.ContainsKey(lastAction.Value.Key))
        {
            // Remove from pending actions
            pendingActions.Remove(lastAction.Value.Key);

            // Refund AP
            currentCharacter.currentAP += lastActionAPCost;

            // Clear last action
            lastAction = null;
            lastActionAPCost = 0;

            Debug.Log($"Undid last action. AP restored to: {currentCharacter.currentAP}");
            onCharacterUpdated?.Invoke(currentCharacter);
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

        var orderedActions = pendingActions.Keys
            .OrderByDescending(a => a.actionPointCost)
            .ToList();

        StartCoroutine(ExecuteActionQueue(orderedActions));
    }

    private IEnumerator ExecuteActionQueue(List<AttackFile> actions)
    {
        var actionsToProcess = new List<AttackFile>(actions);

        foreach (var action in actionsToProcess)
        {
            if (!pendingActions.ContainsKey(action))
                continue;

            List<CharacterData> targets = pendingActions[action];

            if (targets != null && targets.Count > 0)
            {
                pendingActions.Remove(action);

                // Clear last action if this is the one being executed
                if (lastAction.HasValue && lastAction.Value.Key == action)
                {
                    lastAction = null;
                    lastActionAPCost = 0;
                }

                yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, action, targets));

                onActionExecuted?.Invoke(currentCharacter, action, targets);
                onCharacterUpdated?.Invoke(currentCharacter);
            }
            else
            {
                pendingActions.Remove(action);
            }

            yield return new WaitForSeconds(0.2f);
        }

        pendingActions.Clear();
        isExecutingActions = false;

        // After executing all actions, end the turn
        StartNextTurn();
    }

    private IEnumerator ExecuteAttackWithAnimation(CharacterData user, AttackFile attack, List<CharacterData> targets)
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

    private void ApplyAttackEffects(CharacterData user, AttackFile attack, List<CharacterData> targets)
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

    private void ApplyEffect(CharacterData user, List<CharacterData> targets, EffectData effect)
    {
        foreach (var target in targets)
        {
            if (target == null || target.currentHP <= 0)
                continue;

            switch (effect.effectType)
            {
                case EffectType.Damage:
                    int damage = Mathf.Max(1, effect.value + user.attack - target.defense);
                    target.TakeDamage(damage);
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Heal:
                case EffectType.HP_Restore:
                    target.Heal(effect.value);
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Attack:
                    int attackDamage = Mathf.Max(1, user.attack + effect.value - target.defense);
                    target.TakeDamage(attackDamage);
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.ManaRestore:
                    target.currentAP = Mathf.Min(target.maxAP, target.currentAP + effect.value);
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

        int totalExp = enemies.Sum(e => e.expValue);
        foreach (var member in partyMembers.Where(p => p.currentHP > 0))
        {
            member.GainExperience(totalExp);
        }
    }

    public CharacterData GetCurrentCharacter()
    {
        return currentCharacter;
    }

    public bool IsPlayerTurn()
    {
        return currentState == CombatState.PLAYER_TURN;
    }
}