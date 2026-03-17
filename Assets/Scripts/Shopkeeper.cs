using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Yarn.Unity;

[System.Serializable]
public class ShopItem
{
    public DadosItem item;
    public int price;
    public int quantity = 1; // -1 for infinite stock
    public bool isInfinite => quantity == -1;
}

public class Shopkeeper : MonoBehaviour
{
    [Header("Shop Settings")]
    public string shopName = "Shop";
    public List<ShopItem> itemsForSale = new List<ShopItem>();

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.Space;
    public GameObject interactionPopup; // Optional popup to show when player is nearby
    private bool playerInRange = false;

    // Reference to player's movement script to prevent jumping
    private MovimentacaoExploracao playerMovement;

    [Header("UI References")]
    public GameObject shopPanel;
    public Transform itemContainer;
    public GameObject itemButtonPrefab;
    public TextMeshProUGUI playerGoldText;
    public TextMeshProUGUI shopNameText;
    public TextMeshProUGUI itemDetailsNameText;
    public TextMeshProUGUI itemDetailsDescText;
    public TextMeshProUGUI itemDetailsPriceText;
    public Image itemDetailsIcon;
    public Button buyButton;
    public Button closeButton;

    [Header("Messages")]
    public string insufficientGoldMessage = "Not enough gold!";
    public string outOfStockMessage = "Out of stock!";
    public string purchasedMessage = "Purchased {0}!";

    private SistemaInventario playerInventory;
    private ShopItem selectedItem;
    private Dictionary<ShopItem, Button> itemButtons = new Dictionary<ShopItem, Button>();
    private bool isShopOpen = false;

    private void Start()
    {
        // Find player inventory
        playerInventory = FindFirstObjectByType<SistemaInventario>();

        // Set up UI
        if (shopNameText != null)
            shopNameText.text = shopName;

        if (buyButton != null)
            buyButton.onClick.AddListener(TryBuySelectedItem);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        // Hide shop panel initially
        if (shopPanel != null)
            shopPanel.SetActive(false);

        // Hide item details initially
        ClearItemDetails();

        // Hide interaction popup initially
        if (interactionPopup != null)
            interactionPopup.SetActive(false);
    }

    private void Update()
    {
        // Check for input while player is in range and shop is not open
        if (playerInRange && !isShopOpen && Input.GetKeyDown(interactKey))
        {
            // Disable player jumping temporarily while shop is open
            if (playerMovement != null)
                playerMovement.SetCanJump(false);

            OpenShop();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            // Get reference to player's movement script
            if (playerMovement == null)
                playerMovement = collision.GetComponent<MovimentacaoExploracao>();

            ShowPopup();
            Debug.Log($"Press {interactKey} to open shop");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;

            // Clear reference
            playerMovement = null;

            HidePopup();

            // Close shop if it's open when player leaves
            if (isShopOpen)
            {
                CloseShop();
            }
        }
    }

    private void OnTriggerEnter(Collider collision) // For 3D colliders
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            // Get reference to player's movement script
            if (playerMovement == null)
                playerMovement = collision.GetComponent<MovimentacaoExploracao>();

