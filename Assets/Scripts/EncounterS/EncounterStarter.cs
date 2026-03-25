using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterStarter : MonoBehaviour
{
    [Header("Encounter Configuration")]
    public EncounterFile encounterFile;

    [Header("Player Reference")]
    public SistemaInventario playerInventory;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<SistemaInventario>();

            StartEncounter();
        }
    }

    public void StartEncounter()
    {
        if (encounterFile == null)
        {
            Debug.LogError("Nenhum EncounterFile atribuído!");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("Nenhum inventário do jogador encontrado!");
            return;
        }

        EncounterData encounterData = BuildEncounterData(encounterFile, playerInventory);
        encounterData.encounterStarterObject = this.gameObject;

        BattleTransitionManager.GetOrCreate().StartTransitionThen(encounterFile.transitionType, () =>
        {
            // Tela completamente preta — inicia música de batalha aqui para evitar que toque durante a transição de entrada
            MusicManager.Instance?.StopMusic();
            if (encounterFile.battleMusic != null)
                MusicManager.Instance?.PlayClip(encounterFile.battleMusic);

            GameObject sceneObj = new GameObject("PreviousScene");
            sceneObj.AddComponent<PreviousScene>();
            sceneObj.GetComponent<PreviousScene>().UnloadScene();
            SceneManager.LoadScene("Combat", LoadSceneMode.Additive);
        });
    }

    /// <summary>
    /// Chamado pelos gerenciadores de cutscene (DynamicCutsceneScript, SpecialCutsceneScript)
    /// para iniciar um encontro. Centraliza: configuração de dados, início da música, transição e carregamento de cena.
    /// Não define encounterStarterObject — este método é para triggers de cutscene, não objetos de zona descartáveis.
    /// </summary>
    public static void StartEncounterFromCutscene(EncounterFile encounterFile, SistemaInventario inventory)
    {
        BuildEncounterData(encounterFile, inventory);

        BattleTransitionManager.GetOrCreate().StartTransitionThen(encounterFile.transitionType, () =>
        {
            // Tela completamente preta — inicia música de batalha aqui para evitar que toque durante a transição de entrada
            MusicManager.Instance?.StopMusic();
            if (encounterFile.battleMusic != null)
                MusicManager.Instance?.PlayClip(encounterFile.battleMusic);
            else
                Debug.LogWarning($"[EncounterStarter] EncounterFile '{encounterFile.encounterName}' não tem battleMusic atribuído.");

            GameObject sceneObj = new GameObject("PreviousScene");
            sceneObj.AddComponent<PreviousScene>();
            sceneObj.GetComponent<PreviousScene>().UnloadScene();
            SceneManager.LoadSceneAsync("Combat", LoadSceneMode.Additive);
            Debug.Log("Cena de combate sendo carregada.");
        });
    }

    /// <summary>
    /// Cria e preenche o EncounterData a partir de um EncounterFile.
    /// Não define encounterStarterObject — o chamador é responsável por isso (ou por não definir).
    /// </summary>
    public static EncounterData BuildEncounterData(EncounterFile encounterFile, SistemaInventario inventory)
    {
        EncounterData encounterData = Object.FindFirstObjectByType<EncounterData>();
        if (encounterData == null)
        {
            GameObject dataObj = new GameObject("EncounterData");
            Object.DontDestroyOnLoad(dataObj);
            encounterData = dataObj.AddComponent<EncounterData>();
        }

        encounterData.playerInventory = inventory;
        encounterData.encounterFile = encounterFile;
        encounterData.playerPartyMembers = inventory.GetPartyMembersForCombat();

        // Salvar snapshot do estado do grupo antes da batalha (para permitir tentativa em caso de derrota)
        BattleSaveManager.GetOrCreate().SaveSnapshot(encounterData.playerPartyMembers);

        encounterData.enemyPartyMembers.Clear();
        encounterData.enemyPrefabs.Clear();
        foreach (var enemyData in encounterFile.enemies)
        {
            if (enemyData.characterData != null)
            {
                PartyMemberState enemyState = new PartyMemberState(enemyData.characterData, enemyData.level);
                if (enemyData.overrideHP > 0)
                    enemyState.currentHP = enemyData.overrideHP;
                encounterData.enemyPartyMembers.Add(enemyState);
                encounterData.enemyPrefabs.Add(enemyData.enemyPrefab);
            }
        }

        encounterData.CalculateRewards();

        // Set battle music from encounter file
        encounterData.battleMusic = encounterFile.battleMusic;

        return encounterData;
    }
}