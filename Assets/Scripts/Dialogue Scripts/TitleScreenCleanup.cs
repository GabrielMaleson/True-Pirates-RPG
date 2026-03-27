using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class TitleScreenCleanup : MonoBehaviour
{
    [SerializeField] private string saveFileName = "savegame.dat";
    public GameObject continueObject;

    // Store the original scene's root objects
    private List<GameObject> originalSceneRoots = new List<GameObject>();

    void Start()
    {
        // Store all root objects in the current scene before any changes
        CaptureOriginalSceneHierarchy();

        CheckAndEnableContinueButton();
        StartCoroutine(CleanupAfterFrame());
    }

    // Aguarda um frame para que os Destroy() diferidos (ex: singletons duplicados que
    // se auto-destroem no Awake) sejam processados antes de contar duplicatas.
    private IEnumerator CleanupAfterFrame()
    {
        yield return null;
        CleanupNonTitleScreenObjects();
    }

    private void CaptureOriginalSceneHierarchy()
    {
        // Get all root objects in the current scene
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        originalSceneRoots.Clear();

        foreach (GameObject root in rootObjects)
        {
            originalSceneRoots.Add(root);
            Debug.Log($"Original root object: {root.name}");
        }
    }

    public void CheckAndEnableContinueButton()
    {
        if (continueObject != null)
        {
            // Check if save data exists
            bool saveExists = SaveExists();

            // Get the Button component
            Button continueButton = continueObject.GetComponent<Button>();

            if (continueButton != null)
            {
                // Enable the button if save data exists
                continueButton.interactable = saveExists;
            }
            else
            {
                Debug.LogWarning("Continue GameObject found but no Button component attached");
            }

            // Get the TMP_Text component
            TMP_Text continueImage = continueObject.GetComponent<TMP_Text>();
            if (continueImage != null)
            {
                continueImage.color = saveExists ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
            else
            {
                Debug.LogWarning("Continue GameObject found but no TMP_Text component attached");
            }

            Debug.Log($"Continue button {(saveExists ? "enabled" : "disabled")} - Save exists: {saveExists}");
        }
        else
        {
            Debug.Log("No GameObject with 'Continue' tag found in this scene");
        }
    }

    public void CleanupNonTitleScreenObjects()
    {
        Time.timeScale = 1f;

        // Get all objects in the scene
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        // First, collect all SFX objects and count duplicates
        List<GameObject> sfxObjects = new List<GameObject>();
        Dictionary<string, int> sfxNameCounts = new Dictionary<string, int>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            if (obj.CompareTag("SFX"))
            {
                sfxObjects.Add(obj);
                string objName = obj.name;
                if (!sfxNameCounts.ContainsKey(objName))
                    sfxNameCounts[objName] = 0;
                sfxNameCounts[objName]++;
            }
        }

        List<GameObject> objectsToDestroy = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            if (obj == gameObject) continue;
            if (obj.name == "[Debug Updater]") continue;

            // Special handling for SFX objects
            if (obj.CompareTag("SFX"))
            {
                // Check if this is a duplicate of an SFX object with the same name
                if (sfxNameCounts.TryGetValue(obj.name, out int count) && count > 1)
                {
                    objectsToDestroy.Add(obj);
                    Debug.Log($"Marked for destruction: {obj.name} (duplicate SFX object)");
                }
                // Otherwise, preserve SFX objects even if not in original hierarchy
                continue;
            }

            // For non-SFX objects, check if they're part of the original scene hierarchy
            if (!IsInOriginalSceneHierarchy(obj))
            {
                objectsToDestroy.Add(obj);
                Debug.Log($"Marked for destruction: {obj.name} (not in original hierarchy)");
            }
        }

        // Destroy all marked objects
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj == null) continue;
            Destroy(obj);
            Debug.Log("Destruído objeto: " + obj.name);
        }

        Debug.Log($"Limpeza da tela de título concluída. Destruídos {objectsToDestroy.Count} objetos.");
    }

    private bool IsInOriginalSceneHierarchy(GameObject obj)
    {
        // Check if this object is a root object in the original scene
        if (originalSceneRoots.Contains(obj))
        {
            return true;
        }

        // Check if this object is a child of any original root object
        Transform current = obj.transform;
        while (current != null)
        {
            if (originalSceneRoots.Contains(current.gameObject))
            {
                return true;
            }
            current = current.parent;
        }

        return false;
    }

    private bool SaveExists()
    {
        string path = GetSavePath();
        return File.Exists(path);
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }
}