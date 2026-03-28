using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PartyMenuManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject partyMenuPanel;
    public GameObject menuContainer;   // Shared background container for all subpanels
    public GameObject characterPanel;  // Default group/character view
    public GameObject craftingPanel;
    public GameObject itemsPanel;
    public GameObject MenuOpener;

    [Header("Menu Nav Buttons")]
    public TextMeshProUGUI characterNavText;
    public Image characterNavBg;
    public TextMeshProUGUI craftingNavText;
    public Image craftingNavBg;
    public TextMeshProUGUI itemsNavText;
    public Image itemsNavBg;
    public TextMeshProUGUI equipmentNavText;
    public Image equipmentNavBg;

    private static readonly Color NavTextSelected = Color.gray;
    private static readonly Color NavTextDeselected = Color.white;
    private static readonly Color NavBgSelected = new Color(0.12f, 0.12f, 0.12f, 0.8f); // ~#1F1F1F — active tab dimmed
    private static readonly Color NavBgDeselected = new Color(0.19f, 0.19f, 0.19f, 0.8f); // ~#303030 — available tabs bright
    private TextMeshProUGUI currentNavText;
    private Image currentNavBg;

    [Header("Canvas References")]
    public Transform partyMenuCanvas;

    private List<PartyMemberButton> partyMemberButtons = new List<PartyMemberButton>();

    [Header("Stats Display Area")]
    public Transform statsDisplayContainer;
    public GameObject statsDisplayPrefab;
    private Dictionary<PartyMemberState, PartyMemberStatsDisplay> statsDisplays =
        new Dictionary<PartyMemberState, PartyMemberStatsDisplay>();

    private PartyMemberState currentSelectedMember;

    [Header("Crafting Display")]
    public Transform craftingListParent;
    public GameObject craftingSlotPrefab;

    [Header("Equipment Display")]
    public Transform equipmentSlotParent;
    public GameObject equipmentSlotPrefab;        // kept for backwards-compat; unused in new flow
    public GameObject equipmentCardPrefab;         // new per-character card prefab
    private List<EquipmentCharacterCard> _equipmentCards = new List<EquipmentCharacterCard>();

    [Header("Item Targeting")]
    public TextMeshProUGUI selectPromptText;     // "Selecione um personagem:" — inside characterPanel
    public TextMeshProUGUI inventoryTitleText;   // Normal title — "Inventário"
    public TextMeshProUGUI selectItemPromptText; // Equip-mode title — "Selecione um item:"
    public Button backButton;                    // Undo button — enabled only during active flows

    // ── Pending operation state ──────────────────────────────────────────────
    public enum PendingOpType { None, UseItem, EquipItem, EquipItemFromInventory }
    private PendingOpType    _pendingOp          = PendingOpType.None;
    private SlotInventario   _pendingItemSlot;
    private PartyMemberState _pendingEquipMember;
    private EquipmentSlot    _pendingEquipSlot;

    public PendingOpType GetPendingOp()        => _pendingOp;
    public EquipmentSlot GetPendingEquipSlot() => _pendingEquipSlot;

    [Header("HUD Buttons")]
    public GameObject hudButtonsContainer; // Root that holds Group/Objectives/Settings buttons
    public GameObject objectivesButton;
    public CanvasGroup objectivesButtonCanvasGroup;
    public GameObject settingsButton;
    public CanvasGroup settingsButtonCanvasGroup;
    public CanvasGroup menuOpenerCanvasGroup;

    [Header("Gold Count")]
    public TextMeshProUGUI goldCount; // Always visible, root of Party Menu
    public GameObject goldContainer; // Parent GameObject of goldCount — toggled off during battle

    [Header("Inventory Display")]
    public Transform inventoryGrid;
    public GameObject itemSlotPrefab;
    public TextMeshProUGUI goldText;
    private List<SlotUI> inventorySlots = new List<SlotUI>();

    [Header("Item Details")]
    public GameObject itemDetailsPrefab;
    public GameObject partyMemberSelectorPrefab;

    [Header("References")]
    public SistemaInventario inventory;
    public ConfigSceneManager configSceneManager; // Reference to ConfigSceneManager
    public GameObject configPanel;        // Settings panel
    public GameObject leaveConfirmPanel;  // "Tem certeza?" dialog inside configPanel

    [Header("Menu State")]
    public bool canOpenMenu = true; // Can be set to false during cutscenes

    private SlotUI currentlyHighlightedSlot;
    private bool isInBattle = false;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<SistemaInventario>();

        if (partyMenuCanvas == null)
            partyMenuCanvas = transform;

        if (inventory != null)
        {
            inventory.onInventarioMudou += RefreshInventoryDisplay;
            inventory.onPartyUpdated += CreatePartyMemberDisplays;
        }

        // Find ConfigSceneManager if not assigned
        if (configSceneManager == null)
            configSceneManager = FindFirstObjectByType<ConfigSceneManager>();

        // Wire nav button clicks
        if (characterNavBg != null) characterNavBg.GetComponent<Button>()?.onClick.AddListener(ShowCharacter);
        if (craftingNavBg != null) craftingNavBg.GetComponent<Button>()?.onClick.AddListener(ShowCrafting);
        if (itemsNavBg != null) itemsNavBg.GetComponent<Button>()?.onClick.AddListener(ShowItems);
        if (equipmentNavBg != null) equipmentNavBg.GetComponent<Button>()?.onClick.AddListener(ShowEquipment);

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
            backButton.gameObject.SetActive(false);
        }

        HideAllPanels();
        CreatePartyMemberDisplays();
        RefreshInventoryDisplay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Shopkeeper.IsAnyShopOpen()) return;

            if (isInBattle)
            {
                FindFirstObjectByType<CombatSystem>()?.TogglePauseMenu();
                return;
            }

            // Fechar qualquer menu aberto primeiro; só abre configurações se nada estava aberto
            if (partyMenuPanel.activeSelf)
            {
                CloseMenu();
                return;
            }

            CanvasGroup objCg = GetObjectiveCanvasGroup();
            if (objCg != null && objCg.alpha > 0.5f)
            {
                SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
                ToggleObjectives();
                return;
            }

            if (configPanel != null && configPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            // Nada aberto — abre configurações
            ToggleSettings();
            return;
        }

        // Only allow menu opening if not in battle and menu can be opened
        if (!isInBattle && canOpenMenu)
        {
            // Toggle with Tab key
            if (Input.GetKeyDown(KeyCode.Tab))
                ToggleMenu();

            // Close with Tab
            if (Input.GetKeyDown(KeyCode.Tab) && partyMenuPanel.activeSelf)
            {
                CloseMenu();
            }

            // Toggle Inventory with I key
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (!partyMenuPanel.activeSelf)
                    OpenMenu();
                ShowItems();
            }

            // Toggle Quests with O key
            if (Input.GetKeyDown(KeyCode.O))
            {
                ToggleObjectives();
            }

            // Toggle Crafting with C key
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleCrafting();
            }
        }
    }

    // Called by combat system when battle starts/ends
    public void SetBattleState(bool inBattle)
    {
        isInBattle = inBattle;
        if (inBattle && partyMenuPanel.activeSelf)
            CloseMenu();
        if (hudButtonsContainer != null)
            hudButtonsContainer.SetActive(!inBattle);
        if (goldContainer != null)
            goldContainer.SetActive(!inBattle);

        // Disable the GraphicRaycaster on this canvas during combat so it
        // doesn't intercept clicks meant for combat targeting
        GraphicRaycaster gr = GetComponentInParent<GraphicRaycaster>();
        if (gr == null) gr = GetComponent<GraphicRaycaster>();
        if (gr != null) gr.enabled = !inBattle;
    }

    // Called by cutscene system
    public void SetCanOpenMenu(bool canOpen)
    {
        canOpenMenu = canOpen;
        if (!canOpen && partyMenuPanel.activeSelf)
        {
            CloseMenu();
        }
    }

    private void HideAllPanels()
    {
        if (partyMenuPanel != null) partyMenuPanel.SetActive(false);
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(false);
    }

    private CanvasGroup GetObjectiveCanvasGroup()
    {
        if (ObjectiveManager.Instance == null) return null;
        return ObjectiveManager.Instance.GetComponent<CanvasGroup>();
    }

    public void ToggleObjectives()
    {
        if (isInBattle || !canOpenMenu)
        {
            Debug.Log($"OpenMenu bloqueado — isInBattle={isInBattle}, canOpenMenu={canOpenMenu}");
            return;
        }
        CanvasGroup cg = GetObjectiveCanvasGroup();
        if (cg == null) return;
        bool willShow = cg.alpha <= 0.5f;

        if (willShow)
        {
            CloseMenu();
            CloseSettings();
            SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
        }
        else
        {
            SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
        }

        cg.alpha = willShow ? 1f : 0f;
        cg.interactable = willShow;
        cg.blocksRaycasts = willShow;

        SetCanvasGroupVisible(objectivesButtonCanvasGroup, !willShow);

        if (willShow && ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.RefreshQuestList();
    }

    private void CloseObjectivesPanel()
    {
        CanvasGroup cg = GetObjectiveCanvasGroup();
        if (cg != null) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
        SetCanvasGroupVisible(objectivesButtonCanvasGroup, true);
    }

    private void CloseSettings()
    {
        if (configPanel != null && configPanel.activeSelf)
        {
            SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
            configPanel.SetActive(false);
            SetCanvasGroupVisible(settingsButtonCanvasGroup, true);
        }
    }

    public void ToggleSettings()
    {
        if (configPanel == null)
        {
            Debug.LogError("Config panel not assigned in PartyMenuManager!");
            return;
        }

        bool isOpen = configPanel.activeSelf;

        if (isOpen)
        {
            CloseSettings();
        }
        else
        {
            CloseMenu();
            CloseObjectivesPanel();
            SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
            configPanel.SetActive(true);
            SetCanvasGroupVisible(settingsButtonCanvasGroup, false);
        }
    }

    public void ToggleCrafting()
    {
        if (craftingPanel == null)
        {
            Debug.LogError("Crafting panel not assigned in PartyMenuManager!");
            return;
        }

        bool isOpen = craftingPanel.activeSelf;

        if (isOpen)
        {
            // If crafting panel is open, close it
            craftingPanel.SetActive(false);
            // If party menu is still open, show the last active panel or character panel
            if (partyMenuPanel.activeSelf)
            {
                ShowCharacter();
            }
        }
        else
        {
            // If crafting panel is closed, open it
            // If party menu is closed, open it first
            if (!partyMenuPanel.activeSelf)
            {
                OpenMenu();
            }
            ShowCrafting();
        }
    }

    private void CreatePartyMemberDisplays()
    {
        foreach (var btn in partyMemberButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        partyMemberButtons.Clear();

        // Clear both display types — they share statsDisplayContainer
        foreach (var display in statsDisplays.Values)
            if (display != null) Destroy(display.gameObject);
        statsDisplays.Clear();

        foreach (var card in _equipmentCards)
            if (card != null) Destroy(card.gameObject);
        _equipmentCards.Clear();

        foreach (var member in inventory.partyMembers)
        {
            if (member != null)
            {
                if (statsDisplayPrefab != null && statsDisplayContainer != null)
                {
                    GameObject displayObj = Instantiate(statsDisplayPrefab, statsDisplayContainer);
                    displayObj.transform.localScale = Vector3.one;
                    PartyMemberStatsDisplay display = displayObj.GetComponent<PartyMemberStatsDisplay>();
                    display.Initialize(member);
                    statsDisplays[member] = display;
                    displayObj.SetActive(true);
                }
            }
        }

        if (inventory.partyMembers.Count > 0 && inventory.partyMembers[0] != null)
        {
            OnPartyMemberSelected(inventory.partyMembers[0]);
        }
    }

    public void OnPartyMemberSelected(PartyMemberState member)
    {
        currentSelectedMember = member;

    }

    private void SetInventoryTitle(bool normal)
    {
        if (inventoryTitleText   != null) inventoryTitleText.gameObject.SetActive(normal);
        if (selectItemPromptText != null && normal) selectItemPromptText.gameObject.SetActive(false);
    }

    private static void SetNavButtonState(TextMeshProUGUI text, Image bg, bool selected)
    {
        if (text != null) text.color = selected ? NavTextSelected : NavTextDeselected;
        if (bg != null) bg.color = selected ? NavBgSelected : NavBgDeselected;
    }

    private static void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private void SelectNav(TextMeshProUGUI navText, Image navBg)
    {
        if (currentNavText != null) currentNavText.color = NavTextDeselected;
        if (currentNavBg != null) currentNavBg.color = NavBgDeselected;
        currentNavText = navText;
        currentNavBg = navBg;
        if (currentNavText != null) currentNavText.color = NavTextSelected;
        if (currentNavBg != null) currentNavBg.color = NavBgSelected;
    }

    public void ShowCharacter()
    {
        if (currentNavText == characterNavText && _pendingOp == PendingOpType.None) return;
        CancelPendingOperation();
        HideSubPanels();
        if (characterPanel != null) characterPanel.SetActive(true);
        SelectNav(characterNavText, characterNavBg);
        // Repopulate stat displays if equipment cards replaced them
        if (_equipmentCards.Count > 0 || statsDisplays.Count == 0)
            RepopulatePartyDisplays();
    }

    // Rebuilds statsDisplayContainer with stat display prefabs (party view).
    // Called when switching back from equipment view.
    private void RepopulatePartyDisplays()
    {
        if (statsDisplayContainer != null)
            foreach (Transform child in statsDisplayContainer)
                Destroy(child.gameObject);

        statsDisplays.Clear();
        _equipmentCards.Clear();

        foreach (var member in inventory.partyMembers)
        {
            if (member == null || statsDisplayPrefab == null || statsDisplayContainer == null) continue;
            GameObject displayObj = Instantiate(statsDisplayPrefab, statsDisplayContainer);
            displayObj.transform.localScale = Vector3.one;
            PartyMemberStatsDisplay display = displayObj.GetComponent<PartyMemberStatsDisplay>();
            display.Initialize(member);
            statsDisplays[member] = display;
            displayObj.SetActive(true);
        }

        if (inventory.partyMembers.Count > 0 && inventory.partyMembers[0] != null)
            OnPartyMemberSelected(inventory.partyMembers[0]);
    }

    public void ShowCrafting()
    {
        if (currentNavText == craftingNavText && _pendingOp == PendingOpType.None) return;
        CancelPendingOperation();
        HideSubPanels();
        craftingPanel.SetActive(true);
        SelectNav(craftingNavText, craftingNavBg);
    }

    public void ShowItems()
    {
        if (currentNavText == itemsNavText && _pendingOp == PendingOpType.None) return;
        CancelPendingOperation();
        HideSubPanels();
        itemsPanel.SetActive(true);
        SelectNav(itemsNavText, itemsNavBg);
        SetInventoryTitle(normal: true);
        RefreshInventoryDisplay();
    }

    public void ShowEquipment()
    {
        if (currentNavText == equipmentNavText && _pendingOp == PendingOpType.None) return;
        CancelPendingOperation();
        HideSubPanels();
        if (characterPanel != null) characterPanel.SetActive(true); // reusa o mesmo painel do grupo
        SelectNav(equipmentNavText, equipmentNavBg);
        UpdateEquipmentDisplay();
    }

    // ── Pending-operation API ─────────────────────────────────────────────────

    // Called by SlotUI when a consumable is clicked
    public void StartUseItemTargeting(SlotInventario slot)
    {
        _pendingOp       = PendingOpType.UseItem;
        _pendingItemSlot = slot;

        // Switch to character panel without going through HideSubPanels
        if (itemsPanel   != null) itemsPanel.SetActive(false);
        if (characterPanel != null) characterPanel.SetActive(true);
        SelectNav(characterNavText, characterNavBg);

        if (selectPromptText != null)
        {
            selectPromptText.text = "Selecione um personagem:";
            selectPromptText.gameObject.SetActive(true);
        }

        foreach (var display in statsDisplays.Values)
            display.SetTargetable(true, OnUseItemOnCharacter);

        if (backButton != null) backButton.gameObject.SetActive(true);
    }

    // Callback from PartyMemberStatsDisplay when clicked during item targeting
    private void OnUseItemOnCharacter(PartyMemberState member)
    {
        if (_pendingItemSlot?.dadosDoItem == null) return;

        bool used = member.UseConsumable(_pendingItemSlot.dadosDoItem);
        if (used)
        {
            SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
            inventory.RemoverItem(_pendingItemSlot.dadosDoItem, 1);
            UpdateCharacterStats(member);
        }

        CancelPendingOperation();
        ShowItems();
    }

    // Called by EquipmentCharacterCard when a slot button is clicked
    public void StartEquipFromSlot(PartyMemberState member, EquipmentSlot slot)
    {
        _pendingOp          = PendingOpType.EquipItem;
        _pendingEquipMember = member;
        _pendingEquipSlot   = slot;

        // Show items panel without changing the active nav (we're still in the equip flow)
        if (characterPanel != null) characterPanel.SetActive(false);
        if (itemsPanel     != null) itemsPanel.SetActive(true);

        SetInventoryTitle(normal: false);
        if (selectItemPromptText != null)
        {
            string slotLabel = slot == EquipmentSlot.Acessorio ? "acessório" : "armadura";
            selectItemPromptText.text = $"Selecione um(a) {slotLabel} para {member.CharacterName}:";
            selectItemPromptText.gameObject.SetActive(true);
        }

        if (backButton != null) backButton.gameObject.SetActive(true);

        RefreshInventoryDisplay();
    }

    // Called by SlotUI when an equippable item is clicked while in equip-filter mode (Equipment tab flow)
    public void OnEquipItemSelectedFromPanel(SlotInventario slot)
    {
        if (_pendingEquipMember == null || slot == null) return;

        inventory.EquipItemToMember(slot, _pendingEquipMember);
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);

        CancelPendingOperation();
        ReturnToEquipmentView();
    }

    // Called by SlotUI when clicking an equippable from the Items tab (item→character flow)
    public void StartEquipItemFromInventory(SlotInventario slot)
    {
        _pendingOp       = PendingOpType.EquipItemFromInventory;
        _pendingItemSlot = slot;

        // Show character panel with stat displays (same as UseItem flow) — just pick a character
        if (itemsPanel     != null) itemsPanel.SetActive(false);
        if (characterPanel != null) characterPanel.SetActive(true);

        // Ensure stat displays are populated (not equipment cards)
        if (_equipmentCards.Count > 0 || statsDisplays.Count == 0)
            RepopulatePartyDisplays();

        foreach (var display in statsDisplays.Values)
            display.SetTargetable(true, OnEquipItemToMemberFromInventory);

        if (selectPromptText != null)
        {
            selectPromptText.text = $"Equipar {slot.dadosDoItem.nomeDoItem} em:";
            selectPromptText.gameObject.SetActive(true);
        }

        if (backButton != null) backButton.gameObject.SetActive(true);
    }

    private void OnEquipItemToMemberFromInventory(PartyMemberState member)
    {
        if (_pendingItemSlot == null) return;

        inventory.EquipItemToMember(_pendingItemSlot, member);
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);

        CancelPendingOperation();
        ReturnToItemsView();
    }

    // Resets all pending state and disables targeting visuals
    public void CancelPendingOperation()
    {
        _pendingOp          = PendingOpType.None;
        _pendingItemSlot    = null;
        _pendingEquipMember = null;

        foreach (var display in statsDisplays.Values)
            display.SetTargetable(false, null);

        foreach (var card in _equipmentCards)
            if (card != null) card.SetTargetable(false, null);

        if (selectPromptText     != null) selectPromptText.gameObject.SetActive(false);
        if (selectItemPromptText != null) selectItemPromptText.gameObject.SetActive(false);
        if (backButton           != null) backButton.gameObject.SetActive(false);
        SetInventoryTitle(normal: true);
    }

    // Returns to the equipment card view after an equip/unequip action, bypassing ShowEquipment's guard
    private void ReturnToEquipmentView()
    {
        if (itemsPanel     != null) itemsPanel.SetActive(false);
        if (characterPanel != null) characterPanel.SetActive(true);
        SelectNav(equipmentNavText, equipmentNavBg);
        UpdateEquipmentDisplay();
    }

    // Returns to items panel, bypassing the ShowItems guard
    private void ReturnToItemsView()
    {
        HideSubPanels();
        if (itemsPanel != null) itemsPanel.SetActive(true);
        SelectNav(itemsNavText, itemsNavBg);
        SetInventoryTitle(normal: true);
        RefreshInventoryDisplay();
    }

    // Refreshes equipment cards in place without destroying stat displays
    public void RefreshEquipmentCards()
    {
        foreach (var card in _equipmentCards)
            if (card != null) card.Refresh();
    }

    private void OnBackClicked()
    {
        if (_pendingOp == PendingOpType.UseItem)
        {
            CancelPendingOperation();
            ShowItems();
        }
        else if (_pendingOp == PendingOpType.EquipItem)
        {
            CancelPendingOperation();
            ReturnToEquipmentView();
        }
        else if (_pendingOp == PendingOpType.EquipItemFromInventory)
        {
            CancelPendingOperation();
            ReturnToItemsView();
        }
    }

    // ── Sub-panel navigation ──────────────────────────────────────────────────

    // Hides only the content sub-panels — does NOT cancel pending ops (callers do that)
    private void HideSubPanels()
    {
        if (characterPanel != null) characterPanel.SetActive(false);
        if (craftingPanel  != null) craftingPanel.SetActive(false);
        if (itemsPanel     != null) itemsPanel.SetActive(false);
    }


    public void UpdateEquipmentDisplay()
    {
        // Shares statsDisplayContainer with the character view — clear everything first
        if (statsDisplayContainer != null)
            foreach (Transform child in statsDisplayContainer)
                Destroy(child.gameObject);
        statsDisplays.Clear();
        _equipmentCards.Clear();

        if (equipmentCardPrefab == null || inventory == null) return;

        foreach (var member in inventory.partyMembers)
        {
            if (member == null) continue;
            GameObject cardObj = Instantiate(equipmentCardPrefab, statsDisplayContainer);
            EquipmentCharacterCard card = cardObj.GetComponent<EquipmentCharacterCard>();
            if (card != null)
            {
                card.Initialize(member, this);
                _equipmentCards.Add(card);
            }
        }
    }

    public void RefreshInventoryDisplay()
    {
        if (inventory == null) return;

        string goldStr = "Ouro: " + inventory.moedas.ToString();
        if (goldText != null) goldText.text = goldStr;
        if (goldCount != null) goldCount.text = goldStr;

        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        // In equip mode: add a "Remover Item" slot first
        if (_pendingOp == PendingOpType.EquipItem)
        {
            GameObject removeSlotObj = Instantiate(itemSlotPrefab, inventoryGrid);
            SlotUI removeSlotUI = removeSlotObj.GetComponent<SlotUI>();
            removeSlotUI.partyMenuManager = this;
            if (removeSlotUI.itemIcon           != null) removeSlotUI.itemIcon.gameObject.SetActive(false);
            if (removeSlotUI.equippedIndicator  != null) removeSlotUI.equippedIndicator.SetActive(true);
            if (removeSlotUI.unequipXObject     != null) removeSlotUI.unequipXObject.SetActive(true);
            if (removeSlotUI.checkmarkObject    != null) removeSlotUI.checkmarkObject.SetActive(false);
            if (removeSlotUI.clickButton        != null)
                removeSlotUI.clickButton.onClick.AddListener(OnUnequipFromPendingSlot);
        }

        foreach (SlotInventario slot in inventory.inventario)
        {
            // When picking equipment for a slot, show only items of the matching type
            if (_pendingOp == PendingOpType.EquipItem)
            {
                if (slot.dadosDoItem == null ||
                    !slot.dadosDoItem.ehEquipavel ||
                    slot.dadosDoItem.slotEquipamento != _pendingEquipSlot)
                    continue;
            }

            GameObject newSlot = Instantiate(itemSlotPrefab, inventoryGrid);
            SlotUI slotUI = newSlot.GetComponent<SlotUI>();

            slotUI.partyMenuManager             = this;
            slotUI.itemDetailsPrefab            = itemDetailsPrefab;
            slotUI.partyMemberSelectorPrefab    = partyMemberSelectorPrefab;

            slotUI.ConfigurarSlot(slot);
            inventorySlots.Add(slotUI);
        }

        currentlyHighlightedSlot = null;
    }

    public void HighlightSlot(SlotUI selectedSlot)
    {
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(false);
        }

        currentlyHighlightedSlot = selectedSlot;
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(true);
        }
    }

    // Called by "Remover Item" slot in equip filter mode
    private void OnUnequipFromPendingSlot()
    {
        if (_pendingEquipMember == null) return;

        SlotInventario slot = _pendingEquipSlot == EquipmentSlot.Acessorio
            ? inventory.FindSlotForItem(_pendingEquipMember.accessory)
            : inventory.FindSlotForItem(_pendingEquipMember.armor);

        if (slot != null)
        {
            inventory.UnequipItemFromSlot(slot);
            SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
        }

        CancelPendingOperation();
        ReturnToEquipmentView();
    }

    public void UnequipItem(DadosItem item, EquipmentSlot slot)
    {
        SlotInventario invSlot = inventory.FindSlotForItem(item);
        if (invSlot != null)
            inventory.UnequipItemFromSlot(invSlot);

        UpdateEquipmentDisplay();
    }

    public PartyMemberState GetCurrentSelectedMember()
    {
        return currentSelectedMember;
    }

    public void UpdateCharacterStats(PartyMemberState member)
    {
        if (statsDisplays.ContainsKey(member))
        {
            statsDisplays[member].UpdateDisplay();
        }
    }

    public void ToggleMenu()
    {
        bool isOpen = partyMenuPanel != null && partyMenuPanel.activeSelf;
        Debug.Log($"ToggleMenu chamado — painel estava {(isOpen ? "aberto" : "fechado")}");
        if (isOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (isInBattle || !canOpenMenu)
        {
            Debug.Log($"OpenMenu bloqueado — isInBattle={isInBattle}, canOpenMenu={canOpenMenu}");
            return;
        }
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);

        CloseObjectivesPanel();
        CloseSettings();
        HideAllPanels();
        SetCanvasGroupVisible(menuOpenerCanvasGroup, false);

        Debug.Log("OpenMenu: ativando partyMenuPanel");
        partyMenuPanel.SetActive(true);

        CanvasGroup canvasGroup = partyMenuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (menuContainer != null) menuContainer.SetActive(true);

        CreatePartyMemberDisplays();
        RefreshInventoryDisplay();

        // Reset all nav buttons to deselected state before defaulting
        SetCanvasGroupVisible(objectivesButtonCanvasGroup, true);
        SetNavButtonState(characterNavText, characterNavBg, false);
        SetNavButtonState(craftingNavText, craftingNavBg, false);
        SetNavButtonState(itemsNavText, itemsNavBg, false);
        SetNavButtonState(equipmentNavText, equipmentNavBg, false);

        currentNavText = null;
        ShowCharacter();
    }

    public void CloseMenu()
    {
        Debug.Log("CloseMenu chamado");
        SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
        if (menuContainer != null) menuContainer.SetActive(false);
        currentNavText = null;
        HideAllPanels();
        SetCanvasGroupVisible(menuOpenerCanvasGroup, true);
    }

    public void ShowLeaveConfirmation()
    {
        if (configPanel      != null) configPanel.SetActive(false);
        if (leaveConfirmPanel != null) leaveConfirmPanel.SetActive(true);
    }

    public void CancelLeave()
    {
        if (leaveConfirmPanel != null) leaveConfirmPanel.SetActive(false);
        if (configPanel       != null) configPanel.SetActive(true);
    }

    public void ConfirmLeave()
    {
        SaveLoadManager.Instance?.SaveGame();
        SceneManager.LoadScene("TitleScreen");
    }

    // Legacy — kept in case referenced elsewhere
    public void CloseGame() => ShowLeaveConfirmation();

    public void SaveGame()
    {
        SaveLoadManager.Instance?.SaveGame();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onInventarioMudou -= RefreshInventoryDisplay;
        }
    }
}