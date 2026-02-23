using UnityEngine;
using System.Collections.Generic;
using System;

public class SistemaInventario : MonoBehaviour
{
    public List<SlotInventario> inventario = new List<SlotInventario>();

    [Header("Economy")]
    public int moedas = 0;

    [Header("Party Members")]
    public List<CharacterData> partyMembers = new List<CharacterData>();

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
            if (member != null)
            {
                originalCharacterData[member] = member;
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
            if (member != null)
            {
                // Create a runtime copy to preserve original data
                CharacterData combatCopy = ScriptableObject.CreateInstance<CharacterData>();

                // Copy all data
                combatCopy.characterName = member.characterName;
                combatCopy.level = member.level;
                combatCopy.currentHP = member.currentHP;
                combatCopy.hp = member.hp;
                combatCopy.attack = member.attack;
                combatCopy.defense = member.defense;
                combatCopy.maxAP = member.maxAP;
                combatCopy.availableAttacks = new List<AttackFile>(member.availableAttacks);
                combatCopy.expValue = member.expValue;

                // Copy equipped items
                combatCopy.equippedItems = new List<DadosItem>(member.equippedItems);

                combatParty.Add(combatCopy);
            }
        }

        return combatParty;
    }

    public void UpdatePartyMembersFromCombat(List<CharacterData> combatPartyMembers)
    {
        for (int i = 0; i < combatPartyMembers.Count && i < partyMembers.Count; i++)
        {
            if (partyMembers[i] != null && combatPartyMembers[i] != null)
            {
                // Update the original CharacterData with combat results
                partyMembers[i].currentHP = combatPartyMembers[i].currentHP;
                partyMembers[i].level = combatPartyMembers[i].level;
                partyMembers[i].currentExperience = combatPartyMembers[i].currentExperience;

                // Update stats if level changed
                if (partyMembers[i].level != combatPartyMembers[i].level)
                {
                    partyMembers[i].CalculateStatsForLevel();
                }
            }
        }
    }

    // When Unity editor changes, update inventory display
    private void OnValidate()
    {
        if (Application.isPlaying && onInventarioMudou != null)
        {
            onInventarioMudou.Invoke();
        }
    }
}