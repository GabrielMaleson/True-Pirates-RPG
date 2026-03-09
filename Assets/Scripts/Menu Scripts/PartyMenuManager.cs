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
    private Dictionary<CharacterData, PartyMemberStatsDisplay> statsDisplays =
        new Dictionary<CharacterData, PartyMemberStatsDisplay>();

    [Header("Attacks Display")]
    public Transform attackListParent;
    public GameObject attackDisplayPrefab;

    [Header("Equipment Display")]
    public Transform equipmentSlotParent;
    public GameObject equipmentSlotPrefab;
    private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlots =
        new Dictionary<EquipmentSlot, EquipmentSlotUI>();

    [Header("References")]
    public SistemaInventario inventory;
    public InterfaceInventario inventoryUI;

    private CharacterData currentSelectedCharacter;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<SistemaInventario>();

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InterfaceInventario>();

        // Initially hide all panels
        HideAllPanels();

        // Create party member buttons and stats displays
        CreatePartyMemberDisplays();
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
            if (member != null && member.characterData != null)
            {
                CharacterData characterData = member.characterData;

                // Create selection button
                GameObject btnObj = Instantiate(partyMemberButtonPrefab, partyMemberButtonParent);
                PartyMemberButton btn = btnObj.GetComponent<PartyMemberButton>();
                btn.Initialize(characterData, this);
                partyMemberButtons.Add(btn);

                // Create stats display
                GameObject displayObj = Instantiate(statsDisplayPrefab, statsDisplayContainer);
                PartyMemberStatsDisplay display = displayObj.GetComponent<PartyMemberStatsDisplay>();
                display.Initialize(characterData);
                statsDisplays[characterData] = display;

                // Initially hide all displays
                displayObj.SetActive(false);
            }
        }

        // Select first party member by default
        if (inventory.partyMembers.Count > 0 && inventory.partyMembers[0] != null)
        {
            OnPartyMemberSelected(inventory.partyMembers[0].characterData);
        }
    }

    public void OnPartyMemberSelected(CharacterData character)
    {
        currentSelectedCharacter = character;

        // Show only the selected character's stats display
        foreach (var kvp in statsDisplays)
        {
            kvp.Value.gameObject.SetActive(kvp.Key == character);
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
            btn.SetHighlight(btn.GetCharacterData() == currentSelectedCharacter);
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
    }

    public void ShowEquipment()
    {
        HideAllPanels();
        equipmentPanel.SetActive(true);

        if (currentSelectedCharacter != null)
        {
            UpdateEquipmentDisplay();
        }
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

        // Update stats display (stats may change from unequipping)
        if (statsDisplays.ContainsKey(currentSelectedCharacter))
            statsDisplays[currentSelectedCharacter].UpdateDisplay();
    }

    public void OpenMenu()
    {
        partyMenuPanel.SetActive(true);

        // Refresh displays
        CreatePartyMemberDisplays();
    }

    public void CloseMenu()
    {
        partyMenuPanel.SetActive(false);
    }

    public CharacterData GetCurrentSelectedCharacter()
    {
        return currentSelectedCharacter;
    }
}