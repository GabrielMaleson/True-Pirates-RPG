using UnityEngine;
using System.Collections.Generic;

public class CraftingSimples : MonoBehaviour
{
    public SistemaInventario inventario;

    [Header("Crafting Recipes")]
    public List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    [System.Serializable]
    public class CraftingRecipe
    {
        [Header("Recipe Name")]
        public string recipeName;

        [Header("Required Items")]
        public List<ItemRequirement> requiredItems = new List<ItemRequirement>();

        [Header("Results")]
        public List<ItemResult> results = new List<ItemResult>();
    }

    [System.Serializable]
    public class ItemRequirement
    {
        public DadosItem item;
        public int quantidade;
    }

    [System.Serializable]
    public class ItemResult
    {
        public DadosItem item;
        public int quantidade;
    }

    private void Start()
    {
        // Auto-find inventory if not assigned
        if (inventario == null)
            inventario = SistemaInventario.Instance;
    }

    public void CraftItem(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipes.Count)
        {
            Debug.LogError("Índice de receita inválido!");
            return;
        }

        SistemaInventario inv = SistemaInventario.Instance;
        if (inv == null)
        {
            Debug.LogError("SistemaInventario.Instance não encontrado!");
            return;
        }

        CraftingRecipe recipe = recipes[recipeIndex];

        // Check if player has all required items
        bool hasAllItems = true;
        foreach (ItemRequirement requirement in recipe.requiredItems)
        {
            if (!inv.TemItem(requirement.item, requirement.quantidade))
            {
                hasAllItems = false;
                Debug.Log($"Falta: {requirement.quantidade}x {requirement.item.nomeDoItem}");
                break;
            }
        }

        // If player has all items, craft
        if (hasAllItems)
        {
            // Remove required items
            foreach (ItemRequirement requirement in recipe.requiredItems)
            {
                inv.RemoverItem(requirement.item, requirement.quantidade);
            }

            // Add crafted items
            foreach (ItemResult result in recipe.results)
            {
                inv.AdicionarItem(result.item, result.quantidade);
                Debug.Log($"Criado: {result.quantidade}x {result.item.nomeDoItem}");
            }

            SFXManager.Instance?.Play(SFXManager.Instance.pieceCraftFound);
            inv.AddProgress("compass");
            Debug.Log("Progress 'compass' adicionado com sucesso!");
            Debug.Log($"Sucesso! Receita '{recipe.recipeName}' craftada com sucesso!");
        }
        else
        {
            SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
            Debug.Log("Falha: Você não tem os itens necessários!");
        }
    }

    // Helper method to check if a specific recipe can be crafted
    public bool PodeCraftar(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= recipes.Count)
            return false;

        // Ensure we have inventory reference
        if (inventario == null)
            inventario = SistemaInventario.Instance;

        if (inventario == null)
            return false;

        CraftingRecipe recipe = recipes[recipeIndex];

        foreach (ItemRequirement requirement in recipe.requiredItems)
        {
            if (!inventario.TemItem(requirement.item, requirement.quantidade))
            {
                return false;
            }
        }

        return true;
    }
}