using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PreviousScene : MonoBehaviour
{
    /// <summary>Nome da cena de exploração armazenada mais recentemente — usado pelo SaveLoadManager para salvar a cena correta ao sair durante uma batalha.</summary>
    public static string LastExplorationScene { get; private set; }

    private Scene originalScene;
    private string originalSceneName;

    private List<GameObject> sceneObjects         = new List<GameObject>();
    private List<Camera>     disabledIgnoreCameras = new List<Camera>();

    public void UnloadScene()
    {
        originalScene = SceneManager.GetActiveScene();
        originalSceneName = originalScene.name;
        LastExplorationScene = originalSceneName;

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

            // Ignore-tagged objects are not stored and not deactivated —
            // they keep whatever state they're in (e.g. a deactivated NPC stays deactivated).
            // Exception: Camera components are disabled to prevent rendering conflicts with the
            // combat camera. They are re-enabled in LoadScene().
            if (obj.CompareTag("Ignore"))
            {
                Camera ignoreCam = obj.GetComponent<Camera>();
                if (ignoreCam != null)
                {
                    ignoreCam.enabled = false;
                    disabledIgnoreCameras.Add(ignoreCam);
                }
                AudioListener ignoreListener = obj.GetComponent<AudioListener>();
                if (ignoreListener != null) ignoreListener.enabled = false;
                Debug.Log($"Ignorando objeto: {obj.name}");
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
        FixEventSystems();

        // Re-enable exactly the cameras we disabled in UnloadScene — stored by reference so
        // we don't rely on FindGameObjectsWithTag which searches all loaded scenes (including
        // the Combat scene that hasn't been unloaded yet) and could miss or clobber the wrong camera.
        foreach (var cam in disabledIgnoreCameras)
        {
            if (cam != null) cam.enabled = true;
        }
        disabledIgnoreCameras.Clear();

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

    /// <summary>
    /// Garante que exatamente um EventSystem esteja ativo depois de restaurar a cena.
    /// O EventSystem da cena de combate pode desabilitar o da cena de exploração enquanto ambos
    /// coexistem; depois que o Combat descarrega, o da exploração precisa ser reativado.
    /// </summary>
    private void FixEventSystems()
    {
        EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        bool anyEnabled = false;
        foreach (var es in systems)
        {
            if (es.isActiveAndEnabled) { anyEnabled = true; break; }
        }

        if (!anyEnabled)
        {
            if (systems.Length > 0)
            {
                // Reativa o primeiro encontrado
                systems[0].gameObject.SetActive(true);
                Debug.Log($"[PreviousScene] EventSystem reativado: {systems[0].gameObject.name}");
            }
            else
            {
                // Nenhum EventSystem na cena — cria um
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
                Debug.LogWarning("[PreviousScene] Nenhum EventSystem encontrado — criado um novo. Considere manter um EventSystem permanente na cena.");
            }
        }
    }
}
