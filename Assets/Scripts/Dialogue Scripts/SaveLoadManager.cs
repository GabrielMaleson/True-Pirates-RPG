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
        // Reinício de batalha pendente (salvo ao sair durante um combate)
        public bool   hasPendingBattleRestart;
        public string pendingEncounterFileName;
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
        public string accessoryID;
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

        SaveData data = BuildSaveData(inventory, SceneManager.GetActiveScene().name);
        File.WriteAllText(GetSavePath(), JsonUtility.ToJson(data, prettyPrint: true));
        Debug.Log($"[SaveLoadManager] Jogo salvo em: {GetSavePath()}");
    }

    /// <summary>
    /// Salva o estado pré-batalha e fecha o jogo. Na próxima carga, o combate será reiniciado.
    /// Chame este método ao confirmar saída durante um combate.
    /// </summary>
    public void SaveAndQuitFromBattle(EncounterData encounterData)
    {
        WriteBattleSave(encounterData);
        Application.Quit();
    }

    public void SaveAndReturnToTitle(EncounterData encounterData)
    {
        WriteBattleSave(encounterData);
        SceneManager.LoadScene("TitleScreen");
    }

    private void WriteBattleSave(EncounterData encounterData)
    {
        SistemaInventario inventory = SistemaInventario.Instance;
        if (inventory == null)
        {
            Debug.LogError("[SaveLoadManager] SistemaInventario não encontrado ao salvar estado de batalha!");
            return;
        }

        // Restaurar HP/AP do grupo para o estado pré-batalha antes de salvar
        BattleSaveManager.Instance?.RestoreSnapshot(encounterData.playerPartyMembers);

        // Salvar usando a cena de exploração (não "Combat")
        string explorationScene = string.IsNullOrEmpty(PreviousScene.LastExplorationScene)
            ? SceneManager.GetActiveScene().name
            : PreviousScene.LastExplorationScene;

        SaveData data = BuildSaveData(inventory, explorationScene);
        data.hasPendingBattleRestart = true;
        data.pendingEncounterFileName = encounterData.encounterFile != null ? encounterData.encounterFile.name : "";

        File.WriteAllText(GetSavePath(), JsonUtility.ToJson(data, prettyPrint: true));
        Debug.Log($"[SaveLoadManager] Estado de batalha salvo — retornando ao título. Reinício pendente: '{data.pendingEncounterFileName}'");
    }

    private SaveData BuildSaveData(SistemaInventario inventory, string sceneName)
    {
        SaveData data = new SaveData
        {
            savedSceneName    = sceneName,
            savedGold         = inventory.moedas,
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
                characterName     = member.CharacterName,
                level             = member.level,
                currentHP         = member.currentHP,
                currentAP         = member.currentAP,
                currentExperience = member.currentExperience,
                accessoryID       = member.accessory != null ? member.accessory.id : "",
                armorID           = member.armor  != null ? member.armor.id  : ""
            });
        }

        return data;
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

            if (!string.IsNullOrEmpty(saved.accessoryID))
            {
                DadosItem accessory = FindItemByID(saved.accessoryID);
                if (accessory != null) member.EquipAccessory(accessory);
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

        // Restaura referências equippedTo nos slots de inventário
        inventory.RestoreEquippedSlots();

        Debug.Log($"[SaveLoadManager] Jogo carregado da cena '{data.savedSceneName}'.");

        // Reinício de batalha pendente — relançar o combate como se fosse uma nova tentativa
        if (data.hasPendingBattleRestart && !string.IsNullOrEmpty(data.pendingEncounterFileName))
        {
            string encounterFileName = data.pendingEncounterFileName;

            // Limpar a flag para não entrar em loop nas próximas cargas
            data.hasPendingBattleRestart = false;
            data.pendingEncounterFileName = "";
            File.WriteAllText(GetSavePath(), JsonUtility.ToJson(data, prettyPrint: true));

            yield return null; // aguardar um frame para a cena inicializar

            EncounterFile encounterFile = FindEncounterFileByName(encounterFileName);
            if (encounterFile != null)
            {
                Debug.Log($"[SaveLoadManager] Reiniciando batalha pendente: '{encounterFileName}'");
                EncounterStarter.StartEncounterFromCutscene(encounterFile, inventory);
            }
            else
            {
                Debug.LogWarning($"[SaveLoadManager] EncounterFile '{encounterFileName}' não encontrado — reinício de batalha cancelado. Certifique-se de que o asset está em uma pasta Resources.");
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private EncounterFile FindEncounterFileByName(string name)
    {
        foreach (var f in Resources.LoadAll<EncounterFile>(""))
            if (f.name == name) return f;
        return null;
    }

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
