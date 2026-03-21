using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically finds every CanvasScaler in all loaded scenes and keeps them
/// synced so UI scales correctly whenever the window is resized.
///
/// Setup:
///   1. Add this to a persistent GameObject in your starting scene (e.g. alongside MusicManager).
///   2. That's it. No manual list filling needed.
///
/// It re-scans whenever a scene is loaded or unloaded, so additive scenes
/// (like Combat or Config) are picked up automatically.
/// </summary>
public class DynamicResolutionHandler : MonoBehaviour
{
    public static DynamicResolutionHandler Instance { get; private set; }

    [Header("Reference Resolution")]
    [Tooltip("The resolution the game was designed at. Everything scales relative to this.")]
    public Vector2 designResolution = new Vector2(1920, 1080);

    private readonly List<CanvasScaler> canvasScalers = new List<CanvasScaler>();
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Start()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        RefreshAll();
    }

    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            ApplyToAll();
        }
    }

    // Re-scan whenever a scene is loaded (including additive)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAll();
    }

    // Re-scan whenever a scene is unloaded (removes any destroyed scalers)
    private void OnSceneUnloaded(Scene scene)
    {
        RefreshAll();
    }

    /// <summary>
    /// Clears the internal list and finds every CanvasScaler across all loaded scenes.
    /// </summary>
    private void RefreshAll()
    {
        canvasScalers.Clear();

        // FindObjectsByType searches all loaded scenes including additive ones
        CanvasScaler[] all = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
        foreach (var scaler in all)
        {
            if (!scaler.CompareTag("Ignore"))
                canvasScalers.Add(scaler);
        }

        ApplyToAll();
    }

    private void ApplyToAll()
    {
        canvasScalers.RemoveAll(s => s == null); // clean up destroyed ones
        foreach (var scaler in canvasScalers)
            ApplyTo(scaler);
    }

    private void ApplyTo(CanvasScaler scaler)
    {
        // Only touch canvases already using Scale With Screen Size —
        // don't override canvases intentionally set to Constant Pixel Size or World Space.
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return;

        scaler.referenceResolution = designResolution;
        scaler.matchWidthOrHeight = 0.5f; // balanced between width and height
    }
}
