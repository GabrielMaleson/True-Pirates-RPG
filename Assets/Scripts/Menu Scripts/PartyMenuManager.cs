using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PartyMenuManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject partyMenuPanel;
    public GameObject attacksPanel;
    public GameObject itemsPanel;
    public GameObject equipmentPanel;

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

    private SlotUI currentlyHighlightedSlot;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<SistemaInventario>();

        // Subscribe to inventory change events
        if (inventory != null)
        {
            inventory.onInventarioMudou += RefreshInventoryDisplay;
        }

        // Initially hide all panels
        HideAllPanels();

        // Create party member buttons and stats displays
        CreatePartyMemberDisplays();
    }

    private void Update()
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

    private void HideAllPanels()
    {
        if (attacksPanel != null) attacksPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
    }

    private void CreatePartyMemberDisplays()
    {
        // Clear existing
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

        // Create displays for each party member
        foreach (var member in inventory.partyMembers)
        {
            if (member != null)
            {
                // Create selection button
                GameObject btnObj = Instantiate(partyMemberButtonPrefab, partyMemberButtonParent);
                PartyMemberButton btn = btnObj.GetComponent<PartyMemberButton>();
                btn.Initialize(member, this);
                partyMemberButtons.Add(btn);

                // Create stats display
                GameObject displayObj = Instantiate(statsDisplayPrefab, statsDisplayContainer);
                PartyMemberStatsDisplay display = displayObj.GetComponent<PartyMemberStatsDisplay>();
                display.Initialize(member);
                statsDisplays[member] = display;

                // Initially hide all displays
                displayObj.SetActive(false);
            }
        }

        // Select first party member by default
        if (inventory.partyMembers.Count > 0 && inventory.partyMembers[0] != null)
        {
            OnPartyMemberSelected(inventory.partyMembers[0]);
        }
    }

    public void OnPartyMemberSelected(PartyMemberState member)
    {
        currentSelectedMember = member;

        // Show only the selected character's stats display
        foreach (var kvp in statsDisplays)
        {
            kvp.Value.gameObject.SetActive(kvp.Key == member);
        }

        // Update party member highlights
        UpdatePartyMemberHighlights();

        // Refresh attacks if attacks panel is open
        if (attacksPanel.activeSelf)
        {
            UpdateAttacksDisplay();
        }

        // Refresh equipment if equipment panel is open
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
        HideAllPanels();
        attacksPanel.SetActive(true);

        if (currentSelectedMember != null)
        {
            UpdateAttacksDisplay();
        }
    }

    public void ShowItems()
    {
        HideAllPanels();
        itemsPanel.SetActive(true);

        // Refresh the inventory display
        RefreshInventoryDisplay();
    }

    public void ShowEquipment()
    {
        HideAllPanels();
        equipmentPanel.SetActive(true);

        if (currentSelectedMember != null)
        {
            UpdateEquipmentDisplay();
        }
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

        // Create weapon slot
        GameObject weaponSlot = Instantiate(equipmentSlotPrefab, equipmentSlotParent);
        EquipmentSlotUI weaponUI = weaponSlot.GetComponent<EquipmentSlotUI>();
        weaponUI.Initialize(EquipmentSlot.Arma, currentSelectedMember.weapon, this);
        equipmentSlots[EquipmentSlot.Arma] = weaponUI;

        // Create armor slot
        GameObject armorSlot = Instantiate(equipmentSlotPrefab, equipmentSlotParent);
        EquipmentSlotUI armorUI = armorSlot.GetComponent<EquipmentSlotUI>();
        armorUI.Initialize(EquipmentSlot.Armadura, currentSelectedMember.armor, this);
        equipmentSlots[EquipmentSlot.Armadura] = armorUI;
    }

    public void RefreshInventoryDisplay()
    {
        if (inventory == null) return;

        // Update gold
        if (goldText != null)
        {
            goldText.text = "Ouro: " + inventory.moedas.ToString();
        }

        // Clear the grid
        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        // Build the inventory
        foreach (SlotInventario slot in inventory.inventario)
        {
            GameObject newSlot = Instantiate(itemSlotPrefab, inventoryGrid);
            SlotUI slotUI = newSlot.GetComponent<SlotUI>();

            // Set up references
            slotUI.partyMenuManager = this;
            slotUI.itemDetailsPrefab = itemDetailsPrefab;
            slotUI.partyMemberSelectorPrefab = partyMemberSelectorPrefab;
            slotUI.partyMemberButtonPrefab = partyMemberButtonPrefab;

            slotUI.ConfigurarSlot(slot);
            inventorySlots.Add(slotUI);
        }

        // Clear highlight
        currentlyHighlightedSlot = null;
    }

    public void HighlightSlot(SlotUI selectedSlot)
    {
        // Unhighlight previous slot
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(false);
        }

        // Highlight new slot
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

        // Add back to inventory
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
        partyMenuPanel.SetActive(true);

        // Refresh displays
        CreatePartyMemberDisplays();
        RefreshInventoryDisplay();
    }

    public void CloseMenu()
    {
        partyMenuPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onInventarioMudou -= RefreshInventoryDisplay;
        }
    }
}