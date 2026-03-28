using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class SlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image itemIcon;
    public Image highlightBorder;
    public Button clickButton;

    [Header("Equipped Indicator")]
    public GameObject equippedIndicator; // Parent object — active when item is equipped
    public GameObject checkmarkObject;   // Child shown at rest
    public GameObject unequipXObject;    // Child shown on hover

    [Header("Prefabs (set by PartyMenuManager)")]
    [HideInInspector] public GameObject itemDetailsPrefab;
    [HideInInspector] public GameObject partyMemberSelectorPrefab;

    [Header("References")]
    [HideInInspector] public PartyMenuManager partyMenuManager;

    [Header("Tooltip Settings")]
    public float tooltipDelay = 0.5f;

    private GameObject activeTooltip;
    private SlotInventario slotData;
    private Coroutine tooltipCoroutine;
    private bool isPointerOver = false;

    private void Awake()
    {
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(false);

        if (equippedIndicator != null)
            equippedIndicator.SetActive(false);
    }

    private void Start()
    {
        if (clickButton != null)
            clickButton.onClick.AddListener(OnItemClick);
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

                if (highlightBorder != null)
                {
                    highlightBorder.sprite = slot.dadosDoItem.icone;
                    highlightBorder.color = Color.yellow;
                    RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
                    RectTransform borderRect = highlightBorder.GetComponent<RectTransform>();
                    if (iconRect != null && borderRect != null)
                        borderRect.sizeDelta = iconRect.sizeDelta * 1.1f;
                }
            }

            RefreshEquippedIndicator();
        }
        else
        {
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            if (highlightBorder != null) highlightBorder.gameObject.SetActive(false);
            if (equippedIndicator != null) equippedIndicator.SetActive(false);
            if (equippedIndicator != null) equippedIndicator.gameObject.SetActive(false);
        }
    }

    private void RefreshEquippedIndicator()
    {
        if (equippedIndicator == null || slotData == null) return;

        bool isEquipped = slotData.equippedTo != null;
        equippedIndicator.SetActive(isEquipped);
        if (isEquipped)
        {
            if (checkmarkObject != null) checkmarkObject.SetActive(true);
            if (unequipXObject  != null) unequipXObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        // Switch indicator to X if equipped
        if (equippedIndicator != null && slotData?.equippedTo != null)
        {
            if (checkmarkObject != null) checkmarkObject.SetActive(false);
            if (unequipXObject  != null) unequipXObject.SetActive(true);
        }

        if (slotData != null && slotData.dadosDoItem != null && itemDetailsPrefab != null && partyMenuManager != null)
        {
            if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        // Restore checkmark indicator
        if (equippedIndicator != null && slotData?.equippedTo != null)
        {
            if (checkmarkObject != null) checkmarkObject.SetActive(true);
            if (unequipXObject  != null) unequipXObject.SetActive(false);
        }

        if (tooltipCoroutine != null) { StopCoroutine(tooltipCoroutine); tooltipCoroutine = null; }
        if (activeTooltip != null) { Destroy(activeTooltip); activeTooltip = null; }
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSeconds(tooltipDelay);
        if (!isPointerOver || activeTooltip != null) yield break;

        Transform parentCanvas = partyMenuManager.partyMenuCanvas;
        activeTooltip = Instantiate(itemDetailsPrefab, parentCanvas);
        activeTooltip.transform.SetAsLastSibling();

        // Position centered below this slot
        RectTransform slotRect    = GetComponent<RectTransform>();
        RectTransform tooltipRect = activeTooltip.GetComponent<RectTransform>();
        if (slotRect != null && tooltipRect != null)
        {
            // Get the four world corners of this slot (0=BL, 1=TL, 2=TR, 3=BR)
            Vector3[] slotCorners = new Vector3[4];
            slotRect.GetWorldCorners(slotCorners);

            // Center X of the slot, just below the bottom edge
            float centerX  = (slotCorners[0].x + slotCorners[3].x) * 0.5f;
            float bottomY  = slotCorners[0].y;

            activeTooltip.transform.position = new Vector3(centerX, bottomY, 0f);

            // After placing, clamp so it stays on screen
            Canvas.ForceUpdateCanvases();
            Vector3[] tipCorners = new Vector3[4];
            tooltipRect.GetWorldCorners(tipCorners);
            float tipWidth  = tipCorners[2].x - tipCorners[0].x;
            float tipHeight = tipCorners[2].y - tipCorners[0].y;

            float x = activeTooltip.transform.position.x;
            float y = activeTooltip.transform.position.y;

            if (x - tipWidth * 0.5f < 0)            x = tipWidth * 0.5f;
            if (x + tipWidth * 0.5f > Screen.width)  x = Screen.width - tipWidth * 0.5f;
            if (y - tipHeight < 0)                    y = tipHeight;

            activeTooltip.transform.position = new Vector3(x, y, 0f);
        }

        ItemDetails details = activeTooltip.GetComponent<ItemDetails>();
        if (details != null) details.Initialize(slotData.dadosDoItem);

        CanvasGroup cg = activeTooltip.GetComponent<CanvasGroup>();
        if (cg == null) cg = activeTooltip.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        tooltipCoroutine = null;
    }

    private void OnItemClick()
    {
        if (tooltipCoroutine != null) { StopCoroutine(tooltipCoroutine); tooltipCoroutine = null; }
        if (activeTooltip != null)    { Destroy(activeTooltip); activeTooltip = null; }

        // "Remover Item" slot has no slotData but has a listener added externally
        if (slotData == null) return;
        if (slotData.dadosDoItem == null) return;

        DadosItem item = slotData.dadosDoItem;

        // ── Equip-filter mode (Equipment tab: character → slot → item) ────────
        if (partyMenuManager != null &&
            partyMenuManager.GetPendingOp() == PartyMenuManager.PendingOpType.EquipItem)
        {
            if (item.ehEquipavel && item.slotEquipamento == partyMenuManager.GetPendingEquipSlot())
                partyMenuManager.OnEquipItemSelectedFromPanel(slotData);
            return;
        }

        // ── Equipped item: clicking the X unequips directly ───────────────────
        if (slotData.equippedTo != null && partyMenuManager.GetPendingOp() == PartyMenuManager.PendingOpType.None)
        {
            partyMenuManager.inventory.UnequipItemFromSlot(slotData);
            partyMenuManager.RefreshInventoryDisplay();
            partyMenuManager.RefreshEquipmentCards();
            return;
        }

        // ── Normal mode ───────────────────────────────────────────────────────
        if (partyMenuManager != null) partyMenuManager.HighlightSlot(this);

        if (item.ehConsumivel && item.usavelNoMapa)
        {
            partyMenuManager?.StartUseItemTargeting(slotData);
        }
        else if (item.ehEquipavel)
        {
            partyMenuManager?.StartEquipItemFromInventory(slotData);
        }
    }

    public void SetHighlight(bool highlighted)
    {
        if (highlightBorder != null)
        {
            highlightBorder.gameObject.SetActive(highlighted);
            if (highlighted && itemIcon != null)
                highlightBorder.transform.SetSiblingIndex(itemIcon.transform.GetSiblingIndex());
        }
    }

    public SlotInventario GetSlotData() => slotData;

    private void OnDestroy()
    {
        if (clickButton != null) clickButton.onClick.RemoveListener(OnItemClick);
        if (activeTooltip != null) Destroy(activeTooltip);
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
    }
}
