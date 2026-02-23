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

        // Reactivate all stored objects
        foreach (GameObject obj in sceneObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        // Reactivate player at the encounter location
        if (encounterData != null && encounterData.originalPlayer != null)
        {
            // Get the position where combat started (where the EncounterStarter was)
            Vector3 playerPosition = Vector3.zero;

            // Find the encounter starter position from the destroyed object's last known position
            // You might want to store this position in EncounterData
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