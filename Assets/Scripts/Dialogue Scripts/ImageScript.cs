using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Yarn.Unity;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class PositionData
    {
        public string positionName;
        public Transform positionTransform;
    }

    [System.Serializable]
    public class SoundData
    {
        public string soundName;
        public AudioClip soundClip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = false;
    }

    [Header("UI References")]
    public GameObject dialogueCanvas;
    public Button continueButton;
    public GraphicRaycaster graphicRaycaster;
    public TextMeshProUGUI objectiveText;
    public GameObject objectivePanel;
    public GameObject blackScreen;

    [Header("Image Display")]
    public List<Sprite> sprites = new List<Sprite>();
    public List<string> spriteTags = new List<string>();
    public List<Image> imagePrefabs = new List<Image>();
    public Image defaultImagePrefab;
    public List<PositionData> positions = new List<PositionData>();

    [Header("Audio")]
    public List<SoundData> sounds = new List<SoundData>();
    public AudioSource audioSource;

    [Header("Fade Settings")]
    public float defaultFadeDuration = 0.5f;

    private static DialogueManager instance;
    private static Dictionary<string, Image> activeImages = new Dictionary<string, Image>();
    private DialogueRunner dialogueRunner;

    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Tag as Inventory to persist through scene unloading
            gameObject.tag = "Inventory";

            // Find references if not assigned
            if (dialogueCanvas == null)
                dialogueCanvas = gameObject;

            if (continueButton == null)
                continueButton = GetComponentInChildren<Button>();

            if (graphicRaycaster == null)
                graphicRaycaster = GetComponent<GraphicRaycaster>();

            if (objectivePanel != null)
                objectivePanel.GetComponent<CanvasGroup>().alpha = 0f;

            // Get DialogueRunner
            dialogueRunner = GetComponent<DialogueRunner>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        // Keep sprite tags and sprites lists synchronized
        while (sprites.Count > spriteTags.Count)
        {
            spriteTags.Add("");
        }

        while (spriteTags.Count > sprites.Count)
        {
            spriteTags.RemoveAt(spriteTags.Count - 1);
        }
    }

    private void EnsureContinueButtonInteractable()
    {
        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.enabled = true;
        }
    }

    [YarnCommand("sprite")]
    public static void ShowImage(string spriteTag, string positionName, string prefabName = "")
    {
        if (instance == null)
        {
            Debug.LogError("No DialogueManager instance found in scene!");
            return;
        }

        RemoveImage(positionName);

        // Find the sprite by tag
        Sprite foundSprite = null;
        for (int i = 0; i < instance.spriteTags.Count; i++)
        {
            if (instance.spriteTags[i] == spriteTag)
            {
                foundSprite = instance.sprites[i];
                break;
            }
        }

        if (foundSprite == null)
        {
            Debug.LogError($"No sprite found with tag: {spriteTag}");
            return;
        }

        // Find the position in our list
        Transform positionTransform = null;
        foreach (var positionData in instance.positions)
        {
            if (positionData.positionName == positionName)
            {
                positionTransform = positionData.positionTransform;
                break;
            }
        }

        if (positionTransform == null)
        {
            Debug.LogError($"No position found with name: {positionName}");
            return;
        }

        // Find the appropriate image prefab
        Image imagePrefabToUse = instance.defaultImagePrefab;

        if (!string.IsNullOrEmpty(prefabName))
        {
            foreach (var prefab in instance.imagePrefabs)
            {
                if (prefab.name == prefabName)
                {
                    imagePrefabToUse = prefab;
                    break;
                }
            }
        }

        // Create a new UI Image at the target position
        Image newImage = Instantiate(imagePrefabToUse, positionTransform);
        newImage.sprite = foundSprite;
        newImage.transform.localPosition = Vector3.zero;
        newImage.gameObject.SetActive(true);

        // Track this active image
        activeImages[positionName] = newImage;
    }

    [YarnCommand("removesprite")]
    public static void RemoveImage(string positionName)
    {
        if (activeImages != null && activeImages.TryGetValue(positionName, out Image existingImage))
        {
            if (existingImage != null && existingImage.gameObject != null)
            {
                Destroy(existingImage.gameObject);
            }
            activeImages.Remove(positionName);
        }
    }

    [YarnCommand("fadein")]
    public static void FadeInImage(string positionName, float fadeDuration = -1f)
    {
        if (fadeDuration < 0) fadeDuration = instance.defaultFadeDuration;

        if (activeImages.TryGetValue(positionName, out Image image))
        {
            instance.StartCoroutine(instance.FadeImageRoutine(image, 0f, 1f, fadeDuration));
        }
    }

    [YarnCommand("fadeout")]
    public static void FadeOutImage(string positionName, float fadeDuration = -1f)
    {
        if (fadeDuration < 0) fadeDuration = instance.defaultFadeDuration;

        if (activeImages.TryGetValue(positionName, out Image image))
        {
            instance.StartCoroutine(instance.FadeImageRoutine(image, 1f, 0f, fadeDuration));
        }
    }

    private IEnumerator FadeImageRoutine(Image image, float startAlpha, float targetAlpha, float duration)
    {
        Color color = image.color;
        color.a = startAlpha;
        image.color = color;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            image.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        image.color = color;
    }

    [YarnCommand("progress")]
    public static void AddProgress(string progressID)
    {
        if (SistemaInventario.Instance != null)
        {
            SistemaInventario.Instance.AddProgress(progressID);
        }
    }

    [YarnCommand("removeprogress")]
    public static void RemoveProgress(string progressID)
    {
        if (SistemaInventario.Instance != null)
        {
            SistemaInventario.Instance.RemoveProgress(progressID);
        }
    }

    [YarnFunction("hasprogress")]
    public static bool HasProgress(string progressID)
    {
        if (SistemaInventario.Instance != null)
        {
            return SistemaInventario.Instance.GetGameProgress().Contains(progressID);
        }
        return false;
    }
    [YarnCommand("objective")]
    public static void SetObjective(string objective)
    {
        if (instance.objectiveText != null)
        {
            instance.objectiveText.text = objective;

            if (instance.objectivePanel != null)
            {
                CanvasGroup canvasGroup = instance.objectivePanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }
    }

    [YarnCommand("clearobjective")]
    public static void ClearObjective()
    {
        if (instance.objectiveText != null)
        {
            instance.objectiveText.text = "";

            if (instance.objectivePanel != null)
            {
                CanvasGroup canvasGroup = instance.objectivePanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }
    }

    [YarnCommand("darken")]
    public static void DarkenScreen(float alpha = 0.67f, float duration = 0.5f)
    {
        if (instance.blackScreen != null)
        {
            instance.blackScreen.SetActive(true);
            instance.StartCoroutine(instance.FadeScreenRoutine(alpha, duration));
            instance.EnsureContinueButtonInteractable();
        }
    }

    [YarnCommand("brighten")]
    public static void BrightenScreen(float duration = 0.5f)
    {
        if (instance.blackScreen != null)
        {
            instance.StartCoroutine(instance.FadeScreenRoutine(0f, duration, true));
        }
    }

    [YarnCommand("blackout")]
    public static void Blackout(float duration = 1.0f)
    {
        if (instance.blackScreen != null)
        {
            instance.blackScreen.SetActive(true);
            instance.StartCoroutine(instance.BlackoutRoutine(duration));
            instance.EnsureContinueButtonInteractable();
        }
    }

    private IEnumerator FadeScreenRoutine(float targetAlpha, float duration, bool deactivateAtEnd = false)
    {
        Image blackScreenImage = blackScreen.GetComponent<Image>();
        if (blackScreenImage == null) yield break;

        Color startColor = blackScreenImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            blackScreenImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        blackScreenImage.color = targetColor;

        if (deactivateAtEnd && targetAlpha <= 0f)
        {
            blackScreen.SetActive(false);
        }
    }

    private IEnumerator BlackoutRoutine(float duration)
    {
        Image blackScreenImage = blackScreen.GetComponent<Image>();
        if (blackScreenImage == null) yield break;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color opaque = new Color(0f, 0f, 0f, 1f);

        // Fade to black
        float halfDuration = duration / 2f;
        float elapsedTime = 0f;

        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / halfDuration);
            blackScreenImage.color = Color.Lerp(transparent, opaque, t);
            yield return null;
        }

        blackScreenImage.color = opaque;
        yield return new WaitForSeconds(0.1f);

        // Fade back to transparent
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / halfDuration);
            blackScreenImage.color = Color.Lerp(opaque, transparent, t);
            yield return null;
        }

        blackScreenImage.color = transparent;
        blackScreen.SetActive(false);
        EnableRaycaster();
    }

    [YarnCommand("playsound")]
    public static void PlaySound(string soundName)
    {
        if (instance.audioSource == null) return;

        SoundData foundSound = null;
        foreach (var sound in instance.sounds)
        {
            if (sound.soundName == soundName)
            {
                foundSound = sound;
                break;
            }
        }

        if (foundSound != null)
        {
            instance.audioSource.volume = foundSound.volume;
            instance.audioSource.loop = foundSound.loop;
            instance.audioSource.PlayOneShot(foundSound.soundClip);
        }
        else
        {
            Debug.LogError($"Sound not found: {soundName}");
        }
    }

    [YarnCommand("stopsound")]
    public static void StopSound()
    {
        if (instance.audioSource != null)
        {
            instance.audioSource.Stop();
        }
    }

    [YarnCommand("sceneload")]
    public static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    [YarnCommand("enableraycaster")]
    public static void EnableRaycaster()
    {
        if (instance.graphicRaycaster != null)
            instance.graphicRaycaster.enabled = true;
    }

    [YarnCommand("disableraycaster")]
    public static void DisableRaycaster()
    {
        if (instance.graphicRaycaster != null)
            instance.graphicRaycaster.enabled = false;
    }

    [YarnCommand("wait")]
    public static IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}