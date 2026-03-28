using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfigSceneManager : MonoBehaviour
{
    [SerializeField] private string configSceneName = "Config";

    private Scene configScene;
    private bool isConfigLoaded = false;
    public bool IsConfigLoaded => isConfigLoaded;

    /// <summary>
    /// Additively loads the Config scene
    /// </summary>
    public void LoadConfigScene()
    {
        if (isConfigLoaded)
        {
            Debug.LogWarning("Config scene is already loaded");
            return;
        }

        SceneManager.LoadSceneAsync(configSceneName, LoadSceneMode.Additive).completed += operation =>
        {
            configScene = SceneManager.GetSceneByName(configSceneName);
            isConfigLoaded = true;
            Debug.Log("Config scene loaded");
        };
    }

    /// <summary>
    /// Deletes everything from the Config scene (unloads it completely)
    /// </summary>
    public void DeleteConfigScene()
    {
        if (!isConfigLoaded)
        {
            Debug.LogWarning("Config scene is not loaded");
            return;
        }

        SceneManager.UnloadSceneAsync(configScene).completed += operation =>
        {
            isConfigLoaded = false;
            Debug.Log("Config scene deleted");
        };
    }
}