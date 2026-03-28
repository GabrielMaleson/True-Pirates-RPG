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

    /// <summary>
    /// Adds a new party member to the party
    /// </summary>
    /// <param name="characterData">The CharacterData template for the new member</param>
    /// <param name="level">The starting level (default 1)</param>
    /// <returns>True if successfully added, false if already in party</returns>
    public bool AddPartyMember(CharacterData characterData, int level = 1)
    {
        if (characterData == null)
        {
            Debug.LogError("Cannot add party member: CharacterData is null!");
            return false;
        }

        // Check if already in party
        foreach (var member in partyMembers)
        {
            if (member.template == characterData)
            {
                Debug.LogWarning($"Character {characterData.characterName} is already in the party!");
                return false;
            }
        }

        // Create new party member state
        PartyMemberState newMember = new PartyMemberState(characterData, level);

        // Ensure HP and AP are at max
        newMember.currentHP = newMember.MaxHP;
        newMember.currentAP = newMember.MaxAP;
        newMember.currentExperience = 0;

        // Add to party list
        partyMembers.Add(newMember);

        // Show notification
        ShowPartyMemberAddedNotification(characterData.characterName);

        // Trigger event
        onPartyUpdated?.Invoke();

        Debug.Log($"Added {characterData.characterName} (Level {level}) to the party!");
        return true;
    }

    /// <summary>
    /// Adds a new party member by name (looks up CharacterData by name)
    /// </summary>
    /// <param name="characterName">Name of the character to add</param>
    /// <param name="level">Starting level (default 1)</param>
    /// <returns>True if successfully added</returns>
    public bool AddPartyMemberByName(string characterName, int level = 1)
    {
        // Try to find CharacterData by name from Resources
        CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("");
        foreach (var characterData in allCharacters)
        {
            if (characterData.characterName == characterName)
            {
                return AddPartyMember(characterData, level);
            }
        }

        Debug.LogError($"Character '{characterName}' not found in Resources!");
        return false;
    }

    /// <summary>
    /// Shows a notification when a new party member joins
    /// </summary>
    private void ShowPartyMemberAddedNotification(string characterName)
    {
        if (pickupNotificationPrefab == null || targetCanvas == null) return;

        GameObject notification = Instantiate(pickupNotificationPrefab, targetCanvas.transform);

        // Position at top center
        RectTransform rectTransform = notification.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, notificationOffsetY);
            rectTransform.anchoredPosition = new Vector2(0, notificationOffsetY);
        }

        TextMeshProUGUI textComponent = notification.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = $"{characterName} entrou na equipe!";
        }
    }

    /// <summary>
    /// Removes a party member from the party
    /// </summary>
    /// <param name="member">The PartyMemberState to remove</param>
    /// <returns>True if successfully removed</returns>
    public bool RemovePartyMember(PartyMemberState member)
    {
        if (member == null) return false;

        if (partyMembers.Contains(member))
        {
            partyMembers.Remove(member);
            onPartyUpdated?.Invoke();
            Debug.Log($"Removed {member.CharacterName} from the party!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a party member by index
    /// </summary>
    /// <param name="index">Index in party list</param>
    /// <returns>True if successfully removed</returns>
    public bool RemovePartyMemberAt(int index)
    {
        if (index < 0 || index >= partyMembers.Count) return false;

        string memberName = partyMembers[index].CharacterName;
        partyMembers.RemoveAt(index);
        onPartyUpdated?.Invoke();
        Debug.Log($"Removed {memberName} from the party!");
        return true;
    }

    /// <summary>
    /// Gets the current party size
    /// </summary>
    public int GetPartySize()
    {
        return partyMembers.Count;
    }

    /// <summary>
    /// Checks if a character is in the party
    /// </summary>
    public bool IsInParty(CharacterData characterData)
    {
        foreach (var member in partyMembers)
        {
            if (member.template == characterData)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a party member by index
    /// </summary>
    public PartyMemberState GetPartyMember(int index)
    {
        if (index >= 0 && index < partyMembers.Count)
            return partyMembers[index];
        return null;
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
        if (inventario.Count >= maxInventorySize)
        {
            Debug.LogWarning("Inventário cheio!");
            return;
        }

        // Each item always gets its own slot — no stacking
        for (int i = 0; i < quantidade; i++)
        {
            if (inventario.Count >= maxInventorySize) break;
            inventario.Add(new SlotInventario(itemParaAdicionar, 1));
        }

        ShowPickupNotification(itemParaAdicionar, quantidade);
        onInventarioMudou?.Invoke();
    }

    // Removes one slot whose item matches. Each slot is a single item, so this removes that instance.
    public void RemoverItem(DadosItem item, int quantidade = 1)
    {
        for (int removed = 0; removed < quantidade; removed++)
        {
            for (int i = 0; i < inventario.Count; i++)
            {
                if (inventario[i].dadosDoItem == item)
                {
                    inventario.RemoveAt(i);
                    break;
                }
            }
        }
        onInventarioMudou?.Invoke();
    }

    // Removes a specific slot instance (used when the caller already has a reference)
    public void RemoverSlot(SlotInventario slot)
    {
        if (inventario.Remove(slot))
            onInventarioMudou?.Invoke();
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

    // ── Equipment Methods ─────────────────────────────────────────────────────

    // Finds the inventory slot that holds a specific item
    public SlotInventario FindSlotForItem(DadosItem item)
    {
        foreach (var slot in inventario)
            if (slot.dadosDoItem == item) return slot;
        return null;
    }

    // Equips an inventory slot to a party member.
    // Items stay in inventory — equipping only assigns ownership.
    public void EquipItemToMember(SlotInventario slot, PartyMemberState member)
    {
        if (slot?.dadosDoItem == null || member == null || !slot.dadosDoItem.ehEquipavel) return;

        DadosItem item = slot.dadosDoItem;

        // Unassign from the previous owner if switching characters
        if (slot.equippedTo != null && slot.equippedTo != member)
        {
            PartyMemberState prev = slot.equippedTo;
            if (item.slotEquipamento == EquipmentSlot.Acessorio) prev.UnequipAccessory();
            else prev.UnequipArmor();
        }

        // If the target already has something else in that slot, clear the old slot's assignment
        DadosItem displaced = item.slotEquipamento == EquipmentSlot.Acessorio ? member.accessory : member.armor;
        if (displaced != null && displaced != item)
        {
            SlotInventario oldSlot = FindSlotForItem(displaced);
            if (oldSlot != null) oldSlot.equippedTo = null;
        }

        slot.equippedTo = member;
        if (item.slotEquipamento == EquipmentSlot.Acessorio) member.EquipAccessory(item);
        else member.EquipArmor(item);

        onPartyUpdated?.Invoke();
    }

    // Removes the equipped assignment from a slot without touching inventory
    public void UnequipItemFromSlot(SlotInventario slot)
    {
        if (slot == null || slot.equippedTo == null) return;

        PartyMemberState member = slot.equippedTo;
        if (slot.dadosDoItem.slotEquipamento == EquipmentSlot.Acessorio) member.UnequipAccessory();
        else member.UnequipArmor();

        slot.equippedTo = null;
        onPartyUpdated?.Invoke();
    }

    // Restores equippedTo references after loading from save
    public void RestoreEquippedSlots()
    {
        foreach (var member in partyMembers)
        {
            if (member == null) continue;
            if (member.accessory != null)
            {
                SlotInventario slot = FindSlotForItem(member.accessory);
                if (slot != null) slot.equippedTo = member;
            }
            if (member.armor != null)
            {
                SlotInventario slot = FindSlotForItem(member.armor);
                if (slot != null) slot.equippedTo = member;
            }
        }
    }

    // Progress Methods
    public void AddProgress(string thing)
    {
        if (!gameProgress.Contains(thing))
        {
            gameProgress.Add(thing);
            ObjectiveManager.Instance?.CheckAllObjectives();
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