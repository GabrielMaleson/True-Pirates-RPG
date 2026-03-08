using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PartyMenuManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject partyMenuPanel;
    public GameObject statsPanel;
    public GameObject attacksPanel;
    public GameObject itemsPanel;
    public GameObject equipmentPanel;

    [Header("Party Member Selection")]
    public Transform partyMemberButtonParent;
    public GameObject partyMemberButtonPrefab;
    private List<PartyMemberButton> partyMemberButtons = new List<PartyMemberButton>();

    [Header("Stats Display")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI apText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI expText;

    [Header("Attacks Display")]
    public Transform attackListParent;
    public GameObject attackDisplayPrefab;

    [Header("Equipment Display")]
    public Transform equipmentSlotParent;
    public GameObject equipmentSlotPrefab;
    private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlots = new Dictionary<EquipmentSlot, EquipmentSlotUI>();

    [Header("Item Details Panel")]
    public GameObject itemDetailsPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemStatsText;
    public Image itemIcon;
    public Button useItemButton;
    public Button equipItemButton;
    public Button dropItemButton;

    [Header("References")]
    public SistemaInventario inventory;
    public InterfaceInventario inventoryUI;
    private CharacterData currentSelectedCharacter;
    private DadosItem selectedItem;
    private SlotInventario selectedSlot;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<SistemaInventario>();

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InterfaceInventario>();

        // Initially hide all panels
        HideAllPanels();

        // Create party member buttons
        CreatePartyMemberButtons();

        // Setup item action buttons
        if (useItemButton != null)
            useItemButton.onClick.AddListener(OnUseItemClicked);

        if (equipItemButton != null)
            equipItemButton.onClick.AddListener(OnEquipItemClicked);

        if (dropItemButton != null)
            dropItemButton.onClick.AddListener(OnDropItemClicked);

        // Hide item details panel initially
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
        if (attacksPanel != null) attacksPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
    }

    private void CreatePartyMemberButtons()
    {
        // Clear existing buttons
        foreach (var btn in partyMemberButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        partyMemberButtons.Clear();

        // Create buttons for each party member
        foreach (var member in inventory.partyMembers)
        {
            if (member != null && member.characterData != null)
            {
                GameObject btnObj = Instantiate(partyMemberButtonPrefab, partyMemberButtonParent);
                PartyMemberButton btn = btnObj.GetComponent<PartyMemberButton>();
                btn.Initialize(member.characterData, this);
                partyMemberButtons.Add(btn);
            }
        }
    }

    public void OnPartyMemberSelected(CharacterData character)
    {
        currentSelectedCharacter = character;

        // Show stats panel by default
        ShowStats();

        // Update party member highlights
        UpdatePartyMemberHighlights();

        // Hide item details when switching characters
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
    }

    public void UpdatePartyMemberHighlights()
    {
        foreach (var btn in partyMemberButtons)
        {
            btn.SetHighlight(btn.GetCharacterData() == currentSelectedCharacter);
        }
    }

    public void ShowStats()
    {
        HideAllPanels();
        statsPanel.SetActive(true);

        if (currentSelectedCharacter != null)
        {
            UpdateStatsDisplay();
        }
    }

    public void ShowAttacks()
    {
        HideAllPanels();
        attacksPanel.SetActive(true);

        if (currentSelectedCharacter != null)
        {
            UpdateAttacksDisplay();
        }

        // Hide item details
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
    }

    public void ShowItems()
    {
        HideAllPanels();
        itemsPanel.SetActive(true);

        // Refresh the inventory UI
        if (inventoryUI != null)
        {
            inventoryUI.AtualizarInterface();
        }

        // Hide item details
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
    }

    public void ShowEquipment()
    {
        HideAllPanels();
        equipmentPanel.SetActive(true);

        if (currentSelectedCharacter != null)
        {
            UpdateEquipmentDisplay();
        }

        // Hide item details
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
    }

    private void UpdateStatsDisplay()
    {
        if (currentSelectedCharacter == null) return;

        characterNameText.text = currentSelectedCharacter.characterName;
        levelText.text = $"Level: {currentSelectedCharacter.level}";
        hpText.text = $"HP: {currentSelectedCharacter.currentHP}/{currentSelectedCharacter.hp}";
        apText.text = $"AP: {currentSelectedCharacter.currentAP}/{currentSelectedCharacter.maxAP}";
        attackText.text = $"Attack: {currentSelectedCharacter.attack}";
        defenseText.text = $"Defense: {currentSelectedCharacter.defense}";

        int nextLevelExp = currentSelectedCharacter.level * 100;
        expText.text = $"EXP: {currentSelectedCharacter.currentExperience}/{nextLevelExp}";
    }

    private void UpdateAttacksDisplay()
    {
        // Clear existing
        foreach (Transform child in attackListParent)
        {
            Destroy(child.gameObject);
        }

        if (currentSelectedCharacter == null) return;

        foreach (var attack in currentSelectedCharacter.availableAttacks)
        {
            GameObject attackObj = Instantiate(attackDisplayPrefab, attackListParent);
            AttackDisplay display = attackObj.GetComponent<AttackDisplay>();
            if (display != null)
            {
                display.Initialize(attack);
            }
        }
    }

    private void UpdateEquipmentDisplay()
    {
        // Clear existing
        foreach (Transform child in equipmentSlotParent)
        {
            Destroy(child.gameObject);
        }
        equipmentSlots.Clear();

        if (currentSelectedCharacter == null) return;

        // Create slots for each equipment type
        foreach (EquipmentSlot slotType in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            GameObject slotObj = Instantiate(equipmentSlotPrefab, equipmentSlotParent);
            EquipmentSlotUI slotUI = slotObj.GetComponent<EquipmentSlotUI>();

            // Find equipped item of this type
            DadosItem equippedItem = null;
            foreach (var item in currentSelectedCharacter.equippedItems)
            {
                if (item != null && item.ehEquipavel && item.slotEquipamento == slotType)
                {
                    equippedItem = item;
                    break;
                }
            }

            slotUI.Initialize(slotType, equippedItem, this);
            equipmentSlots[slotType] = slotUI;
        }
    }

    public void OnItemSelected(DadosItem item, SlotInventario slot)
    {
        selectedItem = item;
        selectedSlot = slot;

        // Show item details panel
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(true);

        // Update item details UI
        if (itemNameText != null)
            itemNameText.text = item.nomeDoItem;

        if (itemDescriptionText != null)
            itemDescriptionText.text = item.descricao;

        if (itemIcon != null && item.icone != null)
            itemIcon.sprite = item.icone;

        // Build stats text for equipment
        if (itemStatsText != null)
        {
            if (item.ehEquipavel && item.modificadoresStats.Count > 0)
            {
                string stats = "";
                foreach (var mod in item.modificadoresStats)
                {
                    string sign = mod.valorModificador > 0 ? "+" : "";
                    stats += $"{mod.statType}: {sign}{mod.valorModificador}\n";
                }
                itemStatsText.text = stats;
            }
            else
            {
                itemStatsText.text = "";
            }
        }

        // Enable/disable buttons based on item type and context
        if (useItemButton != null)
        {
            useItemButton.interactable = item.ehConsumivel && item.usavelEmBatalha && currentSelectedCharacter != null;
        }

        if (equipItemButton != null)
        {
            equipItemButton.interactable = item.ehEquipavel && currentSelectedCharacter != null;
        }

        if (dropItemButton != null)
        {
            dropItemButton.interactable = true;
        }
    }

    private void OnUseItemClicked()
    {
        if (selectedItem == null || currentSelectedCharacter == null || selectedSlot == null) return;

        // Use item on current character
        bool used = currentSelectedCharacter.UseConsumable(selectedItem);

        if (used && inventory != null)
        {
            inventory.RemoverItem(selectedItem, 1);

            // Refresh displays
            if (inventoryUI != null)
                inventoryUI.AtualizarInterface();

            UpdateStatsDisplay();

            // Hide item details if item is gone
            if (selectedSlot.quantidade <= 0)
            {
                if (itemDetailsPanel != null)
                    itemDetailsPanel.SetActive(false);
                selectedItem = null;
                selectedSlot = null;
            }
        }
    }

    private void OnEquipItemClicked()
    {
        if (selectedItem == null || currentSelectedCharacter == null || !selectedItem.ehEquipavel) return;

        // Check level requirement
        if (selectedItem.nivelRequerido > currentSelectedCharacter.level)
        {
            Debug.Log($"Cannot equip {selectedItem.nomeDoItem} - requires level {selectedItem.nivelRequerido}");
            // You could show a message to the player here
            return;
        }

        // Equip item to current character
        bool equipped = currentSelectedCharacter.EquipItem(selectedItem);

        if (equipped && inventory != null)
        {
            inventory.RemoverItem(selectedItem, 1);

            // Refresh displays
            if (inventoryUI != null)
                inventoryUI.AtualizarInterface();

            UpdateEquipmentDisplay();
            UpdateStatsDisplay(); // Stats may change from equipment

            // Hide item details if item is gone
            if (selectedSlot.quantidade <= 0)
            {
                if (itemDetailsPanel != null)
                    itemDetailsPanel.SetActive(false);
                selectedItem = null;
                selectedSlot = null;
            }
        }
    }

    private void OnDropItemClicked()
    {
        if (selectedItem == null || selectedSlot == null || inventory == null) return;

        // Remove item from inventory (drop it)
        inventory.RemoverItem(selectedItem, 1);

        // Refresh inventory display
        if (inventoryUI != null)
            inventoryUI.AtualizarInterface();

        // Hide item details if item is gone
        if (selectedSlot.quantidade <= 0)
        {
            if (itemDetailsPanel != null)
                itemDetailsPanel.SetActive(false);
            selectedItem = null;
            selectedSlot = null;
        }
    }

    public void UnequipItem(DadosItem item, EquipmentSlot slot)
    {
        if (currentSelectedCharacter == null) return;

        currentSelectedCharacter.UnequipItem(item);

        // Add back to inventory
        if (inventory != null)
        {
            inventory.AdicionarItem(item, 1);

            // Refresh inventory UI
            if (inventoryUI != null)
                inventoryUI.AtualizarInterface();
        }

        UpdateEquipmentDisplay();
        UpdateStatsDisplay();
    }

    public void OpenMenu()
    {
        partyMenuPanel.SetActive(true);
        CreatePartyMemberButtons();

        // Select first party member by default
        if (inventory.partyMembers.Count > 0 && inventory.partyMembers[0] != null)
        {
            OnPartyMemberSelected(inventory.partyMembers[0].characterData);
        }
    }

    public void CloseMenu()
    {
        partyMenuPanel.SetActive(false);
        // Hide item details when closing menu
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);
    }

    public CharacterData GetCurrentSelectedCharacter()
    {
        return currentSelectedCharacter;
    }
}