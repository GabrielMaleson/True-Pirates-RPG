using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatUIManager : MonoBehaviour
{
    [Header("References")]
    public CombatSystem combatSystem;
    public SistemaInventario playerInventory;

    [Header("UI Parents")]
    public Transform partyUIParent;
    public Transform enemyUIParent;

    [Header("Prefabs")]
    public GameObject partyMemberUIPrefab;
    public GameObject enemyUIPrefab;
    public GameObject actionButtonPrefab;

    [Header("Action Menu Panels - Scene Based")]
    public GameObject actionMenuPanel;        // Main menu with Attacks/Items/Defend
    public Transform attackButtonGrid;        // Grid in scene where attack buttons spawn
    public Transform itemButtonGrid;          // Grid in scene where item buttons spawn

    [Header("Action Menu Buttons")]
    public Button attacksMenuButton;
    public Button itemsMenuButton;
    public Button defendMenuButton;

    [Header("Text Displays")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject waitPanel;

    private Dictionary<CharacterData, CharacterUI> partyUIDictionary = new Dictionary<CharacterData, CharacterUI>();
    private Dictionary<CharacterData, EnemyUI> enemyUIDictionary = new Dictionary<CharacterData, EnemyUI>();
    private Dictionary<CharacterData, GameObject> partyVisualDictionary = new Dictionary<CharacterData, GameObject>();
    private Dictionary<CharacterData, GameObject> enemyVisualDictionary = new Dictionary<CharacterData, GameObject>();

    private CharacterData currentCharacter;
    private AttackFile selectedAttack;
    private DadosItem selectedItem;
    private int remainingTargetsToSelect = 0;
    private List<CharacterData> selectedTargets = new List<CharacterData>();
    private bool isInitialized = false;
    private bool isTargeting = false;

    private void Start()
    {
        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<SistemaInventario>();

        if (combatSystem != null)
        {
            combatSystem.onTurnStarted += OnTurnStarted;
            combatSystem.onCharacterUpdated += OnCharacterUpdated;
            combatSystem.onCombatEnded += OnCombatEnded;
        }

        // Set up menu button listeners
        if (attacksMenuButton != null)
            attacksMenuButton.onClick.AddListener(OnAttacksSelected);

        if (itemsMenuButton != null)
            itemsMenuButton.onClick.AddListener(OnItemsSelected);

        if (defendMenuButton != null)
            defendMenuButton.onClick.AddListener(OnDefendSelected);

        StartCoroutine(InitializeAfterCombatSystem());
    }

    private IEnumerator InitializeAfterCombatSystem()
    {
        while (combatSystem == null)
        {
            combatSystem = FindFirstObjectByType<CombatSystem>();
            yield return new WaitForSeconds(0.1f);
        }

        while (combatSystem.partyMembers == null || combatSystem.partyMembers.Count == 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        CreateAllCharacterUI();
        isInitialized = true;

        // Hide all action panels initially
        HideAllActionPanels();

        CharacterData currentChar = combatSystem.GetCurrentCharacter();
        if (currentChar != null)
        {
            OnTurnStarted(currentChar);
        }
    }

    private void HideAllActionPanels()
    {
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        if (waitPanel != null) waitPanel.SetActive(false);
    }

    private void CreateAllCharacterUI()
    {
        foreach (Transform child in partyUIParent) Destroy(child.gameObject);
        foreach (Transform child in enemyUIParent) Destroy(child.gameObject);
        partyUIDictionary.Clear();
        enemyUIDictionary.Clear();
        partyVisualDictionary.Clear();
        enemyVisualDictionary.Clear();

        FindAllVisualObjects();

        foreach (var member in combatSystem.partyMembers)
        {
            if (member != null && member.currentHP > 0)
            {
                GameObject uiObj = Instantiate(partyMemberUIPrefab, partyUIParent);
                CharacterUI characterUI = uiObj.GetComponent<CharacterUI>();

                GameObject visualObj = null;
                if (partyVisualDictionary.ContainsKey(member))
                {
                    visualObj = partyVisualDictionary[member];
                }

                if (characterUI != null)
                {
                    characterUI.Initialize(member, visualObj, this);
                    partyUIDictionary[member] = characterUI;
                }
            }
        }

        foreach (var enemy in combatSystem.enemies)
        {
            if (enemy != null && enemy.currentHP > 0)
            {
                GameObject uiObj = Instantiate(enemyUIPrefab, enemyUIParent);
                EnemyUI enemyUI = uiObj.GetComponent<EnemyUI>();

                GameObject visualObj = null;
                if (enemyVisualDictionary.ContainsKey(enemy))
                {
                    visualObj = enemyVisualDictionary[enemy];
                }

                if (enemyUI != null)
                {
                    enemyUI.Initialize(enemy, visualObj);
                    enemyUIDictionary[enemy] = enemyUI;
                }
            }
        }
    }

    private void FindAllVisualObjects()
    {
        CharacterComponent[] allCharacters = FindObjectsByType<CharacterComponent>(FindObjectsSortMode.None);

        foreach (var characterComp in allCharacters)
        {
            if (characterComp != null && characterComp.characterData != null)
            {
                CharacterData data = characterComp.characterData;

                if (combatSystem.partyMembers.Contains(data))
                {
                    partyVisualDictionary[data] = characterComp.gameObject;

                    // Add TargetSelector to party members too (for ally targeting)
                    if (characterComp.GetComponent<TargetSelector>() == null)
                    {
                        TargetSelector selector = characterComp.gameObject.AddComponent<TargetSelector>();
                        selector.Initialize(this);
                    }
                }
                else if (combatSystem.enemies.Contains(data))
                {
                    enemyVisualDictionary[data] = characterComp.gameObject;

                    if (characterComp.GetComponent<TargetSelector>() == null)
                    {
                        TargetSelector selector = characterComp.gameObject.AddComponent<TargetSelector>();
                        selector.Initialize(this);
                    }
                }
            }
        }
    }

    private void OnTurnStarted(CharacterData character)
    {
        currentCharacter = character;
        turnText.text = $"{character.characterName}'s Turn";

        // Hide all targeting and action panels
        DisableAllTargeting();
        HideAllActionPanels();
        ClearAllButtons();

        if (!isInitialized) return;

        if (combatSystem.partyMembers.Contains(character))
        {
            // Show action menu for player characters
            actionMenuPanel.SetActive(true);
            statusText.text = "Choose an action";
            waitPanel.SetActive(false);
        }
        else
        {
            // Enemy turn
            waitPanel.SetActive(true);
            statusText.text = $"{character.characterName} is thinking...";
        }
    }

    private void ClearAllButtons()
    {
        // Clear attack buttons
        if (attackButtonGrid != null)
        {
            foreach (Transform child in attackButtonGrid)
            {
                Destroy(child.gameObject);
            }
            attackButtonGrid.gameObject.SetActive(false);
        }

        // Clear item buttons
        if (itemButtonGrid != null)
        {
            foreach (Transform child in itemButtonGrid)
            {
                Destroy(child.gameObject);
            }
            itemButtonGrid.gameObject.SetActive(false);
        }
    }
    private void OnAttacksSelected()
    {
        if (currentCharacter == null) return;

        // Hide action menu
        actionMenuPanel.SetActive(false);

        // Show attack grid, hide item grid
        if (attackButtonGrid != null)
            attackButtonGrid.gameObject.SetActive(true);
        if (itemButtonGrid != null)
            itemButtonGrid.gameObject.SetActive(false);

        // Clear old attack buttons
        if (attackButtonGrid != null)
        {
            foreach (Transform child in attackButtonGrid)
            {
                Destroy(child.gameObject);
            }

            // Create attack buttons for current character
            foreach (var attack in currentCharacter.availableAttacks)
            {
                if (attack == null) continue;
                if (attack.actionPointCost <= currentCharacter.currentAP)
                {
                    GameObject btnObj = Instantiate(actionButtonPrefab, attackButtonGrid);
                    ActionButton btn = btnObj.GetComponent<ActionButton>();
                    if (btn != null)
                    {
                        btn.Initialize(attack, () => OnAttackSelected(attack));
                    }
                }
            }

            // Add back button
            CreateBackButton(attackButtonGrid);
        }
    }

    private void OnItemsSelected()
    {
        if (playerInventory == null) return;

        // Hide action menu
        actionMenuPanel.SetActive(false);

        // Show item grid, hide attack grid
        if (itemButtonGrid != null)
            itemButtonGrid.gameObject.SetActive(true);
        if (attackButtonGrid != null)
            attackButtonGrid.gameObject.SetActive(false);

        // Clear old item buttons
        if (itemButtonGrid != null)
        {
            foreach (Transform child in itemButtonGrid)
            {
                Destroy(child.gameObject);
            }

            // Create item buttons from inventory (only usable in battle)
            foreach (var slot in playerInventory.inventario)
            {
                if (slot.dadosDoItem != null && slot.quantidade > 0 &&
                    slot.dadosDoItem.usavelEmBatalha && slot.dadosDoItem.ehConsumivel)
                {
                    GameObject btnObj = Instantiate(actionButtonPrefab, itemButtonGrid);
                    ActionButton btn = btnObj.GetComponent<ActionButton>();
                    if (btn != null)
                    {
                        btn.Initialize(slot.dadosDoItem, () => OnItemSelected(slot.dadosDoItem));
                    }
                }
            }

            // Add back button
            CreateBackButton(itemButtonGrid);
        }
    }


    private void OnDefendSelected()
    {
        if (currentCharacter == null) return;

        // Hide action menu and both grids
        actionMenuPanel.SetActive(false);
        if (attackButtonGrid != null)
            attackButtonGrid.gameObject.SetActive(false);
        if (itemButtonGrid != null)
            itemButtonGrid.gameObject.SetActive(false);

        // Defend action: triple defense for this turn, use all AP
        StartCoroutine(ExecuteDefend());
    }


    private IEnumerator ExecuteDefend()
    {
        // Hide action menu
        actionMenuPanel.SetActive(false);

        // Store original defense
        int originalDefense = currentCharacter.defense;

        // Triple defense
        currentCharacter.defense *= 3;
        OnCharacterUpdated(currentCharacter);

        statusText.text = $"{currentCharacter.characterName} defends!";

        // Use all AP
        currentCharacter.currentAP = 0;
        OnCharacterUpdated(currentCharacter);

        yield return new WaitForSeconds(1f);

        // Restore defense
        currentCharacter.defense = originalDefense;
        OnCharacterUpdated(currentCharacter);

        // End turn
        combatSystem.EndPlayerTurn();
    }

    private void CreateBackButton(Transform parentGrid)
    {
        GameObject backBtnObj = Instantiate(actionButtonPrefab, parentGrid);
        ActionButton backBtn = backBtnObj.GetComponent<ActionButton>();
        if (backBtn != null)
        {
            backBtn.InitializeAsBack(() => {
                // Clear this grid
                foreach (Transform child in parentGrid)
                {
                    Destroy(child.gameObject);
                }
                // Hide both grids
                if (attackButtonGrid != null)
                    attackButtonGrid.gameObject.SetActive(false);
                if (itemButtonGrid != null)
                    itemButtonGrid.gameObject.SetActive(false);
                // Show action menu again
                actionMenuPanel.SetActive(true);
            });
        }
    }

    private void OnAttackSelected(AttackFile attack)
    {
        selectedAttack = attack;
        selectedItem = null;

        // Clear attack buttons
        if (attackButtonGrid != null)
        {
            foreach (Transform child in attackButtonGrid)
            {
                Destroy(child.gameObject);
            }
        }

        StartTargeting(attack);
    }

    private void OnItemSelected(DadosItem item)
    {
        selectedItem = item;
        selectedAttack = null;

        // Clear item buttons
        if (itemButtonGrid != null)
        {
            foreach (Transform child in itemButtonGrid)
            {
                Destroy(child.gameObject);
            }
        }

        if (item.efeitos != null && item.efeitos.Count > 0)
        {
            // Use first effect to determine targeting
            var effect = item.efeitos[0];
            TargetType targetType = effect.targetType;
            int numberOfTargets = effect.numberOfTargets;

            StartTargeting(targetType, numberOfTargets, isItem: true);
        }
    }

    private void StartTargeting(AttackFile attack)
    {
        if (attack.effects.Count == 0) return;

        TargetType targetType = attack.effects[0].targetType;
        int numberOfTargets = attack.effects[0].numberOfTargets;

        StartTargeting(targetType, numberOfTargets, isItem: false);
    }

    private void StartTargeting(TargetType targetType, int numberOfTargets, bool isItem)
    {
        remainingTargetsToSelect = numberOfTargets;
        selectedTargets.Clear();
        isTargeting = true;

        statusText.text = $"Select {remainingTargetsToSelect} target(s)";

        // Enable targeting on appropriate characters
        if (targetType == TargetType.Ally)
        {
            EnableTargetingOnCharacters(combatSystem.partyMembers);
        }
        else
        {
            EnableTargetingOnCharacters(combatSystem.enemies);
        }
    }

    private void EnableTargetingOnCharacters(List<CharacterData> characters)
    {
        foreach (var character in characters)
        {
            if (character.currentHP <= 0) continue;

            GameObject visualObj = null;
            if (combatSystem.partyMembers.Contains(character) && partyVisualDictionary.ContainsKey(character))
            {
                visualObj = partyVisualDictionary[character];
            }
            else if (combatSystem.enemies.Contains(character) && enemyVisualDictionary.ContainsKey(character))
            {
                visualObj = enemyVisualDictionary[character];
            }

            if (visualObj != null)
            {
                TargetSelector selector = visualObj.GetComponent<TargetSelector>();
                if (selector != null)
                {
                    selector.EnableTargeting(character, OnTargetSelected);
                }
            }
        }
    }

    private void DisableAllTargeting()
    {
        isTargeting = false;

        foreach (var visualObj in partyVisualDictionary.Values)
        {
            if (visualObj != null)
            {
                TargetSelector selector = visualObj.GetComponent<TargetSelector>();
                if (selector != null)
                    selector.DisableTargeting();
            }
        }

        foreach (var visualObj in enemyVisualDictionary.Values)
        {
            if (visualObj != null)
            {
                TargetSelector selector = visualObj.GetComponent<TargetSelector>();
                if (selector != null)
                    selector.DisableTargeting();
            }
        }
    }

    private void OnTargetSelected(CharacterData target)
    {
        if (!isTargeting) return;

        if (!selectedTargets.Contains(target))
        {
            selectedTargets.Add(target);
            remainingTargetsToSelect--;

            statusText.text = $"Selected {selectedTargets.Count}. {remainingTargetsToSelect} more needed";

            if (remainingTargetsToSelect <= 0)
            {
                ExecuteSelectedAction();
            }
        }
    }

    private void ExecuteSelectedAction()
    {
        isTargeting = false;
        DisableAllTargeting();

        // Don't execute here - just pass to combat system for queueing
        if (selectedAttack != null && selectedTargets.Count > 0)
        {
            combatSystem.SelectPlayerAction(selectedAttack, selectedTargets);
        }
        else if (selectedItem != null && selectedTargets.Count > 0)
        {
            // Use item on targets (items execute immediately)
            UseItemOnTargets(selectedItem, selectedTargets);
        }

        selectedAttack = null;
        selectedItem = null;
        selectedTargets.Clear();

        // Hide both grids
        if (attackButtonGrid != null)
            attackButtonGrid.gameObject.SetActive(false);
        if (itemButtonGrid != null)
            itemButtonGrid.gameObject.SetActive(false);

        // Update UI
        if (currentCharacter != null)
        {
            OnCharacterUpdated(currentCharacter);
        }

        // Return to action menu if character still has AP
        if (currentCharacter != null && currentCharacter.currentAP > 0)
        {
            actionMenuPanel.SetActive(true);
            statusText.text = "Select another action or Wait";
        }
        else
        {
            // No AP left, automatically end turn and execute queued actions
            waitPanel.SetActive(true);
            statusText.text = "No AP remaining...";
            combatSystem.EndTurnAndExecuteActions();
        }
    }

    private void UseItemOnTargets(DadosItem item, List<CharacterData> targets)
    {
        if (playerInventory == null) return;

        foreach (var target in targets)
        {
            if (target == null || target.currentHP <= 0) continue;

            foreach (var effect in item.efeitos)
            {
                // Check accuracy
                int accuracyRoll = Random.Range(0, 101);
                bool isSuccess = accuracyRoll <= effect.accuracy;

                if ((isSuccess && effect.triggersOn == EffectTrigger.OnSuccess) ||
                    (!isSuccess && effect.triggersOn == EffectTrigger.OnMiss))
                {
                    // Apply item effects
                    switch (effect.tipoEfeito)
                    {
                        case EffectType.Heal:
                        case EffectType.HP_Restore:
                            target.Heal(effect.valor);
                            break;

                        case EffectType.ManaRestore:
                            target.currentAP = Mathf.Min(target.maxAP, target.currentAP + effect.valor);
                            break;

                        case EffectType.Revive:
                            if (target.currentHP <= 0)
                            {
                                target.currentHP = effect.valor;
                            }
                            break;

                        case EffectType.StatusEffect:
                            if (effect.statusEffect != null)
                            {
                                target.AddStatusEffect(effect.statusEffect, combatSystem.GetCurrentCharacter());
                            }
                            break;
                    }
                }
            }

            // Update UI for this target
            OnCharacterUpdated(target);
        }

        // Remove item from inventory
        if (playerInventory != null)
        {
            playerInventory.RemoverItem(item, 1);
            statusText.text = $"Used {item.nomeDoItem}!";
        }
    }

    private void OnCharacterUpdated(CharacterData character)
    {
        if (!isInitialized) return;

        if (partyUIDictionary.ContainsKey(character))
            partyUIDictionary[character].UpdateDisplay();
        if (enemyUIDictionary.ContainsKey(character))
            enemyUIDictionary[character].UpdateDisplay();

        if (character.currentHP <= 0)
        {
            if (partyUIDictionary.ContainsKey(character))
                partyUIDictionary[character].SetDefeated();
            if (enemyUIDictionary.ContainsKey(character))
                enemyUIDictionary[character].SetDefeated();

            if (character == currentCharacter)
            {
                DisableAllTargeting();
                HideAllActionPanels();
            }
        }
    }

    private void OnCombatEnded(CombatState result)
    {
        waitPanel.SetActive(false);
        DisableAllTargeting();
        HideAllActionPanels();
        ClearAllButtons();

        if (result == CombatState.VICTORY)
            statusText.text = "Victory!";
        else if (result == CombatState.DEFEAT)
            statusText.text = "Defeat...";
    }

    private void OnDestroy()
    {
        if (combatSystem != null)
        {
            combatSystem.onTurnStarted -= OnTurnStarted;
            combatSystem.onCharacterUpdated -= OnCharacterUpdated;
            combatSystem.onCombatEnded -= OnCombatEnded;
        }

        if (attacksMenuButton != null)
            attacksMenuButton.onClick.RemoveListener(OnAttacksSelected);
        if (itemsMenuButton != null)
            itemsMenuButton.onClick.RemoveListener(OnItemsSelected);
        if (defendMenuButton != null)
            defendMenuButton.onClick.RemoveListener(OnDefendSelected);
    }
}