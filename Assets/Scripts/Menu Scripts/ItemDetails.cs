using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetails : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemStatsText;

    [Header("Buttons")]
    public Button useButton;
    public Button equipButton;
    public Button dropButton;
    public Button closeButton;

    [Header("References")]
    private SistemaInventario inventory;
    private PartyMenuManager menuManager;
    private DadosItem currentItem;
    private SlotInventario currentSlot;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<SistemaInventario>();

        if (menuManager == null)
            menuManager = FindFirstObjectByType<PartyMenuManager>();

        // Setup button listeners
        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);

        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    public void Initialize(DadosItem item, SlotInventario slot, PartyMemberState selectedCharacter)
    {
        currentItem = item;
        currentSlot = slot;

        // Set UI elements
        if (itemIcon != null && item.icone != null)
            itemIcon.sprite = item.icone;

        if (itemNameText != null)
            itemNameText.text = item.nomeDoItem;

        if (itemDescriptionText != null)
            itemDescriptionText.text = item.descricao;

        // Build stats text for equipment
        if (itemStatsText != null)
        {
            if (item.ehEquipavel && item.modificadoresStats.Count > 0)
            {
                string stats = "";
                foreach (var mod in item.modificadoresStats)
                {
                    string sign = mod.valorModificador > 0 ? "+" : "";
                    string percent = mod.tipoModificador == ModifierType.Percentual ? "%" : "";
                    stats += $"{mod.statType}: {sign}{mod.valorModificador}{percent}\n";
                }

                // Add requirement info
                if (item.nivelRequerido > 0)
                {
                    stats += $"Requires Level: {item.nivelRequerido}";
                }

                itemStatsText.text = stats;
                itemStatsText.gameObject.SetActive(true);
            }
            else
            {
                itemStatsText.gameObject.SetActive(false);
            }
        }

        // Set button interactability
        if (useButton != null)
            useButton.interactable = item.ehConsumivel && item.usavelEmBatalha && selectedCharacter != null;

        if (equipButton != null)
            equipButton.interactable = item.ehEquipavel && selectedCharacter != null;

        if (dropButton != null)
            dropButton.interactable = true;
    }

    private void OnUseClicked()
    {
        if (currentItem == null || currentSlot == null) return;

        PartyMemberState currentCharacter = menuManager?.GetCurrentSelectedMember();
        if (currentCharacter == null) return;

        bool used = currentCharacter.UseConsumable(currentItem);

        if (used && inventory != null)
        {
            inventory.RemoverItem(currentItem, 1);

            if (menuManager != null)
                menuManager.RefreshInventoryDisplay();

            if (currentSlot.quantidade <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnEquipClicked()
    {
        if (currentItem == null || !currentItem.ehEquipavel) return;

        PartyMemberState currentCharacter = menuManager?.GetCurrentSelectedMember();
        if (currentCharacter == null) return;

        if (currentItem.nivelRequerido > currentCharacter.level)
        {
            Debug.Log($"Cannot equip {currentItem.nomeDoItem} - requires level {currentItem.nivelRequerido}");
            return;
        }

        bool equipped = false;
        if (currentItem.slotEquipamento == EquipmentSlot.Arma)
        {
            equipped = currentCharacter.EquipWeapon(currentItem);
        }
        else if (currentItem.slotEquipamento == EquipmentSlot.Armadura)
        {
            equipped = currentCharacter.EquipArmor(currentItem);
        }

        if (equipped && inventory != null)
        {
            inventory.RemoverItem(currentItem, 1);

            if (menuManager != null)
            {
                menuManager.RefreshInventoryDisplay();
                menuManager.UpdateEquipmentDisplay();
                menuManager.UpdateCharacterStats(currentCharacter);
            }

            if (currentSlot.quantidade <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnDropClicked()
    {
        if (currentItem == null || currentSlot == null || inventory == null) return;

        inventory.RemoverItem(currentItem, 1);

        if (menuManager != null)
            menuManager.RefreshInventoryDisplay();

        Destroy(gameObject);
    }

    private void OnCloseClicked()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (useButton != null)
            useButton.onClick.RemoveListener(OnUseClicked);
        if (equipButton != null)
            equipButton.onClick.RemoveListener(OnEquipClicked);
        if (dropButton != null)
            dropButton.onClick.RemoveListener(OnDropClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);
    }
}