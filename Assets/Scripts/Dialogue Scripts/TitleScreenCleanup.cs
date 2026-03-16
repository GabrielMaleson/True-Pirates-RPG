using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Added for Button and Image components
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;

public class TitleScreenCleanup : MonoBehaviour
{
    [SerializeField] private string saveFileName = "savegame.dat";
    public GameObject continueObject;

    void Start()
    {
        CheckAndEnableContinueButton();
        CleanupNonTitleScreenObjects();
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

            // Get the Image component and set its color to white if save exists
            TMP_Text continueImage = continueObject.GetComponent<TMP_Text>();
            if (continueImage != null)
            {
                continueImage.color = saveExists ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
            else
            {
                Debug.LogWarning("Continue GameObject found but no Image component attached");
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
        // Get all active GameObjects in the scene
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> objectsToDestroy = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Don't destroy this cleanup script's object
            if (obj == gameObject) continue;

            // Don't destroy objects called "[Debug Updater]"
            if (obj.name == "[Debug Updater]") continue;

            // Check if this object or any of its parents have the TitleScreen tag
            if (IsInTitleScreenHierarchy(obj)) continue;

            // Don't destroy objects that are part of the title screen scene
            if (obj.scene.name == SceneManager.GetActiveScene().name) continue;

            objectsToDestroy.Add(obj);
        }

        // Destroy all identified objects
        foreach (GameObject obj in objectsToDestroy)
        {
            Destroy(obj);
            Debug.Log("Destroyed object: " + obj.name);
        }

        Debug.Log("Title screen cleanup completed. Destroyed " + objectsToDestroy.Count + " objects.");
    }

    private bool IsInTitleScreenHierarchy(GameObject obj)
    {
        // Check current object and all parents up the hierarchy
        Transform current = obj.transform;

        while (current != null)
        {
            if (current.CompareTag("TitleScreen"))
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