            ShowPopup();
            Debug.Log($"Press {interactKey} to open shop");
        }
    }

    private void OnTriggerExit(Collider collision) // For 3D colliders
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;

            // Clear reference
            playerMovement = null;

            HidePopup();

            if (isShopOpen)
            {
                CloseShop();
            }
        }
    }

    private void ShowPopup()
    {
        if (interactionPopup != null)
        {
            interactionPopup.SetActive(true);

            // Optional: Update popup text with key
            TextMeshProUGUI popupText = interactionPopup.GetComponentInChildren<TextMeshProUGUI>();
            if (popupText != null)
            {
                popupText.text = $"Press {interactKey} to shop";
            }
        }
    }

    private void HidePopup()
    {
        if (interactionPopup != null)
        {
            interactionPopup.SetActive(false);
        }
    }

    private void ClearItemDetails()
    {
        if (itemDetailsNameText != null)
            itemDetailsNameText.text = "";
        if (itemDetailsDescText != null)
            itemDetailsDescText.text = "";
        if (itemDetailsPriceText != null)
            itemDetailsPriceText.text = "";
        if (itemDetailsIcon != null)
            itemDetailsIcon.gameObject.SetActive(false);
        if (buyButton != null)
            buyButton.interactable = false;
    }

    private void UpdateItemDetails(ShopItem shopItem)
    {
        if (shopItem == null || shopItem.item == null) return;

        if (itemDetailsNameText != null)
            itemDetailsNameText.text = shopItem.item.nomeDoItem;

        if (itemDetailsDescText != null)
            itemDetailsDescText.text = shopItem.item.descricao;

        if (itemDetailsPriceText != null)
        {
            string stockInfo = shopItem.isInfinite ? "Infinite" : $"Stock: {shopItem.quantity}";
            itemDetailsPriceText.text = $"Price: {shopItem.price} gold\n{stockInfo}";
        }

        if (itemDetailsIcon != null && shopItem.item.icone != null)
        {
            itemDetailsIcon.sprite = shopItem.item.icone;
            itemDetailsIcon.gameObject.SetActive(true);
        }

        // Check if buy button should be interactable
        if (buyButton != null)
        {
            bool canBuy = playerInventory != null &&
                          playerInventory.moedas >= shopItem.price &&
                          (shopItem.isInfinite || shopItem.quantity > 0);
            buyButton.interactable = canBuy;
        }
    }

    private void TryBuySelectedItem()
    {
        if (selectedItem == null || playerInventory == null) return;

        // Check if in stock
        if (!selectedItem.isInfinite && selectedItem.quantity <= 0)
        {
            Debug.Log(outOfStockMessage);
            return;
        }

        // Check if player has enough gold
        if (playerInventory.moedas < selectedItem.price)
        {
            Debug.Log(insufficientGoldMessage);
            return;
        }

        // Complete the purchase
        playerInventory.ModificadorMoedas(-selectedItem.price);
        playerInventory.AdicionarItem(selectedItem.item, 1);

        // Update quantity if not infinite
        if (!selectedItem.isInfinite)
        {
            selectedItem.quantity--;

            // If out of stock, disable the button
            if (selectedItem.quantity <= 0)
            {
                if (itemButtons.ContainsKey(selectedItem) && itemButtons[selectedItem] != null)
                {
                    itemButtons[selectedItem].interactable = false;

                    // Update button text to show "Sold Out"
                    TextMeshProUGUI btnText = itemButtons[selectedItem].GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                        btnText.text = $"{selectedItem.item.nomeDoItem}\n(Sold Out)";
                }
            }
        }

        // Update UI
        UpdatePlayerGold();
        UpdateItemDetails(selectedItem); // Refresh price/stock info

        Debug.Log(string.Format(purchasedMessage, selectedItem.item.nomeDoItem));
    }

    private void UpdatePlayerGold()
    {
        if (playerGoldText != null && playerInventory != null)
        {
            playerGoldText.text = $"Gold: {playerInventory.moedas}";
        }
    }

    private void RefreshItemList()
    {
        // Clear existing buttons
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        itemButtons.Clear();

        // Create buttons for each item
        foreach (var shopItem in itemsForSale)
        {
            if (shopItem.item == null) continue;

            GameObject buttonObj = Instantiate(itemButtonPrefab, itemContainer);
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Button button = buttonObj.GetComponent<Button>();

            // Set button text
            if (buttonText != null)
            {
                string priceText = $"{shopItem.price}G";
                string stockText = shopItem.isInfinite ? "∞" : $"{shopItem.quantity}";
                buttonText.text = $"{shopItem.item.nomeDoItem}\n{priceText} | Stock: {stockText}";
            }

            // Set button interactable based on stock
            bool hasStock = shopItem.isInfinite || shopItem.quantity > 0;
            button.interactable = hasStock;

            // Add listener
            ShopItem capturedItem = shopItem;
            button.onClick.AddListener(() => SelectItem(capturedItem));

            // Store reference
            itemButtons[shopItem] = button;
        }
    }

    private void SelectItem(ShopItem shopItem)
    {
        selectedItem = shopItem;
        UpdateItemDetails(shopItem);
    }

    // Public method to open shop
    public void OpenShop()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<SistemaInventario>();

        if (playerInventory == null)
        {
            Debug.LogError("No player inventory found!");
            return;
        }

        isShopOpen = true;

        // Refresh UI
        RefreshItemList();
        UpdatePlayerGold();
        ClearItemDetails();

        // Show shop panel
        if (shopPanel != null)
            shopPanel.SetActive(true);

        // Hide popup while shop is open
        HidePopup();
    }

    // Public method to close shop
    public void CloseShop()
    {
        isShopOpen = false;
        selectedItem = null;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        // Show popup again if player is still in range
        if (playerInRange)
        {
            ShowPopup();
        }

        // Re-enable player jumping when shop closes
        if (playerMovement != null)
            playerMovement.SetCanJump(true);
    }

    // Yarn command to open shop
    [YarnCommand("openshop")]
    public void OpenShopYarn()
    {
        OpenShop();
    }

    // Check if shop is open (for other scripts)
    public bool IsShopOpen()
    {
        return isShopOpen;
    }

    // Check if player is in range (for other scripts)
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }
}