using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PreviousScene : MonoBehaviour
{
    private Scene originalScene;
    private List<GameObject> sceneObjects = new List<GameObject>();
    private string originalSceneName;

    public void UnloadScene()
    {
        // Get the current active scene (the map scene)
        originalScene = SceneManager.GetActiveScene();
        originalSceneName = originalScene.name;

        // Get all root objects in the scene
        GameObject[] rootObjects = originalScene.GetRootGameObjects();

        // Find the EncounterData object to exclude it
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();

        // Store references to all scene objects
        foreach (GameObject obj in rootObjects)
        {
            // Skip objects with "Inventory" tag - they should persist
            if (obj.CompareTag("Inventory"))
            {
                Debug.Log($"Preserving Inventory object: {obj.name}");
                DontDestroyOnLoad(obj);
                continue;
            }
            
            // Skip objects with "Ignore" tag - they won't be stored or reactivated
            if (obj.CompareTag("Ignore"))
            {
                Debug.Log($"Ignoring object (won't be reactivated): {obj.name}");
                // We don't store these objects, so they'll stay as they are
                continue;
            }
            
            // Skip the EncounterData object and this manager
            if (obj != this.gameObject &&
                obj != encounterData?.gameObject)
            {
                sceneObjects.Add(obj);
                obj.SetActive(false);
            }
        }

        Debug.Log($"PreviousScene: Stored {sceneObjects.Count} objects from {originalSceneName}");
    }

    public void LoadScene()
    {
        Debug.Log($"PreviousScene: Loading {originalSceneName} with {sceneObjects.Count} objects");

        // Find EncounterData
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();

        // Destroy the encounter starter object if it exists
        if (encounterData != null && encounterData.encounterStarterObject != null)
        {
            GameObject starterObject = encounterData.encounterStarterObject;
            Destroy(starterObject);
            Debug.Log($"PreviousScene: Destroyed encounter starter: {starterObject.name}");
            encounterData.encounterStarterObject = null;
        }

        // Find all Inventory objects that were preserved
        GameObject[] inventoryObjects = GameObject.FindGameObjectsWithTag("Inventory");
        foreach (var invObj in inventoryObjects)
        {
            Debug.Log($"Found preserved Inventory object: {invObj.name}");
            invObj.SetActive(true);
        }

        // Find all Ignore objects (they were never deactivated, so nothing to do)
        GameObject[] ignoreObjects = GameObject.FindGameObjectsWithTag("Ignore");
        foreach (var ignoreObj in ignoreObjects)
        {
            Debug.Log($"Found Ignore object (keeping as is): {ignoreObj.name}");
            // These objects were never deactivated, so they remain active
        }

        // Reactivate all stored objects
        foreach (GameObject obj in sceneObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        // Reactivate player at the encounter location
        if (encounterData != null)
        {
            // Get the position where combat started
            Vector3 playerPosition = Vector3.zero;
            if (encounterData.encounterStarterObject != null)
            {
                playerPosition = encounterData.encounterStarterObject.transform.position;
            }
            
            // If combat was victorious, apply rewards
            if (encounterData.combatVictory)
            {
                encounterData.ApplyCombatRewards();
            }
            
            encounterData.ReactivateOriginalPlayer(playerPosition);
        }

        // Clear the list
        sceneObjects.Clear();

        Debug.Log($"PreviousScene: Reactivated all objects for {originalSceneName}");

        // Destroy this manager after scene is loaded
        Destroy(gameObject);
    }
}