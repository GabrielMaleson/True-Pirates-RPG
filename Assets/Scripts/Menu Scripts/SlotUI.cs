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
    public GameObject itemDetailsPrefab;
    public Transform detailsParent;

    // References set by PartyMenuManager
    [HideInInspector] public PartyMenuManager partyMenuManager;

    private SlotInventario slotData;

    private void Start()
    {
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
            if (partyMenuManager != null)
            {
                partyMenuManager.HighlightSlot(this);
            }

            // Spawn item details
            SpawnItemDetails();
        }
    }

    private void SpawnItemDetails()
    {
        if (itemDetailsPrefab == null) return;

        Transform parent = detailsParent != null ? detailsParent : transform.root;

        GameObject detailsObj = Instantiate(itemDetailsPrefab, parent);
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