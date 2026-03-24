using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CraftingSimples;

public class InterfaceCrafting : MonoBehaviour
{
    public CraftingSimples craftingSystem;
    public SistemaInventario sistemaInventario;

    [Header("UI References")]
    public Transform containerGrid; // Grid for crafting slots
    public GameObject prefabSlot; // Prefab for crafting slots (similar to inventory slots)
    public Button botaoCraftar;

    [Header("Crafting Feedback")]
    public Color corNormal = Color.white;
    public Color corSelecionado = Color.yellow;
    public Color corPodeUsar = Color.green;
    public Color corNaoPodeUsar = Color.red;

    [Header("Selected Items Info")]
    public TextMeshProUGUI textoStatusCraft;
    public TextMeshProUGUI textoResultadoCraft;

    private List<SlotUICrafting> slotsCrafting = new List<SlotUICrafting>();
    private List<SlotInventario> slotsSelecionados = new List<SlotInventario>();
    private CraftingSimples.CraftingRecipe receitaAtual;

    private void Start()
    {
        // Subscribe to inventory change events
        if (sistemaInventario != null)
        {
            sistemaInventario.onInventarioMudou += AtualizarInterface;
        }

        // Setup craft button
        if (botaoCraftar != null)
        {
            // Remove any existing listeners to avoid duplicates
            botaoCraftar.onClick.RemoveAllListeners();
            botaoCraftar.onClick.AddListener(TentarCraftar);
            botaoCraftar.interactable = false;
        }

        // Initial UI update
        AtualizarInterface();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (sistemaInventario != null)
        {
            sistemaInventario.onInventarioMudou -= AtualizarInterface;
        }
    }

    public void AtualizarInterface()
    {
        Debug.Log("Atualizando Interface Crafting");

        // Clear selection when inventory updates
        slotsSelecionados.Clear();
        receitaAtual = null;

        // Clear the grid
        foreach (Transform child in containerGrid)
        {
            Destroy(child.gameObject);
        }

        slotsCrafting.Clear();

        // Create slots for each inventory item
        foreach (SlotInventario slot in sistemaInventario.inventario)
        {
            if (slot != null && slot.dadosDoItem != null && slot.quantidade > 0)
            {
                GameObject novoSlot = Instantiate(prefabSlot, containerGrid);
                SlotUICrafting slotUI = novoSlot.GetComponent<SlotUICrafting>();

                if (slotUI != null)
                {
                    slotUI.ConfigurarSlot(slot);
                    slotsCrafting.Add(slotUI);

                    // Add click listener - IMPORTANT: Use a local variable to avoid closure issues
                    Button button = novoSlot.GetComponent<Button>();
                    if (button != null)
                    {
                        // Remove any existing listeners first
                        button.onClick.RemoveAllListeners();

                        // Store reference to this slotUI for the listener
                        SlotUICrafting currentSlot = slotUI;
                        button.onClick.AddListener(() => {
                            Debug.Log($"Slot clicado: {currentSlot.slotReferencia?.dadosDoItem?.nomeDoItem}");
                            SelecionarSlot(currentSlot);
                        });
                    }
                    else
                    {
                        Debug.LogError("Slot prefab doesn't have a Button component!");
                    }
                }
                else
                {
                    Debug.LogError("Slot prefab doesn't have SlotUICrafting component!");
                }
            }
        }

        // Update craft button state
        VerificarPossibilidadeCraft();
    }

    public void SelecionarSlot(SlotUICrafting slotUI)
    {
        if (slotUI == null)
        {
            Debug.LogError("SlotUI is null!");
            return;
        }

        SlotInventario slotInventario = slotUI.slotReferencia;

        if (slotInventario == null || slotInventario.dadosDoItem == null)
        {
            Debug.LogError("SlotInventario or item is null!");
            return;
        }

        Debug.Log($"Selecionando slot: {slotInventario.dadosDoItem.nomeDoItem}, Quantidade: {slotInventario.quantidade}");

        // Toggle selection
        if (slotsSelecionados.Contains(slotInventario))
        {
            // Deselect
            Debug.Log("Deselecionando item");
            slotsSelecionados.Remove(slotInventario);
            slotUI.MudarCor(corNormal);
        }
        else
        {
            // Select
            Debug.Log("Selecionando item");
            slotsSelecionados.Add(slotInventario);
            slotUI.MudarCor(corSelecionado);
        }

        Debug.Log($"Total itens selecionados: {slotsSelecionados.Count}");

        // Check if selected items match any recipe
        VerificarPossibilidadeCraft();
    }

