using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public void Initialize(EquipmentSlot slot, DadosItem item, PartyMenuManager manager)
    {
        slotType = slot;
        equippedItem = item;
        menuManager = manager;

        slotNameText.text = slot.ToString();

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

    private void OnSlotClicked()
    {
        Debug.Log($"Clicked on {slotType} slot");
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
    }
}