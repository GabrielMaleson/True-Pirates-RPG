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
    public int quantity = 1; // -1 para estoque infinito
    public bool isInfinite => quantity == -1;
}

public class Shopkeeper : MonoBehaviour
{
    [Header("Itens à Venda")]
    public List<ShopItem> itemsForSale = new List<ShopItem>();

    // ── Painel da Loja ────────────────────────────────────────────────────────

    [Header("Painel da Loja")]
    public GameObject shopPanel;           // Root "New Shop"
    public Transform slotGrid;            // ItemSelectorGrid
    public GameObject slotPrefab;         // Slot Shop prefab (tem Image para ícone + Button)
    public Button shopBuyButton;          // Botão "BOTAO COMPRAR"
    public GameObject shopInteractionPopup; // Popup de proximidade

    // ── Painel de Info do Item Selecionado ────────────────────────────────────

    [Header("Info do Item Selecionado (preenchido automaticamente)")]
    public Image         selectedIcon;        // Ícone central
    public TextMeshProUGUI selectedPrice;     // "Preço: 9999"
    public TextMeshProUGUI selectedType;      // "Tipo: arma"
    public TextMeshProUGUI selectedShortDesc; // Descrição mecânica curta — topo direito
    public TextMeshProUGUI selectedLongDesc;  // Descrição narrativa — bloco central direito

    // ── Estado Interno ────────────────────────────────────────────────────────

    private SistemaInventario playerInventory;
    private ShopItem selectedItem;
    private bool playerInRange = false;
    private bool isShopOpen    = false;

    private static readonly string[] shopkeeperQuotes =
    {
        "Bem-vindo, piratas! Temos tudo que um saqueador de respeito precisa!",
        "Não vendo fiado. Já perdi um navio assim.",
        "Qualidade pirata garantida... ou quase isso.",
        "Se não tiver aqui, não existe. Se existe, está aqui.",
        "Compre agora, arrependa-se depois — esse é o lema!",
        "Já cansei de ver pirata bravo comigo por não comprar escudo. Compra o escudo.",
        "Esse item aí foi parar aqui depois de muita aventura. Tô vendendo barato.",
    };

    private void Start()
    {
        playerInventory = SistemaInventario.Instance ?? FindFirstObjectByType<SistemaInventario>();

        if (shopBuyButton != null)
            shopBuyButton.onClick.AddListener(TryBuySelectedItem);

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (shopInteractionPopup != null)
            shopInteractionPopup.SetActive(false);

        ClearSelectedInfo();
    }

    private void Update()
    {
        if (playerInRange && !isShopOpen && Input.GetKeyDown(KeyCode.Space))
            OpenShop();
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        playerInRange = true;
        if (shopInteractionPopup != null) shopInteractionPopup.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        playerInRange = false;
        if (shopInteractionPopup != null) shopInteractionPopup.SetActive(false);
        if (isShopOpen) CloseShop();
    }

    // ── Abrir / Fechar ────────────────────────────────────────────────────────

    public void OpenShop()
    {
        if (playerInventory == null)
            playerInventory = SistemaInventario.Instance ?? FindFirstObjectByType<SistemaInventario>();

        isShopOpen = true;
        MovimentacaoExploracao.StopForDialogue();

        RefreshSlotGrid();
        ClearSelectedInfo();

        if (shopPanel != null) shopPanel.SetActive(true);
        if (shopInteractionPopup != null) shopInteractionPopup.SetActive(false);
    }

    public void CloseShop()
    {
        isShopOpen  = false;
        selectedItem = null;

        if (shopPanel != null) shopPanel.SetActive(false);

        MovimentacaoExploracao.ResumeFromDialogue();

        if (playerInRange && shopInteractionPopup != null)
            shopInteractionPopup.SetActive(true);
    }

    [YarnCommand("openshop")]
    public void OpenShopYarn() => OpenShop();

    // ── Grade de Slots ────────────────────────────────────────────────────────

