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
    [Header("Battle Animation")]
    public TextMeshProUGUI battleAnimationText; // Add this with other headers

    [Header("Prefabs")]
    public GameObject partyMemberUIPrefab;
    public GameObject enemyUIPrefab;
    public GameObject actionButtonPrefab;

    [Header("Action Menu")]
    public GameObject actionMenuPanel;
    public Button attacksMenuButton;
    public Button itemsMenuButton;
    public Button defendMenuButton;
    public Button waitMenuButton;
    public Button undoButton;

    [Header("Attack UI")]
    public GameObject attackPanel;
    public Transform attackButtonGrid;
    public Button cancelAttackButton;

    [Header("Item UI")]
    public GameObject itemPanel;
    public Transform itemButtonGrid;
    public Button cancelItemButton;

    [Header("Targeting UI")]
    public GameObject targetingBackButton;    // Back button that appears during targeting
    public GameObject targetingPanel;         // Optional panel to show during targeting
    private Button targetingBackButtonComponent; // Cache the button component

    [Header("AP Bar")]
    public Image sharedApBar;
    public Image sharedApBar2;
    public TextMeshProUGUI sharedApText;

    [Header("Text Displays")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject waitPanel;

    [Header("Attack Info Box")]
    public TextMeshProUGUI attackInfoPlaceholder; // "Attack info Here" — hidden while info is shown
    public GameObject attackInfoContent;          // container with name/desc/cost — hidden while placeholder is shown
    public TextMeshProUGUI attackInfoName;
    public TextMeshProUGUI attackInfoEffects;
    public TextMeshProUGUI attackInfoAPCost;

    [Header("Debug")]
    public Button winButton;

    private Dictionary<PartyMemberState, CharacterUI> partyUIDictionary = new Dictionary<PartyMemberState, CharacterUI>();
    private Dictionary<PartyMemberState, EnemyUI> enemyUIDictionary = new Dictionary<PartyMemberState, EnemyUI>();
    private Dictionary<PartyMemberState, GameObject> partyVisualDictionary = new Dictionary<PartyMemberState, GameObject>();
    private Dictionary<PartyMemberState, GameObject> enemyVisualDictionary = new Dictionary<PartyMemberState, GameObject>();

    private PartyMemberState currentCharacter;
    private AttackFile selectedAttack;
    private DadosItem selectedItem;
    private int remainingTargetsToSelect = 0;
    private List<PartyMemberState> selectedTargets = new List<PartyMemberState>();
    private bool isInitialized = false;
    private bool wasAttackPanelOpen = false;
    private bool isTargeting = false;

    private void Start()
    {
        ForceInitialUIState();

        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<SistemaInventario>();

        if (combatSystem != null)
        {
            combatSystem.onTurnStarted += OnTurnStarted;
            combatSystem.onCharacterUpdated += OnCharacterUpdated;
            combatSystem.onCombatEnded += OnCombatEnded;
            combatSystem.onAttackStarted += OnAttackStarted;
            combatSystem.onAttackFinished += OnAttackFinished;
            combatSystem.onDamageTaken += OnDamageTaken;
        }
        if (battleAnimationText != null)
        {
            BattleAnimationData.Initialize(battleAnimationText, this);
            Debug.Log("Battle animation text initialized");
        }

        // Set up menu button listeners
        if (attacksMenuButton != null)
            attacksMenuButton.onClick.AddListener(OnAttacksSelected);

        if (itemsMenuButton != null)
            itemsMenuButton.onClick.AddListener(OnItemsSelected);

        if (defendMenuButton != null)
            defendMenuButton.onClick.AddListener(OnDefendSelected);

        if (waitMenuButton != null)
            waitMenuButton.onClick.AddListener(OnWaitSelected);

        if (undoButton != null)
            undoButton.onClick.AddListener(OnUndoSelected);

        if (cancelAttackButton != null)
            cancelAttackButton.onClick.AddListener(OnCancelGridSelected);
        if (cancelItemButton != null)
            cancelItemButton.onClick.AddListener(OnCancelGridSelected);
        if (winButton != null)
            winButton.onClick.AddListener(() => combatSystem?.ForceWin());
        if (targetingPanel != null)
            targetingPanel.SetActive(false);

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

        PartyMemberState currentChar = combatSystem.GetCurrentCharacter();
        if (currentChar != null)
        {
            OnTurnStarted(currentChar);
        }
    }

    private void ForceInitialUIState()
    {
        // Activate parent chains first so children can be shown/hidden correctly
        ActivateParentChain(actionMenuPanel != null ? actionMenuPanel.transform : null);
        ActivateParentChain(waitPanel != null ? waitPanel.transform : null);
        ActivateParentChain(attackPanel != null ? attackPanel.transform : null);
        ActivateParentChain(attackButtonGrid);
        ActivateParentChain(itemPanel != null ? itemPanel.transform : null);
        ActivateParentChain(itemButtonGrid);
        ActivateParentChain(targetingBackButton != null ? targetingBackButton.transform : null);
        ActivateParentChain(targetingPanel != null ? targetingPanel.transform : null);

        // Panels that must start hidden
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        if (waitPanel != null) waitPanel.SetActive(false);
        if (attackPanel != null) attackPanel.SetActive(false);
        if (itemPanel != null) itemPanel.SetActive(false);
        if (targetingBackButton != null) targetingBackButton.SetActive(false);
        if (targetingPanel != null) targetingPanel.SetActive(false);

        // Clear and hide button grids
        if (attackButtonGrid != null)
        {
            foreach (Transform child in attackButtonGrid)
                Destroy(child.gameObject);
            attackButtonGrid.gameObject.SetActive(false);
        }
        if (itemButtonGrid != null)
        {
            foreach (Transform child in itemButtonGrid)
                Destroy(child.gameObject);
            itemButtonGrid.gameObject.SetActive(false);
        }

        // Buttons that must start hidden
        if (undoButton != null) undoButton.gameObject.SetActive(false);
        if (cancelAttackButton != null) cancelAttackButton.gameObject.SetActive(false);
        if (cancelItemButton != null) cancelItemButton.gameObject.SetActive(false);

        // Clear all text fields
        if (turnText != null) turnText.text = "";
        if (statusText != null) statusText.text = "";
        if (battleAnimationText != null) battleAnimationText.text = "";

        // Info box starts with placeholder visible
        ClearAttackInfo();
    }

    private void ActivateParentChain(Transform t)
    {
        if (t == null) return;
        Transform parent = t.parent;
        while (parent != null)
        {
            parent.gameObject.SetActive(true);
            parent = parent.parent;
        }
    }

    private void HideAllActionPanels()
    {
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        if (waitPanel != null) waitPanel.SetActive(false);
        if (targetingBackButton != null) targetingBackButton.SetActive(false);
        if (targetingPanel != null) targetingPanel.SetActive(false);
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
            if (characterComp != null && characterComp.partyMemberState != null)
            {
                PartyMemberState data = characterComp.partyMemberState;

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

    private void ShowAttackInfo(AttackFile attack)
    {
        if (attackInfoPlaceholder != null) attackInfoPlaceholder.gameObject.SetActive(false);
        if (attackInfoContent     != null) attackInfoContent.SetActive(true);
        if (attackInfoName        != null) attackInfoName.text        = attack.attackName;
        if (attackInfoEffects != null) attackInfoEffects.text = BuildEffectsText(attack);
        if (attackInfoAPCost      != null) attackInfoAPCost.text      = $"AP: {attack.actionPointCost}";
    }

    private static string BuildEffectsText(AttackFile attack)
    {
        if (attack.effects == null || attack.effects.Count == 0) return "";

        var lines = new System.Text.StringBuilder();
        foreach (var e in attack.effects)
        {
            switch (e.effectType)
            {
                case EffectType.Damage:
                case EffectType.Attack:
                {
                    string line = $"Dano: {e.value}";
                    if (e.accuracy < 100)       line += $" | Precisão: {e.accuracy}%";
                    if (e.numberOfTargets > 1)  line += $" | {e.numberOfTargets} golpes";
                    lines.AppendLine(line);
                    break;
                }
                case EffectType.MultiHit:
                {
                    string line = $"{e.hitCount} golpes | Dano: {e.value / Mathf.Max(1, e.hitCount)} cada";
                    if (e.accuracy < 100) line += $" | Precisão: {e.accuracy}%";
                    lines.AppendLine(line);
                    break;
                }
                case EffectType.Heal:
                case EffectType.HP_Restore:
                {
                    string line = $"Cura: {e.value}";
                    if (e.numberOfTargets > 1) line += $" | Alvos: {e.numberOfTargets}";
                    lines.AppendLine(line);
                    break;
                }
                case EffectType.ManaRestore:
                {
                    string line = $"Restaura AP: {e.value}";
                    if (e.numberOfTargets > 1) line += $" | Alvos: {e.numberOfTargets}";
                    lines.AppendLine(line);
                    break;
                }
                case EffectType.Revive:
                    lines.AppendLine($"Revive com {e.value} HP");
                    break;
                case EffectType.StatusEffect:
                {
                    string statusName = e.statusEffect != null ? e.statusEffect.effectName : "?";
                    string line = $"Aplica: {statusName}";
                    if (e.accuracy < 100) line += $" | Precisão: {e.accuracy}%";
                    lines.AppendLine(line);
                    break;
                }
                case EffectType.Buff:
                    lines.AppendLine("Melhora atributos");
                    break;
                case EffectType.Debuff:
                {
                    string line = "Reduz atributos";
                    if (e.accuracy < 100) line += $" | Precisão: {e.accuracy}%";
                    lines.AppendLine(line);
                    break;
                }
            }
        }
        return lines.ToString().TrimEnd();
    }

    // Fully hides the info box — called when the attack panel is closed or on turn start
    private void ClearAttackInfo()
    {
        if (attackInfoPlaceholder != null) attackInfoPlaceholder.gameObject.SetActive(false);
        if (attackInfoContent     != null) attackInfoContent.SetActive(false);
    }

    // Shows placeholder, hides content — called when attack panel opens or hover exits
    private void ResetAttackInfoToPlaceholder()
    {
        if (attackInfoPlaceholder != null) attackInfoPlaceholder.gameObject.SetActive(true);
        if (attackInfoContent     != null) attackInfoContent.SetActive(false);
    }

    private void RefreshSharedApBar(PartyMemberState character)
    {
        float pct = Mathf.Clamp01((float)character.currentAP / character.MaxAP);
        if (sharedApBar != null)
        {
            sharedApBar.type = Image.Type.Filled;
            sharedApBar.fillMethod = Image.FillMethod.Horizontal;
            sharedApBar.fillAmount = pct;
        }
        if (sharedApBar2 != null)
        {
            sharedApBar2.type = Image.Type.Filled;
            sharedApBar2.fillMethod = Image.FillMethod.Horizontal;
            sharedApBar2.fillAmount = pct;
        }
        if (sharedApText != null)
            sharedApText.text = $"AP: {character.currentAP}/{character.MaxAP}";
    }

    private void ClearSharedApBar()
    {
        if (sharedApBar  != null) sharedApBar.fillAmount  = 0f;
        if (sharedApBar2 != null) sharedApBar2.fillAmount = 0f;
        if (sharedApText != null) sharedApText.text = "";
    }

    private void OnDamageTaken(PartyMemberState target, int amount)
    {
        if (partyUIDictionary.TryGetValue(target, out CharacterUI cui))
            cui.ShowDamage(amount);
        else if (enemyUIDictionary.TryGetValue(target, out EnemyUI eui))
            eui.ShowDamage(amount);
    }

    private void OnAttackStarted(PartyMemberState attacker)
    {
        // Enemies show their selected portrait only while actively attacking
        if (enemyUIDictionary.TryGetValue(attacker, out EnemyUI enemyUI))
            enemyUI.SetSelected(true);
    }

    private void OnAttackFinished(PartyMemberState attacker)
    {
        if (enemyUIDictionary.TryGetValue(attacker, out EnemyUI enemyUI))
            enemyUI.SetSelected(false);
    }

    private void OnTurnStarted(PartyMemberState character)
    {
        currentCharacter = character;
        if (turnText != null) turnText.text = $"{character.CharacterName}'s Turn";

        // Hide all targeting and action panels
        DisableAllTargeting();
        HideAllActionPanels();
        ClearAllButtons();
        ClearAttackInfo();

        if (!isInitialized) return;

        // Reset selected state on all portraits; activate only the current party member's
        foreach (var kvp in partyUIDictionary) kvp.Value.SetSelected(false);
        foreach (var kvp in enemyUIDictionary) kvp.Value.SetSelected(false);

        if (combatSystem.partyMembers.Contains(character))
        {
            if (partyUIDictionary.TryGetValue(character, out CharacterUI activeCui))
                activeCui.SetSelected(true);
            RefreshSharedApBar(character);
        }
        else
        {
            ClearSharedApBar();
        }

        if (combatSystem.partyMembers.Contains(character))
        {
            // Show action menu for player characters
            actionMenuPanel.SetActive(true);

            // Make sure undo button is hidden at start of turn
            if (undoButton != null)
                undoButton.gameObject.SetActive(false);

            // Update status text based on AP
            if (statusText != null)
            {
                statusText.text = character.currentAP == character.MaxAP
                    ? "Escolha uma ação"
                    : $"AP: {character.currentAP}/{character.MaxAP} - Escolha outra ação ou Espere";
            }

            UpdateActionMenuButtons();
            if (waitPanel != null) waitPanel.SetActive(false);
        }
        else
        {
            // Enemy turn
            if (waitPanel != null) waitPanel.SetActive(true);
            if (statusText != null) statusText.text = $"{character.CharacterName} está pensando...";
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

        if (cancelAttackButton != null) cancelAttackButton.gameObject.SetActive(false);
        if (cancelItemButton != null) cancelItemButton.gameObject.SetActive(false);
        if (attackPanel != null) attackPanel.SetActive(false);
        if (itemPanel != null) itemPanel.SetActive(false);
    }

    private void OnAttacksSelected()
    {
        if (currentCharacter == null) return;
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
        ShowAttackGrid();
    }

    private void ShowAttackGrid()
    {
        // Show placeholder in the info box now that the attack panel is open
        ResetAttackInfoToPlaceholder();

        // Hide action menu
        actionMenuPanel.SetActive(false);

        // Show attack grid, hide item grid
        if (attackButtonGrid != null)
        {
            attackButtonGrid.gameObject.SetActive(true);

            // Clear old attack buttons
            foreach (Transform child in attackButtonGrid)
            {
                Destroy(child.gameObject);
            }

            // Create attack buttons for current character based on CURRENT AP
            foreach (var attack in currentCharacter.learnedAttacks)
            {
                if (attack == null) continue;
                if (attack.actionPointCost <= currentCharacter.currentAP)
                {
                    GameObject btnObj = Instantiate(actionButtonPrefab, attackButtonGrid);
                    ActionButton btn = btnObj.GetComponent<ActionButton>();
                    if (btn != null)
                    {
                        btn.Initialize(attack, () => OnAttackSelected(attack),
                            hoverEnter: ShowAttackInfo,
                            hoverExit:  ResetAttackInfoToPlaceholder);
                    }
                }
            }

            // Add back button inside grid only if no standalone cancel button is assigned
            if (cancelAttackButton == null)
                CreateBackButton(attackButtonGrid);
        }

        if (attackPanel != null) attackPanel.SetActive(true);
        if (cancelAttackButton != null) cancelAttackButton.gameObject.SetActive(true);
        if (itemPanel != null) itemPanel.SetActive(false);
        if (cancelItemButton != null) cancelItemButton.gameObject.SetActive(false);
        if (itemButtonGrid != null) itemButtonGrid.gameObject.SetActive(false);
    }

    private void OnItemsSelected()
    {
        if (playerInventory == null) return;
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
        ShowItemGrid();
    }

    private void ShowItemGrid()
    {
        // Hide action menu
        actionMenuPanel.SetActive(false);

        // Show item grid, hide attack grid
        if (itemButtonGrid != null)
        {
            itemButtonGrid.gameObject.SetActive(true);

            // Clear old item buttons
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

            // Add back button inside grid only if no standalone cancel button is assigned
            if (cancelItemButton == null)
                CreateBackButton(itemButtonGrid);
        }

        if (itemPanel != null) itemPanel.SetActive(true);
        if (cancelItemButton != null) cancelItemButton.gameObject.SetActive(true);
        if (attackPanel != null) attackPanel.SetActive(false);
        if (cancelAttackButton != null) cancelAttackButton.gameObject.SetActive(false);
        if (attackButtonGrid != null) attackButtonGrid.gameObject.SetActive(false);
    }

    private void OnDefendSelected()
    {
        if (currentCharacter == null) return;

        // Check if character has max AP
        if (currentCharacter.currentAP < currentCharacter.MaxAP)
        {
            SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
            statusText.text = "Só é possível defender com PA máximo!";
            StartCoroutine(ClearStatusMessageAfterDelay(1.5f));
            return;
        }
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);

        // Hide action menu and both grids
        actionMenuPanel.SetActive(false);
        if (attackButtonGrid != null)
            attackButtonGrid.gameObject.SetActive(false);
        if (itemButtonGrid != null)
            itemButtonGrid.gameObject.SetActive(false);

        // Defend action: triple defense for this turn, use all AP
        StartCoroutine(ExecuteDefend());
    }

    private void OnWaitSelected()
    {
        if (currentCharacter == null) return;
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);

        // Hide action menu
        actionMenuPanel.SetActive(false);

        // End turn and execute any queued actions
        statusText.text = "Esperando...";
        combatSystem.EndTurnAndExecuteActions();
    }

    private void OnUndoSelected()
    {
        if (currentCharacter == null || combatSystem == null) return;
        SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);

        // Tell combat system to undo the last action
        combatSystem.UndoLastAction();

        // Update UI
        OnCharacterUpdated(currentCharacter);

        // Update button states
        UpdateActionMenuButtons();

        // Show action menu again
        actionMenuPanel.SetActive(true);

        // Hide grids
        if (attackButtonGrid != null)
            attackButtonGrid.gameObject.SetActive(false);
        if (itemButtonGrid != null)
            itemButtonGrid.gameObject.SetActive(false);

        statusText.text = $"AP: {currentCharacter.currentAP}/{currentCharacter.MaxAP} - Ação desfeita";
    }

    private IEnumerator ClearStatusMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentCharacter != null && statusText != null)
            statusText.text = $"AP: {currentCharacter.currentAP}/{currentCharacter.MaxAP} - Escolha outra ação ou Espere";
    }

    private IEnumerator ExecuteDefend()
    {
        // Hide action menu
        actionMenuPanel.SetActive(false);

        statusText.text = $"{currentCharacter.CharacterName} defends!";

        // Apply defend bonus through combat system
        combatSystem.ApplyDefendBonus(currentCharacter);

        // Use all AP
        currentCharacter.currentAP = 0;
        OnCharacterUpdated(currentCharacter);

        yield return new WaitForSeconds(1f);

        // End turn (defense bonus will be removed at start of next turn)
        combatSystem.EndTurnAndExecuteActions();
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

    private void OnCancelGridSelected()
    {
        SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
        ClearAllButtons();
        ClearAttackInfo();
        actionMenuPanel.SetActive(true);
    }

    private void OnAttackSelected(AttackFile attack)
    {
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
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
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
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

        // Track which panel was open so we can restore it on cancel
        wasAttackPanelOpen = !isItem;

        // Hide whichever selection panel was open
        if (attackPanel != null) attackPanel.SetActive(false);
        if (itemPanel != null) itemPanel.SetActive(false);
        if (cancelAttackButton != null) cancelAttackButton.gameObject.SetActive(false);
        if (cancelItemButton != null) cancelItemButton.gameObject.SetActive(false);

        if (statusText != null) statusText.text = $"Selecione {remainingTargetsToSelect} alvo(s)";

        // Show targeting back button
        if (targetingBackButton != null)
        {
            targetingBackButton.SetActive(true);
        }

        if (targetingPanel != null)
            targetingPanel.SetActive(true);

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

    // Add this public method to CombatUIManager.cs that can be called from a UI button:

    public void CancelTargeting()
    {
        SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
        Debug.Log("Targeting cancelled via UI button");

        // Cancel targeting
        DisableAllTargeting();

        // Hide targeting UI
        if (targetingBackButton != null)
            targetingBackButton.SetActive(false);
        if (targetingPanel != null)
            targetingPanel.SetActive(false);

        // Repopulate e restaurar o painel anterior sem reproduzir o som de avanço
        if (wasAttackPanelOpen)
            ShowAttackGrid();
        else
            ShowItemGrid();

        // Clear selection
        selectedAttack = null;
        selectedItem = null;
        selectedTargets.Clear();
        isTargeting = false;

        if (statusText != null) statusText.text = "Seleção cancelada";
    }
    private void EnableTargetingOnCharacters(List<PartyMemberState> characters)
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

        // Hide targeting back button
        if (targetingBackButton != null)
            targetingBackButton.SetActive(false);
        if (targetingPanel != null)
            targetingPanel.SetActive(false);
    }

    private void OnTargetSelected(PartyMemberState target)
    {
        if (!isTargeting) return;

        if (!selectedTargets.Contains(target))
        {
            SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
            selectedTargets.Add(target);
            remainingTargetsToSelect--;

            if (statusText != null) statusText.text = $"Selecionado {selectedTargets.Count}. Faltam {remainingTargetsToSelect}";

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

        // Update UI
        if (currentCharacter != null)
        {
            OnCharacterUpdated(currentCharacter);
        }

        // Return to appropriate menu based on AP and current state
        if (currentCharacter != null && currentCharacter.currentAP > 0 &&
            combatSystem.GetCurrentCharacter() == currentCharacter &&
            combatSystem.currentState == CombatState.PLAYER_TURN)
        {
            // Still have AP - go back to action menu
            actionMenuPanel.SetActive(true);

            // Update status text
            if (statusText != null) statusText.text = $"AP: {currentCharacter.currentAP}/{currentCharacter.MaxAP} - Selecione outra ação ou Espere";

            // Update button states (this will show undo button if there are pending actions)
            UpdateActionMenuButtons();

            // Hide the grids
            if (attackButtonGrid != null)
                attackButtonGrid.gameObject.SetActive(false);
            if (itemButtonGrid != null)
                itemButtonGrid.gameObject.SetActive(false);
        }
        else if (currentCharacter != null && currentCharacter.currentAP <= 0)
        {
            // No AP left, automatically end turn and execute queued actions
            waitPanel.SetActive(true);
            if (statusText != null) statusText.text = "Sem PA restante...";

            // Hide grids
            if (attackButtonGrid != null)
                attackButtonGrid.gameObject.SetActive(false);
            if (itemButtonGrid != null)
                itemButtonGrid.gameObject.SetActive(false);

            combatSystem.EndTurnAndExecuteActions();
        }
    }

    private void UseItemOnTargets(DadosItem item, List<PartyMemberState> targets)
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
                            target.currentAP = Mathf.Min(target.MaxAP, target.currentAP + effect.valor);
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

    private static readonly Color UsableColor   = new Color(1f, 1f, 1f, 1f);       // #FFFFFF
    private static readonly Color UnusableColor = new Color(0.8f, 0.8f, 0.8f, 1f); // #CCCCCC

    private void SetButtonVisualState(Button btn, bool usable)
    {
        if (btn == null) return;
        btn.interactable = usable;
        Color c = usable ? UsableColor : UnusableColor;

        // Color children named "Outline" and "Text (TMP)" only
        foreach (Transform child in btn.transform)
        {
            if (child.name == "Outline")
            {
                Image img = child.GetComponent<Image>();
                if (img != null) img.color = c;
            }
            else if (child.name == "Text (TMP)")
            {
                TMP_Text tmp = child.GetComponent<TMP_Text>();
                if (tmp != null) tmp.color = c;
            }
        }
    }

    private void UpdateActionMenuButtons()
    {
        if (currentCharacter == null) return;

        // Defend button only available at max AP
        SetButtonVisualState(defendMenuButton, currentCharacter.currentAP == currentCharacter.MaxAP);

        // Attacks button available if any attack costs <= current AP
        if (attacksMenuButton != null)
        {
            bool hasAffordableAttack = false;
            foreach (var attack in currentCharacter.learnedAttacks)
            {
                if (attack != null && attack.actionPointCost <= currentCharacter.currentAP)
                {
                    hasAffordableAttack = true;
                    break;
                }
            }
            SetButtonVisualState(attacksMenuButton, hasAffordableAttack);
        }

        // Items and Wait are always usable
        SetButtonVisualState(itemsMenuButton, true);
        SetButtonVisualState(waitMenuButton, true);

        // Undo button only appears when there are pending actions
        if (undoButton != null)
        {
            bool hasPendingActions = combatSystem.HasPendingActions();
            undoButton.gameObject.SetActive(hasPendingActions);
        }
    }

    private void OnCharacterUpdated(PartyMemberState character)
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

        // Update action menu buttons if this is the current character and action menu is active
        if (character == currentCharacter && actionMenuPanel != null && actionMenuPanel.activeSelf)
        {
            UpdateActionMenuButtons();
        }

        // Keep shared AP bar in sync whenever the active player character changes
        if (character == currentCharacter && combatSystem.partyMembers.Contains(character))
            RefreshSharedApBar(character);
    }

    private void OnCombatEnded(CombatState result)
    {
        waitPanel.SetActive(false);
        DisableAllTargeting();
        HideAllActionPanels();
        ClearAllButtons();

        if (result == CombatState.VICTORY)
        {
            SFXManager.Instance?.Play(SFXManager.Instance.successAcquired);
            if (statusText != null) statusText.text = "Vitória!";
        }
        else if (result == CombatState.DEFEAT)
        {
            SFXManager.Instance?.Play(SFXManager.Instance.defeatFail);
            if (statusText != null) statusText.text = "Derrota...";
        }
    }

    private void OnDestroy()
    {
        if (combatSystem != null)
        {
            combatSystem.onTurnStarted -= OnTurnStarted;
            combatSystem.onCharacterUpdated -= OnCharacterUpdated;
            combatSystem.onCombatEnded -= OnCombatEnded;
            combatSystem.onAttackStarted -= OnAttackStarted;
            combatSystem.onAttackFinished -= OnAttackFinished;
            combatSystem.onDamageTaken -= OnDamageTaken;
        }

        if (attacksMenuButton != null)
            attacksMenuButton.onClick.RemoveListener(OnAttacksSelected);
        if (itemsMenuButton != null)
            itemsMenuButton.onClick.RemoveListener(OnItemsSelected);
        if (defendMenuButton != null)
            defendMenuButton.onClick.RemoveListener(OnDefendSelected);
        if (waitMenuButton != null)
            waitMenuButton.onClick.RemoveListener(OnWaitSelected);
        if (undoButton != null)
            undoButton.onClick.RemoveListener(OnUndoSelected);
        if (cancelAttackButton != null)
            cancelAttackButton.onClick.RemoveListener(OnCancelGridSelected);
        if (cancelItemButton != null)
            cancelItemButton.onClick.RemoveListener(OnCancelGridSelected);
    }
}