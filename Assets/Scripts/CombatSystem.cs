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
        while (currentCharacter.currentAP > 0 && !isExecutingActions && !isAnimating)
        {
            yield return null;
        }

        if (pendingActions.Count > 0)
        {
            ExecuteActionsInOrder();
        }
        else
        {
            EndPlayerTurn();
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
            yield return StartCoroutine(ExecuteAttackWithAnimation(currentCharacter, selectedAction, targets));
            currentCharacter.currentAP -= selectedAction.actionPointCost;
            onCharacterUpdated?.Invoke(currentCharacter);
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

    public void EndPlayerTurn()
    {
        if (currentState == CombatState.PLAYER_TURN && !isExecutingActions && !isAnimating)
        {
            StartNextTurn();
        }
    }

    public void SelectPlayerAction(AttackFile action, List<CharacterData> targets)
    {
        if (currentState == CombatState.PLAYER_TURN &&
            currentCharacter.currentAP >= action.actionPointCost &&
            !isExecutingActions && !isAnimating)
        {
            pendingActions[action] = targets;

            // Immediately deduct AP? Or wait until execution?
            // Let's deduct now to prevent multiple selections
            currentCharacter.currentAP -= action.actionPointCost;
            onCharacterUpdated?.Invoke(currentCharacter);
        }
    }

    private void ExecuteActionsInOrder()
    {
        isExecutingActions = true;

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

                onActionExecuted?.Invoke(currentCharacter, action, targets);
                onCharacterUpdated?.Invoke(currentCharacter);

                foreach (var target in targets)
                {
                    if (target.currentHP <= 0)
                    {
                        onCharacterUpdated?.Invoke(target);
                    }
                }
            }

            yield return new WaitForSeconds(0.2f);
        }

        pendingActions.Clear();
        isExecutingActions = false;

        if (currentCharacter.currentAP > 0 && currentCharacter.CanAct())
        {
            currentState = CombatState.PLAYER_TURN;
            onTurnStarted?.Invoke(currentCharacter);
        }
        else
        {
            EndPlayerTurn();
        }
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
        Debug.Log($"=== Applying Attack Effects ===");
        Debug.Log($"User: {user.characterName}");
        Debug.Log($"Attack: {attack.attackName}");
        Debug.Log($"Number of effects: {attack.effects.Count}");
        Debug.Log($"Number of targets: {targets.Count}");

        foreach (var effect in attack.effects)
        {
            Debug.Log($"Processing effect: {effect.effectType}, value={effect.value}, accuracy={effect.accuracy}");

            int accuracyRoll = Random.Range(0, 101);
            bool isSuccess = accuracyRoll <= effect.accuracy;

            Debug.Log($"Accuracy roll: {accuracyRoll}, Success: {isSuccess}");

            if ((isSuccess && effect.triggersOn == EffectTrigger.OnSuccess) ||
                (!isSuccess && effect.triggersOn == EffectTrigger.OnMiss))
            {
                Debug.Log($"Effect triggered! Applying to {targets.Count} targets");
                ApplyEffect(user, targets, effect);
            }
            else
            {
                Debug.Log($"Effect did not trigger (triggerOn={effect.triggersOn})");
            }
        }

        CheckBattleEnd();
    }

    private void ApplyEffect(CharacterData user, List<CharacterData> targets, EffectData effect)
    {
        Debug.Log($"ApplyEffect: {effect.effectType} from {user.characterName}");

        foreach (var target in targets)
        {
            if (target == null || target.currentHP <= 0)
            {
                Debug.Log($"Target is null or dead, skipping");
                continue;
            }

            Debug.Log($"Target: {target.characterName}, Current HP: {target.currentHP}, Defense: {target.defense}");

            switch (effect.effectType)
            {
                case EffectType.Damage:
                    int damage = Mathf.Max(1, effect.value + user.attack - target.defense);
                    Debug.Log($"Damage calculation: {effect.value} + {user.attack} - {target.defense} = {damage}");

                    int oldHP = target.currentHP;
                    target.TakeDamage(damage);
                    Debug.Log($"HP changed: {oldHP} -> {target.currentHP}");

                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Attack:
                    int attackDamage = Mathf.Max(1, user.attack + effect.value - target.defense);
                    Debug.Log($"Attack damage calculation: {user.attack} + {effect.value} - {target.defense} = {attackDamage}");

                    int oldAttackHP = target.currentHP;
                    target.TakeDamage(attackDamage);
                    Debug.Log($"HP changed: {oldAttackHP} -> {target.currentHP}");

                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.HP_Restore:
                    target.Heal(effect.value);
                    onCharacterUpdated?.Invoke(target);
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

                case EffectType.MultiHit:
                    for (int i = 0; i < effect.hitCount; i++)
                    {
                        int multiDamage = Mathf.Max(1, (effect.value + user.attack - target.defense) / effect.hitCount);
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