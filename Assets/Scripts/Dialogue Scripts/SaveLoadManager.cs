using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Salva e carrega o estado do jogo em JSON em Application.persistentDataPath.
///
/// Requisito para lookup de itens/personagens:
///   DadosItem e CharacterData precisam estar dentro de uma pasta Resources
///   para que Resources.LoadAll possa encontrá-los ao carregar o save.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [SerializeField] private string saveFileName = "savegame.json";

    // ── Estruturas serializáveis ──────────────────────────────────────────────

    [System.Serializable]
    private class SaveData
    {
        public string savedSceneName;
        public int savedGold;
        public List<string> savedGameProgress = new List<string>();
        public List<SavedItem> savedItems       = new List<SavedItem>();
        public List<SavedPartyMember> savedPartyMembers = new List<SavedPartyMember>();
    }

    [System.Serializable]
    private class SavedItem
    {
        public string itemID;
        public int    quantity;
    }

    [System.Serializable]
    private class SavedPartyMember
    {
        public string characterName;
        public int    level;
        public int    currentHP;
        public int    currentAP;
        public int    currentExperience;
        public string weaponID;
        public string armorID;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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

    // ── API pública ───────────────────────────────────────────────────────────

    public void NewGame()
    {
        SceneManager.LoadScene("Beginning");
    }

    public void SaveGame()
    {
        SistemaInventario inventory = SistemaInventario.Instance;
        if (inventory == null)
        {
            Debug.LogError("[SaveLoadManager] SistemaInventario não encontrado ao salvar!");
            return;
        }

        SaveData data = new SaveData
        {
            savedSceneName  = SceneManager.GetActiveScene().name,
            savedGold       = inventory.moedas,
            savedGameProgress = new List<string>(inventory.GetGameProgress())
        };

        foreach (SlotInventario slot in inventory.inventario)
        {
            if (slot.dadosDoItem != null && slot.quantidade > 0)
                data.savedItems.Add(new SavedItem { itemID = slot.dadosDoItem.id, quantity = slot.quantidade });
        }

        foreach (var member in inventory.partyMembers)
        {
            if (member == null) continue;
            data.savedPartyMembers.Add(new SavedPartyMember
            {
                characterName   = member.CharacterName,
                level           = member.level,
                currentHP       = member.currentHP,
                currentAP       = member.currentAP,
                currentExperience = member.currentExperience,
                weaponID        = member.weapon != null ? member.weapon.id : "",
                armorID         = member.armor  != null ? member.armor.id  : ""
            });
        }

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(GetSavePath(), json);
        Debug.Log($"[SaveLoadManager] Jogo salvo em: {GetSavePath()}");
    }

    public void LoadGame()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("[SaveLoadManager] Nenhum arquivo de save encontrado!");
            return;
        }

        string   json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            Debug.LogError("[SaveLoadManager] Falha ao ler o arquivo de save!");
            return;
        }

        StartCoroutine(LoadGameCoroutine(data));
    }

    private IEnumerator LoadGameCoroutine(SaveData data)
    {
        SceneManager.LoadScene(data.savedSceneName);
        yield return new WaitForSeconds(0.3f);

        SistemaInventario inventory = SistemaInventario.Instance;
        if (inventory == null)
        {
            Debug.LogError("[SaveLoadManager] SistemaInventario não encontrado após carregar a cena!");
            yield break;
        }

        // Ouro
        inventory.ModificadorMoedas(data.savedGold - inventory.moedas);

        // Inventário
        inventory.inventario.Clear();
        foreach (var saved in data.savedItems)
        {
            DadosItem item = FindItemByID(saved.itemID);
            if (item != null)
                inventory.AdicionarItem(item, saved.quantity);
            else
                Debug.LogWarning($"[SaveLoadManager] Item não encontrado: '{saved.itemID}'");
        }

        // Membros do grupo
        inventory.partyMembers.Clear();
        foreach (var saved in data.savedPartyMembers)
        {
            CharacterData template = FindCharacterByName(saved.characterName);
            if (template == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Personagem não encontrado: '{saved.characterName}'");
                continue;
            }

            PartyMemberState member = new PartyMemberState(template, saved.level);
            member.currentHP          = saved.currentHP;
            member.currentAP          = saved.currentAP;
            member.currentExperience  = saved.currentExperience;

            if (!string.IsNullOrEmpty(saved.weaponID))
            {
                DadosItem weapon = FindItemByID(saved.weaponID);
                if (weapon != null) member.EquipWeapon(weapon);
            }
            if (!string.IsNullOrEmpty(saved.armorID))
            {
                DadosItem armor = FindItemByID(saved.armorID);
                if (armor != null) member.EquipArmor(armor);
            }

            member.RefreshLearnedAttacks();
            inventory.partyMembers.Add(member);
        }

        // Progresso
        inventory.gameProgress.Clear();
        foreach (string tag in data.savedGameProgress)
            inventory.AddProgress(tag);

        Debug.Log($"[SaveLoadManager] Jogo carregado da cena '{data.savedSceneName}'.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private DadosItem FindItemByID(string id)
    {
        foreach (var item in Resources.LoadAll<DadosItem>(""))
            if (item.id == id) return item;
        return null;
    }

    private CharacterData FindCharacterByName(string name)
    {
        foreach (var c in Resources.LoadAll<CharacterData>(""))
            if (c.characterName == name) return c;
        return null;
    }

    private string GetSavePath() =>
        Path.Combine(Application.persistentDataPath, saveFileName);

    public bool SaveExists() => File.Exists(GetSavePath());

    public void DeleteSave()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[SaveLoadManager] Save deletado.");
        }
    }
}
