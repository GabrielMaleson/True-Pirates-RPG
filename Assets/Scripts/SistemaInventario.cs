using UnityEngine;
using System.Collections.Generic;
using System;

public class SistemaInventario : MonoBehaviour
{
    public static SistemaInventario Instance { get; private set; }

    [Header("Party Members")]
    public List<PartyMemberState> partyMembers = new List<PartyMemberState>(); // Runtime states

    [Header("Inventory")]
    public List<SlotInventario> inventario = new List<SlotInventario>();
    public int maxInventorySize = 30;

    [Header("Economy")]
    public int moedas = 0;

    [Header("Game Progress")]
    public List<string> gameProgress = new List<string>();

    // Events
    public event Action onInventarioMudou;
    public event Action onPartyUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.tag = "Inventory";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize party members if needed
        InitializePartyMembers();
    }

    private void InitializePartyMembers()
    {
        foreach (var member in partyMembers)
        {
            if (member != null && member.currentHP == 0)
            {
                member.currentHP = member.MaxHP;
                member.currentAP = member.MaxAP;
            }
        }
    }

    // Inventory Methods
    public void AdicionarItem(DadosItem itemParaAdicionar, int quantidade)
    {
        if (inventario.Count >= maxInventorySize && !ItemExistsInInventory(itemParaAdicionar))
        {
            Debug.LogWarning("Inventory is full!");
            return;
        }

        if (itemParaAdicionar.ehEmpilhavel)
        {
            for (int i = 0; i < inventario.Count; i++)
            {
                if (inventario[i].dadosDoItem == itemParaAdicionar)
                {
                    inventario[i].AdicionarQuantidade(quantidade);
                    onInventarioMudou?.Invoke();
                    return;
                }
            }
        }

        SlotInventario novoSlot = new SlotInventario(itemParaAdicionar, quantidade);
        inventario.Add(novoSlot);
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
        for (int i = 0; i < inventario.Count; i++)
        {
            SlotInventario slot = inventario[i];
            if (slot.dadosDoItem == item)
            {
                slot.SubtrairQuantidade(quantidade);
                onInventarioMudou?.Invoke();

                if (slot.quantidade <= 0)
                {
                    inventario.RemoveAt(i);
                    onInventarioMudou?.Invoke();
                }
                return;
            }
        }
    }

    public void ModificadorMoedas(int valor)
    {
        moedas += valor;
        if (moedas < 0) moedas = 0;
        onInventarioMudou?.Invoke();
    }

    public bool TemItem(DadosItem item, int qtd)
    {
        foreach (SlotInventario slot in inventario)
        {
            if (slot.dadosDoItem == item && slot.quantidade >= qtd)
                return true;
        }
        return false;
    }

    // Party Member Methods
    public List<PartyMemberState> GetPartyMembersForCombat()
    {
        // Return a copy of the list (references are fine, we want to modify the same objects)
        return new List<PartyMemberState>(partyMembers);
    }

    public void UpdatePartyMembersFromCombat(List<PartyMemberState> combatPartyMembers)
    {
        // The references are the same, so no need to copy back
        // But we can trigger an update event
        onPartyUpdated?.Invoke();
    }

    public void HealPartyFull()
    {
        foreach (var member in partyMembers)
        {
            if (member != null)
            {
                member.currentHP = member.MaxHP;
                member.currentAP = member.MaxAP;
            }
        }
        onPartyUpdated?.Invoke();
    }

    public void ReviveParty(float percentage)
    {
        foreach (var member in partyMembers)
        {
            if (member != null && member.currentHP <= 0)
            {
                member.currentHP = Mathf.RoundToInt(member.MaxHP * percentage);
                member.currentAP = member.MaxAP;
            }
        }
        onPartyUpdated?.Invoke();
    }

    // Equipment Methods
    public bool EquipWeapon(int partyIndex, DadosItem weaponItem)
    {
        if (partyIndex < 0 || partyIndex >= partyMembers.Count) return false;

        var member = partyMembers[partyIndex];
        if (member == null) return false;

        // Check if item exists in inventory
        if (!TemItem(weaponItem, 1)) return false;

        // Unequip current weapon and add back to inventory
        if (member.weapon != null)
        {
            AdicionarItem(member.weapon, 1);
        }

        // Equip new weapon
        if (member.EquipWeapon(weaponItem))
        {
            RemoverItem(weaponItem, 1);
            onPartyUpdated?.Invoke();
            return true;
        }

        return false;
    }

    public bool EquipArmor(int partyIndex, DadosItem armorItem)
    {
        if (partyIndex < 0 || partyIndex >= partyMembers.Count) return false;

        var member = partyMembers[partyIndex];
        if (member == null) return false;

        if (!TemItem(armorItem, 1)) return false;

        if (member.armor != null)
        {
            AdicionarItem(member.armor, 1);
        }

        if (member.EquipArmor(armorItem))
        {
            RemoverItem(armorItem, 1);
            onPartyUpdated?.Invoke();
            return true;
        }

        return false;
    }

    public void UnequipWeapon(int partyIndex)
    {
        if (partyIndex < 0 || partyIndex >= partyMembers.Count) return;

        var member = partyMembers[partyIndex];
        if (member == null || member.weapon == null) return;

        AdicionarItem(member.weapon, 1);
        member.UnequipWeapon();
        onPartyUpdated?.Invoke();
    }

    public void UnequipArmor(int partyIndex)
    {
        if (partyIndex < 0 || partyIndex >= partyMembers.Count) return;

        var member = partyMembers[partyIndex];
        if (member == null || member.armor == null) return;

        AdicionarItem(member.armor, 1);
        member.UnequipArmor();
        onPartyUpdated?.Invoke();
    }

    // Progress Methods
    public void AddProgress(string thing)
    {
        if (!gameProgress.Contains(thing))
        {
            gameProgress.Add(thing);
        }
    }

    public void RemoveProgress(string thing)
    {
        gameProgress.Remove(thing);
    }

    public bool HasProgress(string thing)
    {
        return gameProgress.Contains(thing);
    }

    public List<string> GetGameProgress()
    {
        return new List<string>(gameProgress);
    }

    private void OnValidate()
    {
        if (Application.isPlaying && onInventarioMudou != null)
        {
            onInventarioMudou.Invoke();
        }
    }
}