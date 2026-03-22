using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PreviousScene : MonoBehaviour
{
    private Scene originalScene;
    private string originalSceneName;

    // Store each object AND its active state at the time of unload
    private List<GameObject> sceneObjects = new List<GameObject>();
    private List<bool> sceneObjectWasActive = new List<bool>();

    public void UnloadScene()
    {
        originalScene = SceneManager.GetActiveScene();
        originalSceneName = originalScene.name;

        GameObject[] rootObjects = originalScene.GetRootGameObjects();
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();

        foreach (GameObject obj in rootObjects)
        {
            if (obj.CompareTag("Inventory"))
            {
                Debug.Log($"Preserving Inventory object: {obj.name}");
                DontDestroyOnLoad(obj);
                continue;
            }

            if (obj.CompareTag("Ignore"))
            {
                Debug.Log($"Ignoring object: {obj.name}");
                continue;
            }

            if (obj == this.gameObject || obj == encounterData?.gameObject)
                continue;

            // Record whether it was active BEFORE we deactivate it
            sceneObjects.Add(obj);
            sceneObjectWasActive.Add(obj.activeSelf);
            obj.SetActive(false);
        }

        Debug.Log($"PreviousScene: Stored {sceneObjects.Count} objects from {originalSceneName}");
    }

    public void LoadScene()
    {
        Debug.Log($"PreviousScene: Restoring {sceneObjects.Count} objects to {originalSceneName}");

        EncounterData encounterData = FindFirstObjectByType<EncounterData>();

        if (encounterData != null && encounterData.encounterStarterObject != null)
        {
            Destroy(encounterData.encounterStarterObject);
            encounterData.encounterStarterObject = null;
        }

        // Fix Inventory objects (preserved via DontDestroyOnLoad)
        foreach (var invObj in GameObject.FindGameObjectsWithTag("Inventory"))
        {
            invObj.SetActive(true);
            AudioListener listener = invObj.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        foreach (var ignoreObj in GameObject.FindGameObjectsWithTag("Ignore"))
        {
            AudioListener listener = ignoreObj.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        // Restore each object to the state it was in when combat started
        for (int i = 0; i < sceneObjects.Count; i++)
        {
            if (sceneObjects[i] != null)
                sceneObjects[i].SetActive(sceneObjectWasActive[i]);
        }

        FixAudioListeners();

        if (encounterData != null)
        {
            Vector3 playerPosition = Vector3.zero;
            encounterData.ReactivateOriginalPlayer(playerPosition);
        }

        sceneObjects.Clear();
        sceneObjectWasActive.Clear();

        Destroy(gameObject);
    }

    private void FixAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        if (listeners.Length > 1)
        {
            for (int i = 1; i < listeners.Length; i++)
                listeners[i].enabled = false;
        }
        else if (listeners.Length == 0)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                mainCam.gameObject.AddComponent<AudioListener>();
        }
    }
}
