using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI quantityText;
    public Button button;
    public Image highlightBorder; // Optional: shows selected item

    private SlotInventario slotData;
    private InterfaceInventario inventoryInterface;
    private PartyMenuManager partyMenuManager;

    private void Start()
    {
        // Find references if not set
        if (inventoryInterface == null)
            inventoryInterface = GetComponentInParent<InterfaceInventario>();

        if (partyMenuManager == null)
            partyMenuManager = FindFirstObjectByType<PartyMenuManager>();

        if (button != null)
            button.onClick.AddListener(OnClick);

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
            // Empty slot (shouldn't happen but just in case)
            if (itemIcon != null)
                itemIcon.gameObject.SetActive(false);

            if (itemNameText != null)
                itemNameText.text = "Empty";

            if (quantityText != null)
                quantityText.text = "";
        }
    }

    private void OnClick()
    {
        if (slotData != null && slotData.dadosDoItem != null)
        {
            // Notify party menu manager that an item was selected
            if (partyMenuManager != null)
            {
                partyMenuManager.OnItemSelected(slotData.dadosDoItem, slotData);
            }

            // Highlight this slot
            if (inventoryInterface != null)
            {
                inventoryInterface.HighlightSlot(this);
            }
        }
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
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}