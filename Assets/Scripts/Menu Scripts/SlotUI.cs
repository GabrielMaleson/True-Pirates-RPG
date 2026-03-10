using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI quantityText;
    public Image highlightBorder;
    public Button clickButton;

    [Header("Prefabs (set by PartyMenuManager)")]
    [HideInInspector] public GameObject itemDetailsPrefab;
    [HideInInspector] public GameObject partyMemberSelectorPrefab;
    [HideInInspector] public GameObject partyMemberButtonPrefab;

    [Header("References")]
    [HideInInspector] public PartyMenuManager partyMenuManager;

    private GameObject activeTooltip;
    private GameObject activeSelector;
    private Transform tooltipParent;
    private SlotInventario slotData;

    private void Start()
    {
        if (tooltipParent == null)
            tooltipParent = GameObject.Find("Canvas")?.transform;

        if (clickButton != null)
            clickButton.onClick.AddListener(OnItemClick);

        // Initially not highlighted
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(false);
    }

    public void ConfigurarSlot(SlotInventario slot)
    {
        slotData = slot;

        if (slot.dadosDoItem != null)
        {
            if (itemIcon != null && slot.dadosDoItem.icone != null)
            {
                itemIcon.gameObject.SetActive(true);
                itemIcon.sprite = slot.dadosDoItem.icone;
            }

            if (itemNameText != null)
                itemNameText.text = slot.dadosDoItem.nomeDoItem;

            if (quantityText != null)
                quantityText.text = $"x{slot.quantidade}";
        }
        else
        {
            if (itemIcon != null)
                itemIcon.gameObject.SetActive(false);

            if (itemNameText != null)
                itemNameText.text = "Empty";

            if (quantityText != null)
                quantityText.text = "";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotData != null && slotData.dadosDoItem != null && itemDetailsPrefab != null && tooltipParent != null)
        {
            if (activeTooltip != null)
                Destroy(activeTooltip);

            // Create ItemDetails as tooltip
            activeTooltip = Instantiate(itemDetailsPrefab, tooltipParent);
            activeTooltip.transform.position = Input.mousePosition + new Vector3(10, -10, 0);

            // Make it look like a tooltip (smaller, no buttons)
            ItemDetails details = activeTooltip.GetComponent<ItemDetails>();
            if (details != null)
            {
                // Hide buttons for tooltip mode
                if (details.useButton != null) details.useButton.gameObject.SetActive(false);
                if (details.equipButton != null) details.equipButton.gameObject.SetActive(false);
                if (details.dropButton != null) details.dropButton.gameObject.SetActive(false);
                if (details.closeButton != null) details.closeButton.gameObject.SetActive(false);

                // Make background semi-transparent
                Image bg = activeTooltip.GetComponent<Image>();
                if (bg != null)
                {
                    Color c = bg.color;
                    c.a = 0.9f;
                    bg.color = c;
                }

                // Make it smaller for tooltip
                RectTransform rt = activeTooltip.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = new Vector3(0.8f, 0.8f, 1f);
                }

                // Initialize with item data
                CharacterData currentCharacter = partyMenuManager?.GetCurrentSelectedCharacter();
                details.Initialize(slotData.dadosDoItem, slotData, currentCharacter);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
        }
    }

    private void OnItemClick()
    {
        if (slotData == null || slotData.dadosDoItem == null) return;

        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
        }

        if (partyMenuManager != null)
        {
            partyMenuManager.HighlightSlot(this);
        }

        ShowPartyMemberSelector();
    }

    private void ShowPartyMemberSelector()
    {
        if (partyMemberSelectorPrefab == null || partyMenuManager == null) return;

        if (activeSelector != null)
            Destroy(activeSelector);

        activeSelector = Instantiate(partyMemberSelectorPrefab, tooltipParent);
        activeSelector.transform.position = Input.mousePosition;

        // Find the button container
        Transform buttonContainer = activeSelector.transform.Find("ButtonContainer");
        if (buttonContainer == null)
        {
            Debug.LogError("PartyMemberSelector prefab must have a child named 'ButtonContainer'");
            return;
        }

        // Add title text - find or create it
        TextMeshProUGUI titleText = activeSelector.GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null)
        {
            if (slotData.dadosDoItem.ehConsumivel)
                titleText.text = "Use on which party member?";
            else if (slotData.dadosDoItem.ehEquipavel)
                titleText.text = "Equip to which party member?";
            else
                titleText.text = "Select party member:";
        }

        // Create party member buttons
        foreach (var member in partyMenuManager.inventory.partyMembers)
        {
            if (member != null && member.characterData != null)
            {
                GameObject btnObj = Instantiate(partyMemberButtonPrefab, buttonContainer);
                PartyMemberButton btn = btnObj.GetComponent<PartyMemberButton>();

                if (btn != null)
                {
                    CharacterData characterData = member.characterData;
                    btn.Initialize(characterData, partyMenuManager);

                    // Check if character can use this item
                    bool canUse = CanUseOnCharacter(characterData);

                    Button uiButton = btnObj.GetComponent<Button>();
                    if (uiButton != null)
                    {
                        uiButton.onClick.RemoveAllListeners();

                        if (canUse)
                        {
                            uiButton.onClick.AddListener(() => OnPartyMemberSelected(characterData));

                            // Reset any visual changes
                            TextMeshProUGUI nameText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                            if (nameText != null)
                                nameText.text = characterData.characterName;
                        }
                        else
                        {
                            // Disable button if can't use
                            uiButton.interactable = false;

                            // Add level requirement text
                            TextMeshProUGUI nameText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                            if (nameText != null)
                            {
                                if (slotData.dadosDoItem.ehEquipavel)
                                    nameText.text = $"{characterData.characterName} (Lv {slotData.dadosDoItem.nivelRequerido} req)";
                                else
                                    nameText.text = characterData.characterName;
                            }
                        }
                    }
                }
            }
        }

        // Add cancel button
        CreateCancelButton(buttonContainer);
    }

    private void CreateCancelButton(Transform parent)
    {
        GameObject cancelBtnObj = Instantiate(partyMemberButtonPrefab, parent);

        // Override the text
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

    private bool CanUseOnCharacter(CharacterData character)
    {
        if (slotData.dadosDoItem.ehConsumivel)
        {
            return true;
        }
        else if (slotData.dadosDoItem.ehEquipavel)
        {
            return character.level >= slotData.dadosDoItem.nivelRequerido;
        }
        return false;
    }

    private void OnPartyMemberSelected(CharacterData selectedCharacter)
    {
        DadosItem item = slotData.dadosDoItem;

        if (item.ehConsumivel)
        {
            bool used = selectedCharacter.UseConsumable(item);

            if (used)
            {
                partyMenuManager.inventory.RemoverItem(item, 1);
                partyMenuManager.RefreshInventoryDisplay();
                partyMenuManager.UpdateCharacterStats(selectedCharacter);
            }
        }
        else if (item.ehEquipavel)
        {
            bool equipped = selectedCharacter.EquipItem(item);

            if (equipped)
            {
                partyMenuManager.inventory.RemoverItem(item, 1);
                partyMenuManager.RefreshInventoryDisplay();
                partyMenuManager.UpdateEquipmentDisplay();
                partyMenuManager.UpdateCharacterStats(selectedCharacter);
            }
        }

        if (activeSelector != null)
            Destroy(activeSelector);
    }

    public void SetHighlight(bool highlighted)
    {
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(highlighted);
    }

    public SlotInventario GetSlotData()
    {
        return slotData;
    }

    private void OnDestroy()
    {
        if (clickButton != null)
            clickButton.onClick.RemoveListener(OnItemClick);
        if (activeTooltip != null)
            Destroy(activeTooltip);
        if (activeSelector != null)
            Destroy(activeSelector);
    }
}