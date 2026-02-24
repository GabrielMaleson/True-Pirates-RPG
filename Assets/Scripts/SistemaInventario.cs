using UnityEngine;
using System.Collections.Generic;
using System;

public class SistemaInventario : MonoBehaviour
{
    public List<SlotInventario> inventario = new List<SlotInventario>();

    [Header("Economy")]
    public int moedas = 0;

    [Header("Party Members")]
    public List<CharacterComponent> partyMembers = new List<CharacterComponent>(); // Changed to CharacterComponent

    [Header("Inventory Settings")]
    public int maxInventorySize = 30;

    // Event that indicates inventory changes
    public event Action onInventarioMudou;

    // Store original CharacterData references
    private Dictionary<CharacterData, CharacterData> originalCharacterData = new Dictionary<CharacterData, CharacterData>();

    private void Start()
    {
        // Store references to original CharacterData for each party member
        foreach (var member in partyMembers)
        {
            if (member != null && member.characterData != null)
            {
                originalCharacterData[member.characterData] = member.characterData;
            }
        }
    }

    public void AdicionarItem(DadosItem itemParaAdicionar, int quantidade)
    {
        if (inventario.Count >= maxInventorySize && !ItemExistsInInventory(itemParaAdicionar))
        {
            Debug.LogWarning("Inventory is full!");
            return;
        }

        // 1. Check if item is stackable
        if (itemParaAdicionar.ehEmpilhavel)
        {
            // 1.1 Check if inventory already has this item type
            for (int i = 0; i < inventario.Count; i++)
            {
                if (inventario[i].dadosDoItem == itemParaAdicionar)
                {
                    inventario[i].AdicionarQuantidade(quantidade);
                    Debug.Log($"Added +{quantidade} to item {itemParaAdicionar.nomeDoItem}");

                    // Notify Unity that inventory has changed
                    onInventarioMudou?.Invoke();
                    return;
                }
            }
        }

        // 2. Non-stackable item or doesn't have one yet
        // Create a new slot
        SlotInventario novoSlot = new SlotInventario(itemParaAdicionar, quantidade);

        // Add slot to inventory
        inventario.Add(novoSlot);

        // Notify Unity that inventory has changed
        onInventarioMudou?.Invoke();
    }

    private bool ItemExistsInInventory(DadosItem item)
    {
        foreach (var slot in inventario)
        {
            if (slot.dadosDoItem == item)
                return true;
        }
        return false;
    }

    public void RemoverItem(DadosItem item, int quantidade)
    {
        // 1. Check if item exists in inventory
        for (int i = 0; i < inventario.Count; i++)
        {
            SlotInventario slot = inventario[i];
            if (slot.dadosDoItem == item)
            {
                // 1.1 Subtract desired quantity
                slot.SubtrairQuantidade(quantidade);
                Debug.Log($"Subtracted -{quantidade} from item {item.nomeDoItem}. Total: {slot.quantidade}");

                // Notify Unity that inventory has changed
                onInventarioMudou?.Invoke();

                if (slot.quantidade <= 0)
                {
                    // Remove item from inventory
                    inventario.RemoveAt(i);
                    Debug.Log($"Slot removed: {item.nomeDoItem}");

                    // Notify Unity that inventory has changed
                    onInventarioMudou?.Invoke();
                }

                return;
            }
        }
    }

    public void ModificadorMoedas(int valor)
    {
        moedas += valor;

        if (moedas < 0)
        {
            moedas = 0;
        }

        // Notify Unity that inventory has changed
        onInventarioMudou?.Invoke();
    }

    public bool TemItem(DadosItem item, int qtd)
    {
        // Check if has required item and quantity in inventory
        foreach (SlotInventario slot in inventario)
        {
            if (slot.dadosDoItem == item && slot.quantidade >= qtd)
            {
                return true;
            }
        }

        return false;
    }

    public List<CharacterData> GetPartyMembersForCombat()
    {
        List<CharacterData> combatParty = new List<CharacterData>();

        foreach (var member in partyMembers)
        {
            if (member != null && member.characterData != null)
            {
                // Create a runtime copy to preserve original data
                CharacterData combatCopy = ScriptableObject.CreateInstance<CharacterData>();

                // Copy all data
                CharacterData source = member.characterData;
                combatCopy.characterName = source.characterName;
                combatCopy.level = source.level;
                combatCopy.currentHP = source.currentHP;
                combatCopy.hp = source.hp;
                combatCopy.attack = source.attack;
                combatCopy.defense = source.defense;
                combatCopy.maxAP = source.maxAP;
                combatCopy.availableAttacks = new List<AttackFile>(source.availableAttacks);
                combatCopy.expValue = source.expValue;

                // Copy equipped items
                combatCopy.equippedItems = new List<DadosItem>(source.equippedItems);

                combatParty.Add(combatCopy);
            }
        }

        return combatParty;
    }

    public void UpdatePartyMembersFromCombat(List<CharacterData> combatPartyMembers)
    {
        for (int i = 0; i < combatPartyMembers.Count && i < partyMembers.Count; i++)
        {
            if (partyMembers[i] != null && partyMembers[i].characterData != null && combatPartyMembers[i] != null)
            {
                // Update the original CharacterData with combat results
                partyMembers[i].characterData.currentHP = combatPartyMembers[i].currentHP;
                partyMembers[i].characterData.level = combatPartyMembers[i].level;
                partyMembers[i].characterData.currentExperience = combatPartyMembers[i].currentExperience;

                // Update stats if level changed
                if (partyMembers[i].characterData.level != combatPartyMembers[i].level)
                {
                    partyMembers[i].characterData.CalculateStatsForLevel();
                }
            }
        }
    }
}