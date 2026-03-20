using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;

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

    [Header("Pickup Notification")]
    public GameObject pickupNotificationPrefab; // Prefab with ControladorTextoDano script
    public Canvas targetCanvas; // The canvas to spawn notifications on
    public float notificationOffsetY = -50f; // Offset from top (negative to go down from top)

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

        // Find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
                Debug.LogError("No Canvas found in scene! Pickup notifications will not appear.");
        }
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

    // Show pickup notification on canvas
    private void ShowPickupNotification(DadosItem item, int quantidade)
    {
        if (pickupNotificationPrefab == null || targetCanvas == null) return;

        // Instantiate notification as child of canvas
        GameObject notification = Instantiate(pickupNotificationPrefab, targetCanvas.transform);

        // Position at top center of screen with offset
        RectTransform rectTransform = notification.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Position at top center of canvas
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, notificationOffsetY);
            rectTransform.anchoredPosition = new Vector2(0, notificationOffsetY);
        }

        // Set text
        TextMeshProUGUI textComponent = notification.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            string itemName = item != null ? item.nomeDoItem : "Item";
            string quantityText = quantidade > 1 ? $" x{quantidade}" : "";
            textComponent.text = $"{itemName}{quantityText} obtido!";
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

                    // Show pickup notification
                    ShowPickupNotification(itemParaAdicionar, quantidade);

                    onInventarioMudou?.Invoke();
                    return;
                }
            }
        }

        SlotInventario novoSlot = new SlotInventario(itemParaAdicionar, quantidade);
        inventario.Add(novoSlot);

        // Show pickup notification
        ShowPickupNotification(itemParaAdicionar, quantidade);

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
        return new List<PartyMemberState>(partyMembers);
    }

    public void UpdatePartyMembersFromCombat(List<PartyMemberState> combatPartyMembers)
    {
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

        if (!TemItem(weaponItem, 1)) return false;

        if (member.weapon != null)
        {
            AdicionarItem(member.weapon, 1);
        }

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