    private void VerificarPossibilidadeCraft()
    {
        // Reset all non-selected slots to normal first
        foreach (var slot in slotsCrafting)
        {
            if (!slotsSelecionados.Contains(slot.slotReferencia))
            {
                slot.MudarCor(corNormal);
            }
        }

        if (slotsSelecionados.Count == 0)
        {
            // No items selected
            if (botaoCraftar != null)
            {
                botaoCraftar.interactable = false;
                // Reset button color
                Image buttonImage = botaoCraftar.GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = corNormal;
            }

            if (textoStatusCraft != null)
                textoStatusCraft.text = "Selecione itens para craftar";

            if (textoResultadoCraft != null)
                textoResultadoCraft.text = "";

            return;
        }

        // Group selected items by type and count quantities
        Dictionary<DadosItem, int> itensSelecionados = new Dictionary<DadosItem, int>();
        foreach (var slot in slotsSelecionados)
        {
            if (itensSelecionados.ContainsKey(slot.dadosDoItem))
                itensSelecionados[slot.dadosDoItem] += slot.quantidade;
            else
                itensSelecionados[slot.dadosDoItem] = slot.quantidade;
        }

        // Debug log selected items
        string debugLog = "Itens selecionados: ";
        foreach (var item in itensSelecionados)
        {
            debugLog += $"{item.Key.nomeDoItem}({item.Value}) ";
        }
        Debug.Log(debugLog);

        // Check each recipe to see if selected items match requirements
        receitaAtual = null;
        bool receitaEncontrada = false;
        string resultados = "";

        foreach (var recipe in craftingSystem.recipes)
        {
            bool corresponde = true;

            // Check if recipe requires the exact number of item types selected
            if (recipe.requiredItems.Count != itensSelecionados.Count)
            {
                continue; // Different number of item types
            }

            // Check each requirement
            foreach (var requirement in recipe.requiredItems)
            {
                if (!itensSelecionados.ContainsKey(requirement.item) ||
                    itensSelecionados[requirement.item] < requirement.quantidade)
                {
                    corresponde = false;
                    break;
                }
            }

            if (corresponde)
            {
                receitaEncontrada = true;
                receitaAtual = recipe;

                // Build results string
                resultados = "Pode craftar:\n";
                foreach (var result in recipe.results)
                {
                    resultados += $"{result.quantidade}x {result.item.nomeDoItem}\n";
                }
                break;
            }
        }

        // Update UI based on result
        if (botaoCraftar != null)
        {
            Image buttonImage = botaoCraftar.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (receitaEncontrada)
                {
                    buttonImage.color = corPodeUsar;
                    botaoCraftar.interactable = true;

                    if (textoStatusCraft != null)
                        textoStatusCraft.text = "Receita válida!";
                }
                else
                {
                    buttonImage.color = corNaoPodeUsar;
                    botaoCraftar.interactable = false;

                    if (textoStatusCraft != null)
                        textoStatusCraft.text = "Combinação inválida";
                }
            }

            if (textoResultadoCraft != null)
                textoResultadoCraft.text = resultados;
        }

        // Update slot colors to show which items can be used in recipes
        AtualizarCoresItensViaveis(itensSelecionados);
    }

    private void AtualizarCoresItensViaveis(Dictionary<DadosItem, int> itensSelecionados)
    {
        foreach (var slot in slotsCrafting)
        {
            SlotInventario slotInventario = slot.slotReferencia;

            if (slotInventario != null && !slotsSelecionados.Contains(slotInventario))
            {
                // Check if this item could be used in any recipe with current selection
                bool podeUsar = PodeUsarItemComSelecao(slotInventario.dadosDoItem, itensSelecionados);

                if (podeUsar)
                    slot.MudarCor(corPodeUsar);
                else
                    slot.MudarCor(corNaoPodeUsar);
            }
        }
    }

    private bool PodeUsarItemComSelecao(DadosItem item, Dictionary<DadosItem, int> itensSelecionados)
    {
        // Check if adding this item to selection could form a valid recipe
        foreach (var recipe in craftingSystem.recipes)
        {
            // Skip recipes that don't match the count of item types (+1 for potential new item)
            if (recipe.requiredItems.Count != itensSelecionados.Count + 1)
                continue;

            // Check if all currently selected items match this recipe
            bool corresponde = true;
            foreach (var req in recipe.requiredItems)
            {
                if (itensSelecionados.ContainsKey(req.item))
                {
                    if (itensSelecionados[req.item] < req.quantidade)
                    {
                        corresponde = false;
                        break;
                    }
                }
                else if (req.item != item)
                {
                    // This required item is neither selected nor the potential new item
                    corresponde = false;
                    break;
                }
            }

            if (corresponde)
                return true;
        }

        return false;
    }
    public void TentarCraftar()
    {
        if (receitaAtual != null && slotsSelecionados.Count > 0)
        {
            Debug.Log($"Tentando craftar: {receitaAtual.recipeName}");

            // Check if we have all required items
            bool temTodos = true;
            Dictionary<DadosItem, int> totalSelecionado = new Dictionary<DadosItem, int>();

            foreach (var slot in slotsSelecionados)
            {
                if (totalSelecionado.ContainsKey(slot.dadosDoItem))
                    totalSelecionado[slot.dadosDoItem] += slot.quantidade;
                else
                    totalSelecionado[slot.dadosDoItem] = slot.quantidade;
            }

            foreach (var requirement in receitaAtual.requiredItems)
            {
                if (!totalSelecionado.ContainsKey(requirement.item) ||
                    totalSelecionado[requirement.item] < requirement.quantidade)
                {
                    Debug.Log($"Faltando: {requirement.quantidade}x {requirement.item.nomeDoItem}");
                    temTodos = false;
                    break;
                }
            }

            if (temTodos)
            {
                // Store the recipe index to call CraftItem properly
                int recipeIndex = craftingSystem.recipes.IndexOf(receitaAtual);

                // Call the main CraftItem method - THIS WILL ADD THE PROGRESS
                craftingSystem.CraftItem(recipeIndex);

                // Clear selection after crafting
                slotsSelecionados.Clear();
                receitaAtual = null;

                // Refresh the UI to show updated inventory
                AtualizarInterface();

                Debug.Log($"Sucesso! Receita '{receitaAtual?.recipeName}' craftada!");
            }
            else
            {
                Debug.Log("Falha: Itens insuficientes!");
            }
        }
    }
}
