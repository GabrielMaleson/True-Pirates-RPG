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
        StartCoroutine(CleanupAfterFrame());
    }

    // Aguarda um frame para que os Destroy() diferidos (ex: singletons duplicados que
    // se auto-destroem no Awake) sejam processados antes de contar duplicatas.
    private IEnumerator CleanupAfterFrame()
    {
        yield return null;
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

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        // Contar quantas vezes cada nome de objeto aparece (para detectar duplicatas)
        Dictionary<string, int> nameCounts = new Dictionary<string, int>();
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            if (!nameCounts.ContainsKey(obj.name))
                nameCounts[obj.name] = 0;
            nameCounts[obj.name]++;
        }

        List<GameObject> objectsToDestroy = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            if (obj == gameObject) continue;
            if (obj.name == "[Debug Updater]") continue;
            if (IsInTitleScreenHierarchy(obj)) continue;
            if (obj.scene.name == SceneManager.GetActiveScene().name) continue;

            // Apenas destruir se for uma duplicata — objetos únicos são preservados
            if (nameCounts.TryGetValue(obj.name, out int count) && count > 1)
                objectsToDestroy.Add(obj);
        }

        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj == null) continue;
            Destroy(obj);
            Debug.Log("Destruído objeto duplicado: " + obj.name);
        }

        Debug.Log("Limpeza da tela de título concluída. Destruídos " + objectsToDestroy.Count + " objetos.");
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