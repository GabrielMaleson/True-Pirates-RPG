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
    public Image highlightBorder;

    [Header("Item Details")]
    public GameObject itemDetailsPrefab; // Reference to the ItemDetails prefab
    public Transform detailsParent; // Where to spawn details (usually the canvas)

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

        if (detailsParent == null)
            detailsParent = GameObject.Find("Canvas")?.transform; // Default to canvas

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
            // Highlight this slot
            if (inventoryInterface != null)
            {
                inventoryInterface.HighlightSlot(this);
            }

            // Spawn item details
            SpawnItemDetails();
        }
    }

    private void SpawnItemDetails()
    {
        if (itemDetailsPrefab == null || detailsParent == null) return;

        // Check if details for this item already exist (optional)
        ItemDetails[] existingDetails = detailsParent.GetComponentsInChildren<ItemDetails>();
        foreach (var details in existingDetails)
        {
            // If details for same item exist, just return or activate them
            // This is optional behavior
        }

        // Spawn new details
        GameObject detailsObj = Instantiate(itemDetailsPrefab, detailsParent);
        ItemDetails itemDetails = detailsObj.GetComponent<ItemDetails>();

        if (itemDetails != null)
        {
            CharacterData currentCharacter = partyMenuManager?.GetCurrentSelectedCharacter();
            itemDetails.Initialize(slotData.dadosDoItem, slotData, currentCharacter);
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