    private void RefreshSlotGrid()
    {
        if (slotGrid == null || slotPrefab == null) return;

        foreach (Transform child in slotGrid)
            Destroy(child.gameObject);

        foreach (var shopItem in itemsForSale)
        {
            if (shopItem.item == null) continue;

            GameObject slotObj = Instantiate(slotPrefab, slotGrid);

            // Nome do item no slot (TMP, se houver)
            TextMeshProUGUI slotName = slotObj.GetComponentInChildren<TextMeshProUGUI>();
            if (slotName != null)
                slotName.text = shopItem.item.nomeDoItem;

            bool hasStock = shopItem.isInfinite || shopItem.quantity > 0;

            // Botão — procura em filhos também, caso não esteja na raiz
            Button btn = slotObj.GetComponentInChildren<Button>();
            if (btn == null) btn = slotObj.AddComponent<Button>();
            btn.interactable = hasStock;

            ShopItem captured = shopItem;
            btn.onClick.AddListener(() => SelectItem(captured));
        }
    }

    // ── Seleção ───────────────────────────────────────────────────────────────

    private void SelectItem(ShopItem shopItem)
    {
        selectedItem = shopItem;
        FillSelectedInfo(shopItem);
    }

    private void FillSelectedInfo(ShopItem shopItem)
    {
        if (shopItem?.item == null) return;

        DadosItem item = shopItem.item;

        if (selectedIcon != null)
        {
            selectedIcon.sprite = item.icone;
            selectedIcon.preserveAspect = true;
            selectedIcon.gameObject.SetActive(item.icone != null);
        }

        if (selectedPrice     != null) selectedPrice.text     = $"Preço: {shopItem.price}";
        if (selectedType      != null) selectedType.text      = $"Tipo: {GetItemTypeLabel(item)}";
        if (selectedShortDesc != null) selectedShortDesc.text = item.descricao;           // ex: "Aumenta dano em +1"
        if (selectedLongDesc  != null) selectedLongDesc.text  = item.descricaoNarrativa; // texto narrativo

        bool canBuy = playerInventory != null
                   && playerInventory.moedas >= shopItem.price
                   && (shopItem.isInfinite || shopItem.quantity > 0);
        SetBuyButtonState(canBuy);
    }

    private void ClearSelectedInfo()
    {
        if (selectedIcon      != null) selectedIcon.gameObject.SetActive(false);
        if (selectedPrice     != null) selectedPrice.text     = "Preço:";
        if (selectedType      != null) selectedType.text      = "Tipo:";
        if (selectedShortDesc != null) selectedShortDesc.text = "Informações básicas";
        if (selectedLongDesc  != null) selectedLongDesc.text  = shopkeeperQuotes[Random.Range(0, shopkeeperQuotes.Length)];
        SetBuyButtonState(false);
    }

    private void SetBuyButtonState(bool enabled)
    {
        if (shopBuyButton == null) return;
        shopBuyButton.interactable = enabled;
        CanvasGroup cg = shopBuyButton.GetComponent<CanvasGroup>();
        if (cg == null) cg = shopBuyButton.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = enabled ? 1f : 0.35f;
    }

    private string GetItemTypeLabel(DadosItem item)
    {
        if (item.ehEquipavel)
            return item.slotEquipamento == EquipmentSlot.Arma ? "arma" : "armadura";
        if (item.ehConsumivel)
            return "consumível";
        return "item";
    }

    // ── Compra ────────────────────────────────────────────────────────────────

    private void TryBuySelectedItem()
    {
        if (selectedItem == null || playerInventory == null) return;

        if (!selectedItem.isInfinite && selectedItem.quantity <= 0)
        {
            Debug.Log("[Shopkeeper] Item sem estoque.");
            return;
        }

        if (playerInventory.moedas < selectedItem.price)
        {
            Debug.Log("[Shopkeeper] Ouro insuficiente.");
            return;
        }

        playerInventory.ModificadorMoedas(-selectedItem.price);
        playerInventory.AdicionarItem(selectedItem.item, 1);
        SFXManager.Instance?.Play(SFXManager.Instance.successAcquired);

        if (!selectedItem.isInfinite)
        {
            selectedItem.quantity--;
            if (selectedItem.quantity <= 0)
                RefreshSlotGrid();
        }

        FillSelectedInfo(selectedItem);
        Debug.Log($"[Shopkeeper] {selectedItem.item.nomeDoItem} comprado.");
    }

    public bool IsShopOpen()      => isShopOpen;
    public bool IsPlayerInRange() => playerInRange;
}
