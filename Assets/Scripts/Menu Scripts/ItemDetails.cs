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

    public void Initialize(DadosItem item, SlotInventario slot, CharacterData selectedCharacter)
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

        CharacterData currentCharacter = menuManager?.GetCurrentSelectedCharacter();
        if (currentCharacter == null) return;

        // Use item on current character
        bool used = currentCharacter.UseConsumable(currentItem);

        if (used && inventory != null)
        {
            inventory.RemoverItem(currentItem, 1);

            // Find and refresh PartyMenuManager
            PartyMenuManager menuManager = FindFirstObjectByType<PartyMenuManager>();
            if (menuManager != null)
                menuManager.RefreshInventoryDisplay();

            // Close if item is gone
            if (currentSlot.quantidade <= 0)
            {
                Destroy(gameObject);
            }
            else
            {
                // Update quantity display in inventory, keep details open
                Initialize(currentItem, currentSlot, currentCharacter);
            }
        }
    }

    private void OnEquipClicked()
    {
        if (currentItem == null || !currentItem.ehEquipavel) return;

        CharacterData currentCharacter = menuManager?.GetCurrentSelectedCharacter();
        if (currentCharacter == null) return;

        // Check level requirement
        if (currentItem.nivelRequerido > currentCharacter.level)
        {
            Debug.Log($"Cannot equip {currentItem.nomeDoItem} - requires level {currentItem.nivelRequerido}");
            // You could show a message to the player here
            return;
        }

        // Equip item to current character
        bool equipped = currentCharacter.EquipItem(currentItem);

        if (equipped && inventory != null)
        {
            inventory.RemoverItem(currentItem, 1);

            // Find and refresh PartyMenuManager
            PartyMenuManager menuManager = FindFirstObjectByType<PartyMenuManager>();
            if (menuManager != null)
            {
                menuManager.RefreshInventoryDisplay();
                menuManager.UpdateEquipmentDisplay();

                // Update stats display (stats may change from equipment)
                CharacterData currentChar = menuManager.GetCurrentSelectedCharacter();
                if (currentChar != null)
                {
                    // Find and update the stats display
                    PartyMemberStatsDisplay[] displays = FindObjectsOfType<PartyMemberStatsDisplay>();
                    foreach (var display in displays)
                    {
                        // You might need a better way to find the correct display
                        display.UpdateDisplay();
                    }
                }
            }

            // Close if item is gone
            if (currentSlot.quantidade <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnDropClicked()
    {
        if (currentItem == null || currentSlot == null || inventory == null) return;

        // Remove item from inventory
        inventory.RemoverItem(currentItem, 1);

        // Find and refresh PartyMenuManager
        PartyMenuManager menuManager = FindFirstObjectByType<PartyMenuManager>();
        if (menuManager != null)
            menuManager.RefreshInventoryDisplay();

        // Close this details panel
        Destroy(gameObject);
    }

    private void OnCloseClicked()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Remove button listeners
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