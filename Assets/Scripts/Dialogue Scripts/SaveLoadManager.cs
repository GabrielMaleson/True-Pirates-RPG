using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [SerializeField] private string saveFileName = "savegame.dat";
    private bool isLoading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.tag = "Inventory"; // Ensure it persists
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [System.Serializable]
    private class SaveData
    {
        // Scene info
        public string savedSceneName;

        // Inventory data
        public List<SerializedItem> savedItems = new List<SerializedItem>();
        public int savedGold;

        // Game progress
        public List<string> savedGameProgress = new List<string>();

        // Party member data
        public List<SerializedPartyMember> savedPartyMembers = new List<SerializedPartyMember>();
    }

    [System.Serializable]
    private class SerializedItem
    {
        public string itemID; // Use ID instead of name for reliability
        public int quantity;

        public SerializedItem(DadosItem item, int qty)
        {
            itemID = item.id;
            quantity = qty;
        }
    }

    [System.Serializable]
    private class SerializedPartyMember
    {
        public string characterName; // For finding the template
        public int level;
        public int currentHP;
        public int currentAP;
        public int currentExperience;
        public string weaponID;
        public string armorID;
        public List<string> learnedAttackIDs = new List<string>();

        public SerializedPartyMember(PartyMemberState member)
        {
            characterName = member.CharacterName;
            level = member.level;
            currentHP = member.currentHP;
            currentAP = member.currentAP;
            currentExperience = member.currentExperience;

            weaponID = member.weapon != null ? member.weapon.id : "";
            armorID = member.armor != null ? member.armor.id : "";

            foreach (var attack in member.learnedAttacks)
            {
                if (attack != null)
                    learnedAttackIDs.Add(attack.name);
            }
        }
    }

    public void NewGame()
    {
          SceneManager.LoadScene("Beginning");
    }

    public void SaveGame()
    {
        SistemaInventario inventory = SistemaInventario.Instance;
        if (inventory == null)
        {
            Debug.LogError("SistemaInventario not found during save!");
            return;
        }

        SaveData data = new SaveData();
        data.savedSceneName = SceneManager.GetActiveScene().name;
        data.savedGold = inventory.moedas;
        data.savedGameProgress = new List<string>(inventory.gameProgress);

        // Save inventory items
        foreach (SlotInventario slot in inventory.inventario)
        {
            if (slot.dadosDoItem != null && slot.quantidade > 0)
            {
                data.savedItems.Add(new SerializedItem(slot.dadosDoItem, slot.quantidade));
            }
        }

        // Save party members
        foreach (var member in inventory.partyMembers)
        {
            if (member != null)
            {
                data.savedPartyMembers.Add(new SerializedPartyMember(member));
            }
        }

        BinaryFormatter formatter = new BinaryFormatter();
        string path = GetSavePath();

        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(stream, data);
        }

        Debug.Log($"Game saved in scene: {data.savedSceneName}");
    }

    public void LoadGame()
    {
        string path = GetSavePath();

        if (!File.Exists(path))
        {
            Debug.LogError("No save file found!");
            return;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        SaveData data = null;

        using (FileStream stream = new FileStream(path, FileMode.Open))
        {
            data = (SaveData)formatter.Deserialize(stream);
        }

        if (data == null)
        {
            Debug.LogError("Failed to load save data!");
            return;
        }

        isLoading = true;
        StartCoroutine(LoadGameCoroutine(data));
    }

    private IEnumerator LoadGameCoroutine(SaveData data)
    {
        // Handle special scene cases
        if (data.savedSceneName == "Beginning")
        {
            SceneManager.LoadScene(data.savedSceneName);
        }
        else
        {
            // Load the main scene
            SceneManager.LoadScene(data.savedSceneName);

            // Wait for scenes to load
            yield return new WaitForSeconds(0.2f);
        }

        // Find SistemaInventario after scenes are loaded
        SistemaInventario inventory = FindFirstObjectByType<SistemaInventario>();
        if (inventory == null)
        {
            Debug.LogError("SistemaInventario not found after scene load!");
            isLoading = false;
            yield break;
        }

        // Clear current inventory
        inventory.inventario.Clear();

        // Load saved gold
        inventory.ModificadorMoedas(data.savedGold - inventory.moedas);

        // Load saved items
        foreach (SerializedItem serializedItem in data.savedItems)
        {
            DadosItem item = GetItemByID(serializedItem.itemID);
            if (item != null)
            {
                inventory.AdicionarItem(item, serializedItem.quantity);
            }
            else
            {
                Debug.LogWarning($"Item not found with ID: {serializedItem.itemID}");
            }
        }

        // Load party members (clear existing first)
        inventory.partyMembers.Clear();
        foreach (SerializedPartyMember serializedMember in data.savedPartyMembers)
        {
            // Find the template CharacterData
            CharacterData template = GetCharacterDataByName(serializedMember.characterName);
            if (template != null)
            {
                PartyMemberState member = new PartyMemberState(template, serializedMember.level);
                member.currentHP = serializedMember.currentHP;
                member.currentAP = serializedMember.currentAP;
                member.currentExperience = serializedMember.currentExperience;

                // Load equipped items
                if (!string.IsNullOrEmpty(serializedMember.weaponID))
                {
                    DadosItem weapon = GetItemByID(serializedMember.weaponID);
                    if (weapon != null) member.EquipWeapon(weapon);
                }

                if (!string.IsNullOrEmpty(serializedMember.armorID))
                {
                    DadosItem armor = GetItemByID(serializedMember.armorID);
                    if (armor != null) member.EquipArmor(armor);
                }

                // Load learned attacks
                member.RefreshLearnedAttacks(); // Base attacks from level
                                                // Additional attacks if needed

                inventory.partyMembers.Add(member);
            }
            else
            {
                Debug.LogWarning($"Character template not found: {serializedMember.characterName}");
            }
        }
        // Load saved progress
        inventory.gameProgress.Clear();
        foreach (string progress in data.savedGameProgress)
        {
            inventory.AddProgress(progress);
        }

        Debug.Log($"Game loaded from scene: {data.savedSceneName}");
        isLoading = false;
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private DadosItem GetItemByID(string itemID)
    {
        // Load all DadosItem assets from Resources
        DadosItem[] allItems = Resources.LoadAll<DadosItem>("");
        foreach (DadosItem item in allItems)
        {
            if (item.id == itemID)
            {
                return item;
            }
        }

        // Fallback: search by name if ID not found
        foreach (DadosItem item in allItems)
        {
            if (item.nomeDoItem == itemID)
            {
                Debug.LogWarning($"Found item by name '{itemID}' - consider using IDs for reliability");
                return item;
            }
        }

        return null;
    }

    private CharacterData GetCharacterDataByName(string characterName)
    {
        CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("");
        foreach (CharacterData character in allCharacters)
        {
            if (character.characterName == characterName)
            {
                return character;
            }
        }
        return null;
    }

    private AttackFile GetAttackByName(string attackName)
    {
        AttackFile[] allAttacks = Resources.LoadAll<AttackFile>("");
        foreach (AttackFile attack in allAttacks)
        {
            if (attack.name == attackName)
            {
                return attack;
            }
        }
        return null;
    }

    public bool SaveExists()
    {
        return File.Exists(GetSavePath());
    }

    public void DeleteSave()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save file deleted");
        }
    }
}