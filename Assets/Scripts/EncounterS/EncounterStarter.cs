using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterStarter : MonoBehaviour
{
    [Header("Encounter Configuration")]
    public EncounterFile encounterFile;

    [Header("Player Reference")]
    public SistemaInventario playerInventory; // Assign in inspector or find on player

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"Player triggered encounter: {encounterFile?.name}");

            playerInventory = FindAnyObjectByType<SistemaInventario>();
            StartEncounter();
        }
    }

    public void StartEncounter()
    {
        Debug.Log("=== Starting Encounter ===");

        if (encounterFile == null)
        {
            Debug.LogError("No EncounterFile assigned to EncounterStarter!");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("No player inventory found!");
            return;
        }

        // Store reference to THIS GameObject to disable it later
        GameObject encounterStarterObject = this.gameObject;

        // Get or create EncounterData
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData == null)
        {
            GameObject dataObj = new GameObject("EncounterData");
            DontDestroyOnLoad(dataObj);
            encounterData = dataObj.AddComponent<EncounterData>();
            Debug.Log("Created new EncounterData");
        }
        else
        {
            Debug.Log("Found existing EncounterData");
        }

        // Store the encounter starter reference
        encounterData.encounterStarterObject = encounterStarterObject;

        // Store player inventory
        encounterData.playerInventory = playerInventory;

        // LOAD THE ENCOUNTER FILE INTO ENCOUNTER DATA
        encounterData.LoadEncounterFromFile(encounterFile);

        // Store player's current party state
        encounterData.playerPartyMembers = playerInventory.GetPartyMembersForCombat();
        Debug.Log($"Party members stored: {encounterData.playerPartyMembers.Count}");

        // Log what we're sending to combat
        Debug.Log($"=== Encounter Data Summary ===");
        Debug.Log($"Enemies loaded: {encounterData.enemyCharacters.Count}");
        for (int i = 0; i < encounterData.enemyCharacters.Count; i++)
        {
            Debug.Log($"Enemy {i}: {encounterData.enemyCharacters[i]?.characterName}, Prefab: {encounterData.enemyPrefabs[i]?.name}");
        }

        // Create scene manager and load combat
        GameObject sceneObj = new GameObject("PreviousScene");
        sceneObj.AddComponent<PreviousScene>();
        sceneObj.GetComponent<PreviousScene>().UnloadScene();

        SceneManager.LoadScene("Combat", LoadSceneMode.Additive);
    }
}