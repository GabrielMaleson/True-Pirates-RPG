using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI slotNameText;
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemStatsText;
    public Button slotButton;
    public Button unequipButton;

    private EquipmentSlot slotType;
    private DadosItem equippedItem;
    private PartyMenuManager menuManager;
    private GameObject activeSelector;

    public void Initialize(EquipmentSlot slot, DadosItem item, PartyMenuManager manager)
    {
        slotType = slot;
        equippedItem = item;
        menuManager = manager;

        slotNameText.text = GetSlotDisplayName(slot);

        if (equippedItem != null)
        {
            if (itemIcon != null && equippedItem.icone != null)
            {
                itemIcon.gameObject.SetActive(true);
                itemIcon.sprite = equippedItem.icone;
            }

            itemNameText.text = equippedItem.nomeDoItem;

            // Build stats text
            string statsText = "";
            foreach (var modifier in equippedItem.modificadoresStats)
            {
                string sign = modifier.valorModificador > 0 ? "+" : "";
                string percent = modifier.tipoModificador == ModifierType.Percentual ? "%" : "";
                statsText += $"{modifier.statType}: {sign}{modifier.valorModificador}{percent}\n";
            }

            // Add requirement info
            if (equippedItem.nivelRequerido > 0)
            {
                statsText += $"Requires Level: {equippedItem.nivelRequerido}";
            }

            if (itemStatsText != null)
                itemStatsText.text = statsText;

            if (unequipButton != null)
            {
                unequipButton.gameObject.SetActive(true);
                unequipButton.onClick.AddListener(OnUnequipClicked);
            }
        }
        else
        {
            if (itemIcon != null)
                itemIcon.gameObject.SetActive(false);

            itemNameText.text = "Empty";

            if (itemStatsText != null)
                itemStatsText.text = "";

            if (unequipButton != null)
                unequipButton.gameObject.SetActive(false);
        }

        if (slotButton != null)
            slotButton.onClick.AddListener(OnSlotClicked);
    }

    private string GetSlotDisplayName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Arma:
                return "Weapon";
            case EquipmentSlot.Armadura:
                return "Armor";
            default:
                return slot.ToString();
        }
    }

    private void OnSlotClicked()
    {
        if (equippedItem != null)
        {
            Debug.Log($"Clicked on {slotType} slot with {equippedItem.nomeDoItem}");
        }
        else
        {
            // Empty slot - show party member selector with equippable items
            ShowEquippableItemsSelector();
        }
    }

    private void ShowEquippableItemsSelector()
    {
        if (menuManager == null || menuManager.partyMemberSelectorPrefab == null)
            return;

        if (activeSelector != null)
            Destroy(activeSelector);

        // Use PartyMenuManager's canvas as parent
        Transform parentCanvas = menuManager.partyMenuCanvas;
        activeSelector = Instantiate(menuManager.partyMemberSelectorPrefab, parentCanvas);
        activeSelector.transform.SetAsLastSibling();
        activeSelector.transform.position = Input.mousePosition;

        // Find the button container
        Transform buttonContainer = activeSelector.transform.Find("ButtonContainer");
        if (buttonContainer == null)
        {
            Debug.LogError("PartyMemberSelector prefab must have a child named 'ButtonContainer'");
            Destroy(activeSelector);
            return;
        }

        // Set title text
        TextMeshProUGUI titleText = activeSelector.GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = $"Select character to equip {GetSlotDisplayName(slotType)}:";
        }

        // Get all equippable items of this slot type from inventory
        List<DadosItem> equippableItems = new List<DadosItem>();
        foreach (var slot in menuManager.inventory.inventario)
        {
            if (slot.dadosDoItem != null &&
                slot.dadosDoItem.ehEquipavel &&
                slot.dadosDoItem.slotEquipamento == slotType)
            {
                equippableItems.Add(slot.dadosDoItem);
            }
        }

        if (equippableItems.Count == 0)
        {
            // No items to equip - show message
            TextMeshProUGUI messageText = activeSelector.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText != null)
                messageText.text = "No equippable items found!";

            CreateCloseButton(buttonContainer);
            return;
        }

        // Create a button for each party member
        foreach (var member in menuManager.inventory.partyMembers)
        {
            if (member != null)
            {
                // Check which items this member can equip
                List<DadosItem> usableItems = new List<DadosItem>();
                foreach (var item in equippableItems)
                {
                    if (member.level >= item.nivelRequerido)
                    {
                        usableItems.Add(item);
                    }
                }

                // Create party member button
                GameObject btnObj = Instantiate(menuManager.partyMemberButtonPrefab, buttonContainer);
                PartyMemberButton btn = btnObj.GetComponent<PartyMemberButton>();

                if (btn != null)
                {
                    btn.Initialize(member, menuManager);

                    Button uiButton = btnObj.GetComponent<Button>();
                    if (uiButton != null)
                    {
                        if (usableItems.Count > 0)
                        {
                            uiButton.onClick.RemoveAllListeners();
                            uiButton.onClick.AddListener(() => ShowItemSelectorForMember(member, equippableItems));
                        }
                        else
                        {
                            uiButton.interactable = false;

                            TextMeshProUGUI nameText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                            if (nameText != null)
                            {
                                int minLevel = GetMinLevelRequired(equippableItems);
                                nameText.text = $"{member.CharacterName} (Lv {minLevel} req)";
                            }
                        }
                    }
                }
            }
        }

        // Add cancel button
        CreateCancelButton(buttonContainer);
    }

    private void ShowItemSelectorForMember(PartyMemberState member, List<DadosItem> items)
    {
        if (activeSelector != null)
            Destroy(activeSelector);

        // Use PartyMenuManager's canvas as parent
        Transform parentCanvas = menuManager.partyMenuCanvas;
        activeSelector = Instantiate(menuManager.partyMemberSelectorPrefab, parentCanvas);
        activeSelector.transform.SetAsLastSibling();
        activeSelector.transform.position = Input.mousePosition;

        Transform buttonContainer = activeSelector.transform.Find("ButtonContainer");
        if (buttonContainer == null) return;

        // Set title
        TextMeshProUGUI titleText = activeSelector.GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = $"Select {GetSlotDisplayName(slotType)} for {member.CharacterName}:";
        }

        // Filter items this member can use
        List<DadosItem> usableItems = new List<DadosItem>();
        foreach (var item in items)
        {
            if (member.level >= item.nivelRequerido)
            {
                usableItems.Add(item);
            }
        }

        // Create buttons for each usable item
        foreach (var item in usableItems)
        {
            GameObject btnObj = Instantiate(menuManager.partyMemberButtonPrefab, buttonContainer);

            // Customize button for item
            TextMeshProUGUI nameText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = item.nomeDoItem;

            // Add stat info as subtitle
            TextMeshProUGUI[] texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 1)
            {
                string stats = "";
                foreach (var mod in item.modificadoresStats)
                {
                    string sign = mod.valorModificador > 0 ? "+" : "";
                    string percent = mod.tipoModificador == ModifierType.Percentual ? "%" : "";
                    stats += $"{mod.statType}: {sign}{mod.valorModificador}{percent} ";
                }
                texts[1].text = stats;
            }

            Button uiButton = btnObj.GetComponent<Button>();
            if (uiButton != null)
            {
                uiButton.onClick.RemoveAllListeners();
                uiButton.onClick.AddListener(() => OnItemSelectedForMember(item, member));
            }
        }

        // Add back button
        CreateBackButton(buttonContainer, () => ShowEquippableItemsSelector());
    }

    private void OnItemSelectedForMember(DadosItem item, PartyMemberState member)
    {
        // Equip the item to the selected member
        bool equipped = false;

        if (slotType == EquipmentSlot.Arma)
        {
            equipped = member.EquipWeapon(item);
        }
        else if (slotType == EquipmentSlot.Armadura)
        {
            equipped = member.EquipArmor(item);
        }

        if (equipped && menuManager != null && menuManager.inventory != null)
        {
            // Remove from inventory
            menuManager.inventory.RemoverItem(item, 1);

            // Refresh displays
            menuManager.RefreshInventoryDisplay();
            menuManager.UpdateEquipmentDisplay();
            menuManager.UpdateCharacterStats(member);
        }

        // Close selector
        if (activeSelector != null)
            Destroy(activeSelector);
    }

    private int GetMinLevelRequired(List<DadosItem> items)
    {
        int minLevel = int.MaxValue;
        foreach (var item in items)
        {
            if (item.nivelRequerido < minLevel)
                minLevel = item.nivelRequerido;
        }
        return minLevel;
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject closeBtnObj = Instantiate(menuManager.partyMemberButtonPrefab, parent);

        TextMeshProUGUI nameText = closeBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = "Close";

        Button uiButton = closeBtnObj.GetComponent<Button>();
        if (uiButton != null)
        {
            uiButton.onClick.RemoveAllListeners();
            uiButton.onClick.AddListener(() => {
                if (activeSelector != null)
                    Destroy(activeSelector);
            });
        }
    }

    private void CreateCancelButton(Transform parent)
    {
        GameObject cancelBtnObj = Instantiate(menuManager.partyMemberButtonPrefab, parent);

        TextMeshProUGUI nameText = cancelBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = "Cancel";

        Button uiButton = cancelBtnObj.GetComponent<Button>();
        if (uiButton != null)
        {
            uiButton.onClick.RemoveAllListeners();
            uiButton.onClick.AddListener(() => {
                if (activeSelector != null)
                    Destroy(activeSelector);
            });
        }
    }

    private void CreateBackButton(Transform parent, System.Action backAction)
    {
        GameObject backBtnObj = Instantiate(menuManager.partyMemberButtonPrefab, parent);

        TextMeshProUGUI nameText = backBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = "Back";

        Button uiButton = backBtnObj.GetComponent<Button>();
        if (uiButton != null)
        {
            uiButton.onClick.RemoveAllListeners();
            uiButton.onClick.AddListener(() => {
                backAction?.Invoke();
            });
        }
    }

    private void OnUnequipClicked()
    {
        if (equippedItem != null && menuManager != null)
        {
            menuManager.UnequipItem(equippedItem, slotType);
        }
    }

    private void OnDestroy()
    {
        if (slotButton != null)
            slotButton.onClick.RemoveListener(OnSlotClicked);
        if (unequipButton != null)
            unequipButton.onClick.RemoveListener(OnUnequipClicked);
        if (activeSelector != null)
            Destroy(activeSelector);
    }
}