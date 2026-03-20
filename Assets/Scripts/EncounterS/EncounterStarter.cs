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
            Debug.LogError("No EncounterFile assigned!");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("No player inventory found!");
            return;
        }

        GameObject encounterStarterObject = this.gameObject;

        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData == null)
        {
            GameObject dataObj = new GameObject("EncounterData");
            DontDestroyOnLoad(dataObj);
            encounterData = dataObj.AddComponent<EncounterData>();
        }
        // In EncounterStarter.cs, after creating encounterData:
        encounterData.battleTriggerProgress = "some_progress_tag"; // The progress that triggered this battle
        encounterData.encounterStarterObject = encounterStarterObject;
        encounterData.playerInventory = playerInventory;
        encounterData.encounterFile = encounterFile;

        // Store player's current party state (references are fine)
        encounterData.playerPartyMembers = playerInventory.GetPartyMembersForCombat();

        // Store enemy data from encounter file (creates new PartyMemberState objects)
        encounterData.enemyPartyMembers.Clear();
        foreach (var enemyData in encounterFile.enemies)
        {
            if (enemyData.characterData != null)
            {
                PartyMemberState enemyState = new PartyMemberState(enemyData.characterData, enemyData.level);
                if (enemyData.overrideHP > 0)
                {
                    enemyState.currentHP = enemyData.overrideHP;
                }
                encounterData.enemyPartyMembers.Add(enemyState);
                encounterData.enemyPrefabs.Add(enemyData.enemyPrefab);
            }
        }

        GameObject sceneObj = new GameObject("PreviousScene");
        sceneObj.AddComponent<PreviousScene>();
        sceneObj.GetComponent<PreviousScene>().UnloadScene();

        SceneManager.LoadScene("Combat", LoadSceneMode.Additive);
    }
}