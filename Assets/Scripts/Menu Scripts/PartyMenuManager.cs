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
    public GameObject attacksPanel;
    public GameObject itemsPanel;
    public GameObject equipmentPanel;
    public GameObject MenuOpener;

    [Header("Canvas References")]
    public Transform partyMenuCanvas;

    [Header("Party Member Selection")]
    public Transform partyMemberButtonParent;
    public GameObject partyMemberButtonPrefab;
    private List<PartyMemberButton> partyMemberButtons = new List<PartyMemberButton>();

    [Header("Stats Display Area")]
    public Transform statsDisplayContainer;
    public GameObject statsDisplayPrefab;
    private Dictionary<PartyMemberState, PartyMemberStatsDisplay> statsDisplays =
        new Dictionary<PartyMemberState, PartyMemberStatsDisplay>();

    private PartyMemberState currentSelectedMember;

    [Header("Attacks Display")]
    public Transform attackListParent;
    public GameObject attackDisplayPrefab;

    [Header("Equipment Display")]
    public Transform equipmentSlotParent;
    public GameObject equipmentSlotPrefab;
    private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlots =
        new Dictionary<EquipmentSlot, EquipmentSlotUI>();

    [Header("Objectives")]
    public GameObject objectivesButton;

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


        HideAllPanels();
        MenuOpener.SetActive(true);
        CreatePartyMemberDisplays();
    }

    private void Update()
    {
        // Only allow menu opening if not in battle and menu can be opened
        if (!isInBattle && canOpenMenu)
        {
            // Open with Tab key
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (partyMenuPanel.activeSelf)
                    CloseMenu();
                else
                    OpenMenu();
            }

            // Close with Escape
            if (Input.GetKeyDown(KeyCode.Escape) && partyMenuPanel.activeSelf)
            {
                CloseMenu();
            }
        }
    }

    // Called by combat system when battle starts
    public void SetBattleState(bool inBattle)
    {
        isInBattle = inBattle;
        if (inBattle && partyMenuPanel.activeSelf)
        {
            CloseMenu();
        }
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
        MenuOpener.SetActive(false);
        if (attacksPanel != null) attacksPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
    }

    private CanvasGroup GetObjectiveCanvasGroup()
    {
        if (ObjectiveManager.Instance == null) return null;
        return ObjectiveManager.Instance.GetComponent<CanvasGroup>();
    }

    public void ToggleObjectives()
    {
        CanvasGroup cg = GetObjectiveCanvasGroup();
        if (cg == null) return;
        bool willShow = cg.alpha <= 0.5f;
        cg.alpha = willShow ? 1f : 0f;
        cg.interactable = willShow;
        cg.blocksRaycasts = willShow;
        if (willShow && ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.RefreshQuestList();
    }

    private void CloseObjectivesPanel()
    {
        CanvasGroup cg = GetObjectiveCanvasGroup();
        if (cg != null) cg.alpha = 0f;
    }

    private void CreatePartyMemberDisplays()
    {
        foreach (var btn in partyMemberButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        partyMemberButtons.Clear();

        foreach (var display in statsDisplays.Values)
        {
            if (display != null) Destroy(display.gameObject);
        }
        statsDisplays.Clear();

        foreach (var member in inventory.partyMembers)
        {
            if (member != null)
            {
                GameObject btnObj = Instantiate(partyMemberButtonPrefab, partyMemberButtonParent);
                PartyMemberButton btn = btnObj.GetComponent<PartyMemberButton>();
                btn.Initialize(member, this);
                partyMemberButtons.Add(btn);

                GameObject displayObj = Instantiate(statsDisplayPrefab, statsDisplayContainer);
                displayObj.transform.localScale = Vector3.one;
                PartyMemberStatsDisplay display = displayObj.GetComponent<PartyMemberStatsDisplay>();
                display.Initialize(member);
                statsDisplays[member] = display;

                displayObj.SetActive(false);
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

        foreach (var kvp in statsDisplays)
        {
            kvp.Value.gameObject.SetActive(kvp.Key == member);
        }

        UpdatePartyMemberHighlights();

        if (attacksPanel.activeSelf)
        {
            UpdateAttacksDisplay();
        }

        if (equipmentPanel.activeSelf)
        {
            UpdateEquipmentDisplay();
        }
    }

    public void UpdatePartyMemberHighlights()
    {
        foreach (var btn in partyMemberButtons)
        {
            btn.SetHighlight(btn.GetMemberState() == currentSelectedMember);
        }
    }

    public void ShowAttacks()
    {
        HideSubPanels();
        attacksPanel.SetActive(true);

        if (currentSelectedMember != null)
        {
            UpdateAttacksDisplay();
        }
    }

    public void ShowItems()
    {
        HideSubPanels();
        itemsPanel.SetActive(true);
        RefreshInventoryDisplay();
    }

    public void ShowEquipment()
    {
        HideSubPanels();
        equipmentPanel.SetActive(true);

        if (currentSelectedMember != null)
        {
            UpdateEquipmentDisplay();
        }
    }

    // Hides only the content sub-panels, NOT partyMenuPanel or character buttons
    private void HideSubPanels()
    {
        if (attacksPanel != null) attacksPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
    }

    private void UpdateAttacksDisplay()
    {
        foreach (Transform child in attackListParent)
            Destroy(child.gameObject);

        if (currentSelectedMember == null) return;

        foreach (var attack in currentSelectedMember.learnedAttacks)
        {
            GameObject attackObj = Instantiate(attackDisplayPrefab, attackListParent);
            AttackDisplay display = attackObj.GetComponent<AttackDisplay>();
            if (display != null)
                display.Initialize(attack);
        }
    }

    public void UpdateEquipmentDisplay()
    {
        foreach (Transform child in equipmentSlotParent)
            Destroy(child.gameObject);
        equipmentSlots.Clear();

        if (currentSelectedMember == null) return;

        GameObject weaponSlot = Instantiate(equipmentSlotPrefab, equipmentSlotParent);
        EquipmentSlotUI weaponUI = weaponSlot.GetComponent<EquipmentSlotUI>();
        weaponUI.Initialize(EquipmentSlot.Arma, currentSelectedMember.weapon, this);
        equipmentSlots[EquipmentSlot.Arma] = weaponUI;

        GameObject armorSlot = Instantiate(equipmentSlotPrefab, equipmentSlotParent);
        EquipmentSlotUI armorUI = armorSlot.GetComponent<EquipmentSlotUI>();
        armorUI.Initialize(EquipmentSlot.Armadura, currentSelectedMember.armor, this);
        equipmentSlots[EquipmentSlot.Armadura] = armorUI;
    }

    public void RefreshInventoryDisplay()
    {
        if (inventory == null) return;

        if (goldText != null)
        {
            goldText.text = "Ouro: " + inventory.moedas.ToString();
        }

        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        foreach (SlotInventario slot in inventory.inventario)
        {
            GameObject newSlot = Instantiate(itemSlotPrefab, inventoryGrid);
            SlotUI slotUI = newSlot.GetComponent<SlotUI>();

            slotUI.partyMenuManager = this;
            slotUI.itemDetailsPrefab = itemDetailsPrefab;
            slotUI.partyMemberSelectorPrefab = partyMemberSelectorPrefab;
            slotUI.partyMemberButtonPrefab = partyMemberButtonPrefab;

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

    public void UnequipItem(DadosItem item, EquipmentSlot slot)
    {
        if (currentSelectedMember == null) return;

        if (slot == EquipmentSlot.Arma)
        {
            currentSelectedMember.UnequipWeapon();
        }
        else if (slot == EquipmentSlot.Armadura)
        {
            currentSelectedMember.UnequipArmor();
        }

        if (inventory != null)
        {
            inventory.AdicionarItem(item, 1);
            RefreshInventoryDisplay();
        }

        UpdateEquipmentDisplay();
        UpdateCharacterStats(currentSelectedMember);
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

    public void OpenMenu()
    {
        if (isInBattle || !canOpenMenu) return; // Don't open during battle or cutscenes

        CloseObjectivesPanel();
        HideAllPanels();
        if (objectivesButton != null) objectivesButton.SetActive(false);
        partyMenuPanel.SetActive(true);

        CanvasGroup canvasGroup = partyMenuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        CreatePartyMemberDisplays();
        RefreshInventoryDisplay();
    }

    public void CloseMenu()
    {
        HideAllPanels();
        if (MenuOpener != null) MenuOpener.SetActive(true);
        if (objectivesButton != null) objectivesButton.SetActive(true);
    }

    // Close Game with Save
    public void CloseGame()
    {
        Debug.Log("Closing game... Saving before exit");

        // Find SaveLoadManager instance
        SaveLoadManager saveManager = SaveLoadManager.Instance;

        if (saveManager != null)
        {
            // Save the game before closing
            saveManager.SaveGame();
            Debug.Log("Game saved successfully before closing");
        }
        else
        {
            Debug.LogWarning("SaveLoadManager not found! Game will close without saving.");
        }

        // Optionally add a small delay to ensure save completes
        StartCoroutine(QuitAfterDelay(0.2f));
    }

    public void SaveGame()
    {
        SaveLoadManager saveLoadManager = SaveLoadManager.Instance;
        saveLoadManager.SaveGame();
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Application.Quit();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onInventarioMudou -= RefreshInventoryDisplay;
        }
    }
}