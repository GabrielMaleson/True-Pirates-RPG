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

        GameObject sceneObj = new GameObject("PreviousScene");
        sceneObj.AddComponent<PreviousScene>();
        sceneObj.GetComponent<PreviousScene>().UnloadScene();

        SceneManager.LoadScene("Combat", LoadSceneMode.Additive);
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
        return encounterData;
    }
}