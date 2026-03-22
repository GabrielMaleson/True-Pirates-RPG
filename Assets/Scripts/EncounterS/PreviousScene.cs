using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PreviousScene : MonoBehaviour
{
    private Scene originalScene;
    private string originalSceneName;

    private List<GameObject> sceneObjects = new List<GameObject>();

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

            // Ignore-tagged objects are not stored and not touched —
            // they keep whatever state they're in (e.g. a deactivated NPC stays deactivated)
            if (obj.CompareTag("Ignore"))
            {
                Debug.Log($"Ignoring object: {obj.name}");
                continue;
            }

            if (obj == this.gameObject || obj == encounterData?.gameObject)
                continue;

            sceneObjects.Add(obj);
            obj.SetActive(false);
        }

        Debug.Log($"PreviousScene: Stored {sceneObjects.Count} objects from {originalSceneName}");
    }

    public void LoadScene()
    {
        Debug.Log($"PreviousScene: Restoring {sceneObjects.Count} objects from {originalSceneName}");

        EncounterData encounterData = FindFirstObjectByType<EncounterData>();

        if (encounterData != null && encounterData.encounterStarterObject != null)
        {
            Destroy(encounterData.encounterStarterObject);
            encounterData.encounterStarterObject = null;
        }

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

        // Reactivate stored objects — this triggers OnEnable on things like
        // ProgressCheck, which is how post-combat dialogue gets started.
        // Objects marked KeepDeactivated (dismissed via <<deactivate>> Yarn command)
        // are skipped so they don't come back uninvited.
        foreach (GameObject obj in sceneObjects)
        {
            if (obj == null) continue;
            if (obj.GetComponent<KeepDeactivated>() != null) continue;
            obj.SetActive(true);
        }

        FixAudioListeners();

        if (encounterData != null)
        {
            Vector3 playerPosition = Vector3.zero;
            encounterData.ReactivateOriginalPlayer(playerPosition);
        }

        sceneObjects.Clear();
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
