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

    // Single flag that PlayerTurnRoutine waits on — the only way to end a player turn.
    // Nothing else calls StartNextPlayerTurn directly for player turns.
    private bool endTurnRequested = false;

    private GameObject menubutton;
    private GameObject objbutton2;

    // Actions queued this turn, in the order they were selected
    private List<QueuedAction> pendingActions = new List<QueuedAction>();

    // AP snapshot before each queued action so Undo restores exactly one action's worth of AP
    private Stack<int> apSnapshots = new Stack<int>();

    // Characters currently in defend stance (tripled defense vs incoming damage)
    private HashSet<PartyMemberState> defendingCharacters = new HashSet<PartyMemberState>();

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
            this.isItem = isItem;
        }
    }

    private void Start()
    {
        menubutton = GameObject.FindGameObjectWithTag("MenuOpener");
        objbutton2 = GameObject.FindGameObjectWithTag("ObjectiveButtwo");
        if (menubutton != null) menubutton.SetActive(false);
        if (objbutton2 != null) objbutton2.SetActive(false);

        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData != null)
            InitializeCombatWithData(encounterData);
        else
            Debug.LogError("No EncounterData found! Cannot start combat.");
    }

    public void InitializeCombatWithData(EncounterData encounterData)
    {
        partyMembers.Clear();
        enemies.Clear();

        // Play the encounter's battle music if one is assigned
        if (encounterData.encounterFile != null && encounterData.encounterFile.battleMusic != null)
            MusicManager.Instance?.PlayClip(encounterData.encounterFile.battleMusic);

        if (encounterData.playerPartyMembers != null)
        {
            for (int i = 0; i < encounterData.playerPartyMembers.Count; i++)
            {
                PartyMemberState memberData = encounterData.playerPartyMembers[i];
                if (memberData == null) continue;

                if (i >= partySpawnPoints.Count)
                    Debug.LogWarning($"[CombatSystem] No spawn point for party member {i} ({memberData.CharacterName}). partySpawnPoints.Count={partySpawnPoints.Count}");
                else if (partySpawnPoints[i] == null)
                    Debug.LogWarning($"[CombatSystem] partySpawnPoints[{i}] is null for {memberData.CharacterName}");
                else if (partyMemberVisualPrefab == null)
                    Debug.LogWarning($"[CombatSystem] partyMemberVisualPrefab is null — cannot spawn visual for {memberData.CharacterName}");

                if (i < partySpawnPoints.Count && partySpawnPoints[i] != null && partyMemberVisualPrefab != null)
                {
                    Debug.Log($"[CombatSystem] Spawning party visual for {memberData.CharacterName} at {partySpawnPoints[i].position}");
                    GameObject memberObj = Instantiate(partyMemberVisualPrefab, partySpawnPoints[i].position, Quaternion.identity);
                    memberObj.name = $"Party_{memberData.CharacterName}";

                    CharacterComponent comp = memberObj.GetComponent<CharacterComponent>();
                    if (comp == null) comp = memberObj.AddComponent<CharacterComponent>();
                    comp.partyMemberState = memberData;
                    comp.PrepareForBattle();
                    memberData.transform = memberObj.transform;
                }

                partyMembers.Add(memberData);
            }
        }

        if (encounterData.enemyPartyMembers != null)
        {
            for (int i = 0; i < encounterData.enemyPartyMembers.Count; i++)
            {
                PartyMemberState enemyData = encounterData.enemyPartyMembers[i];
                if (enemyData == null) continue;

                GameObject prefabToUse = (i < encounterData.enemyPrefabs.Count ? encounterData.enemyPrefabs[i] : null)
                                         ?? enemyVisualPrefab;

                if (prefabToUse == null)
                    Debug.LogWarning($"[CombatSystem] No visual prefab for enemy {i} ({enemyData.CharacterName}). enemyPrefabs.Count={encounterData.enemyPrefabs.Count}, fallback enemyVisualPrefab={(enemyVisualPrefab == null ? "null" : "set")}");
                else if (i >= enemySpawnPoints.Count)
                    Debug.LogWarning($"[CombatSystem] No spawn point for enemy {i} ({enemyData.CharacterName}). enemySpawnPoints.Count={enemySpawnPoints.Count}");
                else if (enemySpawnPoints[i] == null)
                    Debug.LogWarning($"[CombatSystem] enemySpawnPoints[{i}] is null for {enemyData.CharacterName}");

                if (prefabToUse != null && i < enemySpawnPoints.Count && enemySpawnPoints[i] != null)
                {
                    Debug.Log($"[CombatSystem] Spawning enemy visual for {enemyData.CharacterName} at {enemySpawnPoints[i].position}");
                    GameObject enemyObj = Instantiate(prefabToUse, enemySpawnPoints[i].position, Quaternion.identity);
                    enemyObj.name = $"Enemy_{enemyData.CharacterName}";

                    CharacterComponent comp = enemyObj.GetComponent<CharacterComponent>();
                    if (comp == null) comp = enemyObj.AddComponent<CharacterComponent>();
                    comp.partyMemberState = enemyData;
                    comp.PrepareForBattle();
                    enemyData.transform = enemyObj.transform;

                    if (enemyObj.GetComponent<ComplexAI>() == null)
                        enemyObj.AddComponent<ComplexAI>();
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
                enemyTurnQueue.Enqueue(enemy);
        }
    }

    // ─── Turn Flow ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Advances to the next player character's turn.
    /// This is the ONLY place that starts PlayerTurnRoutine — nothing else should.
    /// </summary>
    private void StartNextPlayerTurn()
    {
        if (CheckBattleEnd()) return;

        if (playerTurnQueue.Count == 0)
        {
            StartNextEnemyTurn();
            return;
        }

        currentCharacter = playerTurnQueue.Dequeue();

        // Remove any defend bonus from this character's previous round
        RemoveDefendBonus(currentCharacter);

        if (currentCharacter.currentHP <= 0)
        {
            StartNextPlayerTurn();
            return;
        }

        currentCharacter.ProcessStatusEffectsOnTurnStart();
        onCharacterUpdated?.Invoke(currentCharacter);

        if (!currentCharacter.CanAct())
        {
            StartNextPlayerTurn();
            return;
        }

        // Reset per-turn state
        endTurnRequested = false;
        pendingActions.Clear();
        apSnapshots.Clear();

        currentState = CombatState.PLAYER_TURN;
        onTurnStarted?.Invoke(currentCharacter);
        StartCoroutine(PlayerTurnRoutine());
    }

    private void StartNextEnemyTurn()
    {
        if (CheckBattleEnd()) return;

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
        onCharacterUpdated?.Invoke(currentCharacter);

        if (!currentCharacter.CanAct())
        {
            StartNextEnemyTurn();
            return;
        }

        currentState = CombatState.ENEMY_TURN;
        onTurnStarted?.Invoke(currentCharacter);
        StartCoroutine(EnemyTurnRoutine());
    }

    // ─── Coroutines ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sole driver of player turn completion. Waits for EndTurnAndExecuteActions to set
    /// the flag, executes queued actions, then advances the turn. Nothing else should
    /// call StartNextPlayerTurn for player turns.
    /// </summary>
    private IEnumerator PlayerTurnRoutine()
    {
        // Wait until the UI explicitly ends the turn
        while (!endTurnRequested)
            yield return null;

        if (pendingActions.Count > 0)
        {
            isExecutingActions = true;

            var orderedActions = pendingActions
                .Where(a => !a.isItem)
                .OrderByDescending(a => a.attack != null ? a.attack.actionPointCost : 0)
                .ToList();

            yield return StartCoroutine(ExecuteActionQueue(orderedActions));

            isExecutingActions = false;
        }

        StartNextPlayerTurn();
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
                targets = GetTargets(selectedAction.effects[0].targetType, selectedAction.effects[0].numberOfTargets);
        }

        if (selectedAction != null)
        {
            yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, selectedAction, targets));

            if (!CheckBattleEnd())
            {
                onActionExecuted?.Invoke(currentCharacter, selectedAction, targets);
                onCharacterUpdated?.Invoke(currentCharacter);
            }
        }

        if (!CheckBattleEnd())
            StartNextEnemyTurn();
    }

    private IEnumerator ExecuteActionQueue(List<QueuedAction> actions)
    {
        foreach (var action in actions)
        {
            if (action.isItem || action.attack == null) continue;
            if (action.targets == null || action.targets.Count == 0) continue;
            if (CheckBattleEnd()) yield break;

            yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, action.attack, action.targets));

            if (CheckBattleEnd()) yield break;

            onActionExecuted?.Invoke(currentCharacter, action.attack, action.targets);
            onCharacterUpdated?.Invoke(currentCharacter);

            yield return new WaitForSeconds(0.2f);
        }

        pendingActions.Clear();
        apSnapshots.Clear();
    }

    private IEnumerator ExecuteAttackWithAnimation(PartyMemberState user, AttackFile attack, List<PartyMemberState> targets)
    {
        isAnimating = true;

        if (attack.battleAnimation != null)
        {
            yield return attack.battleAnimation.PlayAnimation(user, targets, () => ApplyAttackEffects(user, attack, targets));
        }
        else
        {
            ApplyAttackEffects(user, attack, targets);
            yield return null;
        }

        isAnimating = false;
    }

    // ─── Public API for UI ────────────────────────────────────────────────────────

    /// <summary>
    /// Signals that the player is done acting. PlayerTurnRoutine picks this up and
    /// executes queued actions before advancing the turn. Safe to call multiple times.
    /// </summary>
    public void EndTurnAndExecuteActions()
    {
        if (currentState != CombatState.PLAYER_TURN || isExecutingActions || endTurnRequested)
            return;

        endTurnRequested = true;
    }

    public void EndPlayerTurn() => EndTurnAndExecuteActions();

    public void SelectPlayerAction(AttackFile action, List<PartyMemberState> targets)
    {
        if (currentCharacter == null || !partyMembers.Contains(currentCharacter)) return;
        if (currentState != CombatState.PLAYER_TURN || isExecutingActions || isAnimating) return;
        if (currentCharacter.currentAP < action.actionPointCost) return;

        apSnapshots.Push(currentCharacter.currentAP);
        currentCharacter.currentAP -= action.actionPointCost;
        pendingActions.Add(new QueuedAction(action, targets));
        onCharacterUpdated?.Invoke(currentCharacter);
    }

    public void UseItem(List<PartyMemberState> targets)
    {
        if (currentCharacter == null || !partyMembers.Contains(currentCharacter)) return;
        if (currentState != CombatState.PLAYER_TURN || isExecutingActions || isAnimating) return;

        const int itemAPCost = 5;
        if (currentCharacter.currentAP < itemAPCost) return;

        apSnapshots.Push(currentCharacter.currentAP);
        currentCharacter.currentAP -= itemAPCost;
        pendingActions.Add(new QueuedAction(true));
        onCharacterUpdated?.Invoke(currentCharacter);
    }

    /// <summary>
    /// Removes only the last queued action and restores exactly that action's AP cost.
    /// </summary>
    public void UndoLastAction()
    {
        if (currentCharacter == null || !partyMembers.Contains(currentCharacter)) return;
        if (currentState != CombatState.PLAYER_TURN || isExecutingActions) return;
        if (pendingActions.Count == 0 || apSnapshots.Count == 0) return;

        pendingActions.RemoveAt(pendingActions.Count - 1);
        currentCharacter.currentAP = apSnapshots.Pop();
        onCharacterUpdated?.Invoke(currentCharacter);
    }

    public bool HasPendingActions() => pendingActions.Count > 0;

    // ─── Combat Logic ─────────────────────────────────────────────────────────────

    private void ApplyAttackEffects(PartyMemberState user, AttackFile attack, List<PartyMemberState> targets)
    {
        foreach (var effect in attack.effects)
        {
            bool isSuccess = Random.Range(0, 101) <= effect.accuracy;
            bool shouldApply = (isSuccess && effect.triggersOn == EffectTrigger.OnSuccess) ||
                               (!isSuccess && effect.triggersOn == EffectTrigger.OnMiss);

            if (shouldApply)
                ApplyEffect(user, targets, effect);
        }
    }

    private void ApplyEffect(PartyMemberState user, List<PartyMemberState> targets, EffectData effect)
    {
        foreach (var target in targets)
        {
            if (target == null || target.currentHP <= 0) continue;

            switch (effect.effectType)
            {
                case EffectType.Damage:
                    {
                        int defense = IsDefending(target) ? target.Defense * 3 : target.Defense;
                        int damage = Mathf.Max(1, effect.value + user.Attack - defense);
                        target.TakeDamage(damage);
                        onCharacterUpdated?.Invoke(target);
                        break;
                    }
                case EffectType.Attack:
                    {
                        int defense = IsDefending(target) ? target.Defense * 3 : target.Defense;
                        int damage = Mathf.Max(1, user.Attack + effect.value - defense);
                        target.TakeDamage(damage);
                        onCharacterUpdated?.Invoke(target);
                        break;
                    }
                case EffectType.Heal:
                case EffectType.HP_Restore:
                    target.Heal(effect.value);
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
                    ApplyStatModifiers(target, effect.statModifiers, 1);
                    break;

                case EffectType.Debuff:
                    ApplyStatModifiers(target, effect.statModifiers, -1);
                    break;

                case EffectType.MultiHit:
                    for (int i = 0; i < effect.hitCount; i++)
                    {
                        int defense = IsDefending(target) ? target.Defense * 3 : target.Defense;
                        int damage = Mathf.Max(1, (effect.value + user.Attack - defense) / effect.hitCount);
                        target.TakeDamage(damage);
                        if (target.currentHP <= 0) break;
                    }
                    onCharacterUpdated?.Invoke(target);
                    break;
            }
        }
    }

    private void ApplyStatModifiers(PartyMemberState target, List<StatModifier> modifiers, int sign)
    {
        if (modifiers == null || modifiers.Count == 0) return;

        foreach (var modifier in modifiers)
        {
            switch (modifier.statType)
            {
                case StatType.Attack: target.ModifyAttack(modifier.valorModificador * sign); break;
                case StatType.Defense: target.ModifyDefense(modifier.valorModificador * sign); break;
            }
        }
        onCharacterUpdated?.Invoke(target);
    }

    private AttackFile GetHighestDamageAction(PartyMemberState character)
    {
        AttackFile best = null;
        int bestDamage = 0;

        foreach (var attack in character.learnedAttacks)
        {
            if (attack.partyMemberOnly && enemies.Contains(character)) continue;

            int totalDamage = attack.effects
                .Where(e => e.targetType == TargetType.Enemy &&
                            (e.effectType == EffectType.Damage || e.effectType == EffectType.Attack))
                .Sum(e => e.value);

            if (totalDamage > bestDamage)
            {
                bestDamage = totalDamage;
                best = attack;
            }
        }

        return best;
    }

    private List<PartyMemberState> GetTargets(TargetType targetType, int numberOfTargets)
    {
        var pool = (targetType == TargetType.Ally ? partyMembers : enemies)
            .Where(t => t.currentHP > 0)
            .ToList();

        if (pool.Count == 0) return new List<PartyMemberState>();

        var selected = new List<PartyMemberState>();
        int count = Mathf.Min(numberOfTargets, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            selected.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return selected;
    }

    // ─── Defend ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks a character as defending. Defend is removed at the start of that character's
    /// next turn, so it is active through all enemy attacks in the current round.
    /// </summary>
    public void ApplyDefendBonus(PartyMemberState character)
    {
        if (character == null) return;
        defendingCharacters.Add(character);
        onCharacterUpdated?.Invoke(character);
    }

    public void RemoveDefendBonus(PartyMemberState character)
    {
        if (character == null) return;
        if (defendingCharacters.Remove(character))
            onCharacterUpdated?.Invoke(character);
    }

    public bool IsDefending(PartyMemberState character) => defendingCharacters.Contains(character);

    // ─── Battle End ───────────────────────────────────────────────────────────────

    private bool CheckBattleEnd()
    {
        if (currentState == CombatState.VICTORY || currentState == CombatState.DEFEAT)
            return true;

        bool allPartyDowned = partyMembers.All(p => p.currentHP <= 0);
        bool allEnemiesDowned = enemies.All(e => e.currentHP <= 0);

        if (allEnemiesDowned)
        {
            currentState = CombatState.VICTORY;
            AwardExperience();
            onCombatEnded?.Invoke(CombatState.VICTORY);
            StartCoroutine(ReturnToMapAfterDelay(2f));
            return true;
        }

        if (allPartyDowned)
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
        if (menubutton != null) menubutton.SetActive(true);
        if (objbutton2 != null) objbutton2.SetActive(true);

        yield return new WaitForSeconds(delay);

        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        PreviousScene previousScene = FindFirstObjectByType<PreviousScene>();

        if (previousScene != null)
        {
            if (currentState == CombatState.VICTORY && encounterData != null)
                encounterData.combatVictory = true;

            MusicManager.Instance?.StopMusic();

            previousScene.LoadScene();
            SceneManager.UnloadSceneAsync("Combat");
        }
    }

    private void AwardExperience()
    {
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData == null) return;

        encounterData.combatVictory = true;
        encounterData.CalculateRewards();
        encounterData.ApplyCombatRewards();

        foreach (var member in partyMembers.Where(p => p.currentHP > 0))
            member.GainExperience(encounterData.totalExpReward);
    }

    // ─── Accessors ────────────────────────────────────────────────────────────────

    public PartyMemberState GetCurrentCharacter() => currentCharacter;
    public bool IsPlayerTurn() => currentState == CombatState.PLAYER_TURN;

    // ─── Debug ────────────────────────────────────────────────────────────────────

    public void ForceWin()
    {
        if (currentState == CombatState.VICTORY || currentState == CombatState.DEFEAT) return;
        currentState = CombatState.VICTORY;
        AwardExperience();
        onCombatEnded?.Invoke(CombatState.VICTORY);
        StartCoroutine(ReturnToMapAfterDelay(2f));
    }
}
