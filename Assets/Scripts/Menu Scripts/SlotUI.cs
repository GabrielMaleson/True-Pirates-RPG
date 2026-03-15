using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

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

    [Header("Tooltip Settings")]
    public float tooltipDelay = 0.5f;
    public Vector2 tooltipOffset = new Vector2(10, -10);

    private GameObject activeTooltip;
    private GameObject activeSelector;
    private SlotInventario slotData;
    private Coroutine tooltipCoroutine;
    private bool isPointerOver = false;

    private void Start()
    {
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

                // Setup highlight border to match icon size + 0.1f
                if (highlightBorder != null)
                {
                    // Make highlight border a copy of the icon sprite
                    highlightBorder.sprite = slot.dadosDoItem.icone;
                    highlightBorder.color = Color.yellow; // Yellow highlight

                    // Make it 10% larger than the icon
                    RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
                    RectTransform borderRect = highlightBorder.GetComponent<RectTransform>();

                    if (iconRect != null && borderRect != null)
                    {
                        borderRect.sizeDelta = iconRect.sizeDelta * 1.1f;
                    }
                }
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

            if (highlightBorder != null)
                highlightBorder.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        if (slotData != null && slotData.dadosDoItem != null && itemDetailsPrefab != null && partyMenuManager != null)
        {
            // Start coroutine to show tooltip after delay
            if (tooltipCoroutine != null)
                StopCoroutine(tooltipCoroutine);

            tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        // Cancel tooltip coroutine
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }

        // Destroy tooltip immediately
        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
        }
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSeconds(tooltipDelay);

        // Only show if pointer is still over this slot
        if (!isPointerOver || activeTooltip != null)
            yield break;

        // Use PartyMenuManager's canvas as parent
        Transform parentCanvas = partyMenuManager.partyMenuCanvas;
        activeTooltip = Instantiate(itemDetailsPrefab, parentCanvas);
        activeTooltip.transform.SetAsLastSibling();

        // Position tooltip away from mouse to avoid overlap
        Vector2 mousePos = Input.mousePosition;
        activeTooltip.transform.position = mousePos;

        // Ensure tooltip stays within screen bounds
        RectTransform tooltipRect = activeTooltip.GetComponent<RectTransform>();
        if (tooltipRect != null)
        {
            Vector3[] corners = new Vector3[4];
            tooltipRect.GetWorldCorners(corners);
            float width = corners[2].x - corners[0].x;
            float height = corners[2].y - corners[0].y;

            // Check right edge
            if (corners[2].x > Screen.width)
            {
                activeTooltip.transform.position = new Vector3(
                    Screen.width - width - 10,
                    activeTooltip.transform.position.y,
                    activeTooltip.transform.position.z
                );
            }

            // Check bottom edge
            if (corners[0].y < 0)
            {
                activeTooltip.transform.position = new Vector3(
                    activeTooltip.transform.position.x,
                    height + 10,
                    activeTooltip.transform.position.z
                );
            }
        }

        // Make it look like a tooltip
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

            // Make sure tooltip doesn't block raycasts
            CanvasGroup cg = activeTooltip.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = activeTooltip.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            // Initialize with item data
            PartyMemberState currentCharacter = partyMenuManager?.GetCurrentSelectedMember();
            details.Initialize(slotData.dadosDoItem, slotData, currentCharacter);
        }

        tooltipCoroutine = null;
    }

    private void OnItemClick()
    {
        if (slotData == null || slotData.dadosDoItem == null) return;

        // Only allow click if item is consumable or equippable
        if (!slotData.dadosDoItem.ehConsumivel && !slotData.dadosDoItem.ehEquipavel)
        {
            Debug.Log($"Item {slotData.dadosDoItem.nomeDoItem} cannot be used or equipped.");
            return;
        }

        // Cancel any pending tooltip
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }

        // Destroy tooltip if showing
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

        // Use PartyMenuManager's canvas as parent
        Transform parentCanvas = partyMenuManager.partyMenuCanvas;
        activeSelector = Instantiate(partyMemberSelectorPrefab, parentCanvas);
        activeSelector.transform.SetAsLastSibling();
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
            if (member != null)
            {
                GameObject btnObj = Instantiate(partyMemberButtonPrefab, buttonContainer);
                PartyMemberButton btn = btnObj.GetComponent<PartyMemberButton>();

                if (btn != null)
                {
                    PartyMemberState memberState = member;
                    btn.Initialize(memberState, partyMenuManager);

                    // Check if character can use this item
                    bool canUse = CanUseOnCharacter(memberState);

                    Button uiButton = btnObj.GetComponent<Button>();
                    if (uiButton != null)
                    {
                        uiButton.onClick.RemoveAllListeners();

                        if (canUse)
                        {
                            uiButton.onClick.AddListener(() => OnPartyMemberSelected(memberState));

                            // Reset any visual changes
                            TextMeshProUGUI nameText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                            if (nameText != null)
                                nameText.text = memberState.CharacterName;
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
                                    nameText.text = $"{memberState.CharacterName} (Lv {slotData.dadosDoItem.nivelRequerido} req)";
                                else
                                    nameText.text = memberState.CharacterName;
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

    private bool CanUseOnCharacter(PartyMemberState character)
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

    private void OnPartyMemberSelected(PartyMemberState selectedCharacter)
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
            bool equipped = false;
            if (item.slotEquipamento == EquipmentSlot.Arma)
            {
                equipped = selectedCharacter.EquipWeapon(item);
            }
            else if (item.slotEquipamento == EquipmentSlot.Armadura)
            {
                equipped = selectedCharacter.EquipArmor(item);
            }

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
        {
            highlightBorder.gameObject.SetActive(highlighted);

            // Ensure border stays behind the icon
            if (highlighted && itemIcon != null)
            {
                highlightBorder.transform.SetSiblingIndex(itemIcon.transform.GetSiblingIndex());
            }
        }
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
        if (tooltipCoroutine != null)
            StopCoroutine(tooltipCoroutine);
    }
}