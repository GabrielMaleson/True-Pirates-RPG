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
            playerInventory = FindAnyObjectByType<SistemaInventario>();

            StartEncounter();
        }
    }

    public void StartEncounter()
    {
        Debug.Log("=== Starting Encounter ===");

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

        // Store data
        encounterData.encounterStarterObject = encounterStarterObject;
        encounterData.playerInventory = playerInventory;
        encounterData.encounterFile = encounterFile;

        // Log what's being stored
        Debug.Log($"Encounter File: {encounterFile?.name}");
        if (encounterFile != null)
        {
            Debug.Log($"Number of enemies in file: {encounterFile.enemies.Count}");
            foreach (var enemy in encounterFile.enemies)
            {
                Debug.Log($"- Enemy: {enemy.characterData?.characterName}, Prefab: {enemy.enemyPrefab?.name}, Level: {enemy.level}");
            }
        }

        // Store player's current party state
        encounterData.playerPartyMembers = playerInventory.GetPartyMembersForCombat();
        Debug.Log($"Party members: {encounterData.playerPartyMembers.Count}");

        // Load combat scene
        GameObject sceneObj = new GameObject("PreviousScene");
        sceneObj.AddComponent<PreviousScene>();
        sceneObj.GetComponent<PreviousScene>().UnloadScene();

        SceneManager.LoadScene("Combat", LoadSceneMode.Additive);
    }
}