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

    // Events for UI
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
        Debug.Log("=== CombatSystem Started ===");

        // Check if we have encounter data
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData != null)
        {
            Debug.Log($"Found EncounterData with file: {encounterData.encounterFile?.name}");
            Debug.Log($"Party members in data: {encounterData.playerPartyMembers?.Count}");
            Debug.Log($"Enemies in file: {encounterData.encounterFile?.enemies?.Count}");

            InitializeCombatWithData(encounterData);
        }
        else
        {
            Debug.LogError("No EncounterData found! Using test data.");
            InitializeCombat();
        }
    }

    public void InitializeCombatWithData(EncounterData encounterData)
    {
        Debug.Log("=== Initializing Combat With Data ===");

        // Clear existing lists
        partyMembers.Clear();
        enemies.Clear();

        // 1. Set up party members from player data
        if (encounterData.playerPartyMembers != null && encounterData.playerPartyMembers.Count > 0)
        {
            Debug.Log($"Setting up {encounterData.playerPartyMembers.Count} party members");

            for (int i = 0; i < encounterData.playerPartyMembers.Count; i++)
            {
                CharacterData memberData = encounterData.playerPartyMembers[i];
                Debug.Log($"Party member {i}: {memberData?.characterName}");

                if (memberData == null) continue;

                // Create visual representation
                if (i < partySpawnPoints.Count && partySpawnPoints[i] != null)
                {
                    GameObject visualPrefab = partyMemberVisualPrefab;

                    if (visualPrefab != null)
                    {
                        GameObject memberObj = Instantiate(visualPrefab, partySpawnPoints[i].position, Quaternion.identity);
                        memberObj.name = $"Party_{memberData.characterName}";

                        // Add CharacterComponent and assign data
                        CharacterComponent comp = memberObj.GetComponent<CharacterComponent>();
                        if (comp == null) comp = memberObj.AddComponent<CharacterComponent>();
                        comp.characterData = memberData;

                        // Set transform reference for animations
                        memberData.transform = memberObj.transform;

                        Debug.Log($"Spawned party member: {memberData.characterName} at {partySpawnPoints[i].position}");
                    }
                }

                partyMembers.Add(memberData);
            }
        }
        else
        {
            Debug.LogError("No party members found in EncounterData!");
        }

        // 2. Set up enemies from encounter file (com proteções e logs adicionais)
        if (encounterData.encounterFile != null && encounterData.encounterFile.enemies.Count > 0)
        {
            Debug.Log($"Setting up {encounterData.encounterFile.enemies.Count} enemies");

            if (enemySpawnPoints == null || enemySpawnPoints.Count < encounterData.encounterFile.enemies.Count)
            {
                Debug.LogWarning("Not enough enemySpawnPoints for all enemies. Some enemies may fail to spawn.");
            }

            for (int i = 0; i < encounterData.encounterFile.enemies.Count; i++)
            {
                var enemyData = encounterData.encounterFile.enemies[i];

                if (enemyData.characterData == null)
                {
                    Debug.LogError($"Enemy {i} has no CharacterData!");
                    continue;
                }

                Debug.Log($"Enemy {i}: {enemyData.characterData.characterName}, Prefab: {enemyData.enemyPrefab?.name}, Level: {enemyData.level}");

                // Create a copy of the character data
                CharacterData enemyCopy = CreateCharacterDataCopy(enemyData.characterData);

                // Apply level
                while (enemyCopy.level < enemyData.level)
                {
                    enemyCopy.LevelUp();
                }

                // Override HP if specified
                if (enemyData.overrideHP > 0)
                {
                    enemyCopy.currentHP = enemyData.overrideHP;
                }

                // Add additional attacks
                foreach (var attack in enemyData.additionalAttacks)
                {
                    if (!enemyCopy.availableAttacks.Contains(attack))
                    {
                        enemyCopy.availableAttacks.Add(attack);
                    }
                }

                // Decide qual prefab usar
                GameObject prefabToUse = enemyData.enemyPrefab != null ? enemyData.enemyPrefab : enemyVisualPrefab;

                if (prefabToUse == null)
                {
                    Debug.LogError($"No prefab available for enemy {i} ('{enemyData.characterData.characterName}'). Check EncounterFile and CombatSystem inspector.");
                    // Não tente instanciar sem prefab
                    enemies.Add(enemyCopy); // ainda adiciona dados, mas sem visual
                    continue;
                }

                // Log se o prefab escolhido é igual ao de party (ajuda a detectar atribuição errada no Inspector)
                if (partyMemberVisualPrefab != null && prefabToUse == partyMemberVisualPrefab)
                {
                    Debug.LogWarning($"Enemy prefab for '{enemyData.characterData.characterName}' is the same as party prefab. Did you assign the wrong prefab in the Inspector?");
                }

                // Spawn only if spawn point exists
                if (i < enemySpawnPoints.Count && enemySpawnPoints[i] != null)
                {
                    GameObject enemyObj = Instantiate(prefabToUse, enemySpawnPoints[i].position, Quaternion.identity);
                    enemyObj.name = $"Enemy_{enemyCopy.characterName}";

                    // Ensure CharacterComponent exists and assign data
                    CharacterComponent comp = enemyObj.GetComponent<CharacterComponent>();
                    if (comp == null) comp = enemyObj.AddComponent<CharacterComponent>();
                    comp.characterData = enemyCopy;

                    // Set transform reference for animations
                    enemyCopy.transform = enemyObj.transform;

                    // Add ComplexAI for enemy behavior if missing
                    if (enemyObj.GetComponent<ComplexAI>() == null)
                    {
                        enemyObj.AddComponent<ComplexAI>();
                    }

                    Debug.Log($"Spawned enemy: {enemyCopy.characterName} at {enemySpawnPoints[i].position} (using prefab '{prefabToUse.name}')");
                }
                else
                {
                    Debug.LogError($"Cannot spawn enemy {i}: spawn point missing at index {i}");
                }

                enemies.Add(enemyCopy);
            }
        }
        else
        {
            Debug.LogError("No encounter file or enemies found!");
        }

        Debug.Log($"Final counts - Party: {partyMembers.Count}, Enemies: {enemies.Count}");

        // Start combat
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
        // Wait for player actions
        while (currentCharacter.currentAP > 0 && !isExecutingActions && !isAnimating)
        {
            yield return null; // Player selects actions via UI
        }

        // Process selected actions in order
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
        // Use ComplexAI if available
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
            onCharacterUpdated?.Invoke(currentCharacter);
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
            if (attack.partyMemberOnly && enemies.Contains(character))
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
            // Store the action with its targets
            pendingActions[action] = targets;
            Debug.Log($"Selected: {action.attackName} with {targets.Count} targets");

            // Immediately deduct AP? Or wait until execution?
            // currentCharacter.currentAP -= action.actionPointCost;
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
                onCharacterUpdated?.Invoke(currentCharacter);

                // Check if target died
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

    private void ApplyEffect(CharacterData user, List<CharacterData> targets, EffectData effect)
    {
        foreach (var target in targets)
        {
            if (target == null || target.currentHP <= 0)
                continue; // Skip dead targets

            switch (effect.effectType)
            {
                case EffectType.Damage:
                    // Calculate damage (base damage from effect + user attack - target defense)
                    int damage = Mathf.Max(1, effect.value + user.attack - target.defense);
                    target.TakeDamage(damage);
                    Debug.Log($"{target.characterName} takes {damage} damage!");

                    // Trigger update event
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Heal:
                    int healAmount = effect.value;
                    target.Heal(healAmount);
                    Debug.Log($"{target.characterName} heals for {healAmount} HP!");

                    // Trigger update event
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Attack:
                    // Attack based on user's attack stat plus effect value
                    int attackDamage = Mathf.Max(1, user.attack + effect.value - target.defense);
                    target.TakeDamage(attackDamage);
                    Debug.Log($"{target.characterName} takes {attackDamage} damage from attack!");

                    // Trigger update event
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.HP_Restore:
                    // Restore HP (like a potion)
                    target.Heal(effect.value);
                    Debug.Log($"{target.characterName} restores {effect.value} HP!");

                    // Trigger update event
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.StatusEffect:
                    // Apply status effect if we have StatusEffectData
                    if (effect.statusEffect != null)
                    {
                        target.AddStatusEffect(effect.statusEffect, user);
                        Debug.Log($"{target.characterName} afflicted with {effect.statusEffect.nomeEfeito}!");

                        // Trigger update event for status effect
                        onCharacterUpdated?.Invoke(target);
                    }
                    break;

                case EffectType.Buff:
                    // Apply buff (increase stats temporarily)
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
                        Debug.Log($"{target.characterName} receives buff!");
                        onCharacterUpdated?.Invoke(target);
                    }
                    break;

                case EffectType.Debuff:
                    // Apply debuff (decrease stats temporarily)
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
                        Debug.Log($"{target.characterName} receives debuff!");
                        onCharacterUpdated?.Invoke(target);
                    }
                    break;

                case EffectType.ManaRestore:
                    // Restore AP
                    target.currentAP = Mathf.Min(target.maxAP, target.currentAP + effect.value);
                    Debug.Log($"{target.characterName} restores {effect.value} AP!");
                    onCharacterUpdated?.Invoke(target);
                    break;

                case EffectType.Revive:
                    // Revive a fallen character
                    if (target.currentHP <= 0)
                    {
                        target.currentHP = effect.value;
                        Debug.Log($"{target.characterName} is revived with {effect.value} HP!");
                        onCharacterUpdated?.Invoke(target);
                    }
                    break;

                case EffectType.MultiHit:
                    // Multi-hit attack (hits multiple times)
                    for (int i = 0; i < effect.hitCount; i++)
                    {
                        int multiDamage = Mathf.Max(1, (effect.value + user.attack - target.defense) / effect.hitCount);
                        target.TakeDamage(multiDamage);
                        Debug.Log($"{target.characterName} takes {multiDamage} damage from hit {i + 1}!");

                        if (target.currentHP <= 0)
                            break; // Stop if target dies
                    }
                    onCharacterUpdated?.Invoke(target);
                    break;

                default:
                    Debug.LogWarning($"Unhandled effect type: {effect.effectType}");
                    break;
            }
        }

        // Check if all targets are dead and update battle state
        CheckBattleEnd();
    }

    private List<CharacterData> GetTargets(TargetType targetType, int numberOfTargets)
    {
        List<CharacterData> potentialTargets = targetType == TargetType.Ally ? partyMembers : enemies;
        potentialTargets = potentialTargets.Where(t => t.currentHP > 0).ToList();

        if (potentialTargets.Count == 0)
            return new List<CharacterData>();

        // Random selection
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
            Debug.Log("Victory! All enemies defeated!");

            // Start coroutine to return to map
            StartCoroutine(ReturnToMapAfterDelay(2f));
            return true;
        }
        else if (allPartyDowned)
        {
            currentState = CombatState.DEFEAT;
            onCombatEnded?.Invoke(CombatState.DEFEAT);
            Debug.Log("Defeat! All party members are DOWNED!");

            // Start coroutine to return to map
            StartCoroutine(ReturnToMapAfterDelay(2f));
            return true;
        }

        return false;
    }

    private IEnumerator ReturnToMapAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Find EncounterData
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();

        // Get PreviousScene component
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

        int totalExp = enemies.Sum(e => e.expValue);
        foreach (var member in partyMembers.Where(p => p.currentHP > 0))
        {
            member.GainExperience(totalExp);
        }
        Debug.Log($"Party gained {totalExp} experience!");
    }

    // Public method for UI to get current character
    public CharacterData GetCurrentCharacter()
    {
        return currentCharacter;
    }

    // Public method to check if it's player's turn
    public bool IsPlayerTurn()
    {
        return currentState == CombatState.PLAYER_TURN;
    }
}