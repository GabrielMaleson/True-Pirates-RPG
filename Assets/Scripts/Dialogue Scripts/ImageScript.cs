using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Yarn.Unity;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class StaticImageTagManager : MonoBehaviour
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

    [Tooltip("TextMeshProUGUI component for objectives")]
    private TextMeshProUGUI ObjectiveObj;

    private GameObject objective;

    public GameObject blackScreen;

    private GameObject objectivePanel;

    public Image disappearCharacter; // Reference to the character image that will disappear/reappear

    public Button continueButton;

    public GraphicRaycaster graphicRaycaster;

    [Tooltip("List of Sprites to use for images")]
    public List<Sprite> sprites = new List<Sprite>();

    [Tooltip("List of tags corresponding to each sprite")]
    public List<string> spriteTags = new List<string>();

    [Tooltip("List of Image Prefabs to use for different image types")]
    public List<Image> imagePrefabs = new List<Image>();

    [Tooltip("Default Image Prefab to use if no specific prefab is specified")]
    public Image defaultImagePrefab;

    [Tooltip("List of positions where images can be placed")]
    public List<PositionData> positions = new List<PositionData>();

    [Tooltip("List of sounds that can be played")]
    public List<SoundData> sounds = new List<SoundData>();

    [Tooltip("AudioSource to play sounds")]
    public AudioSource audioSource;

    private static StaticImageTagManager instance;
    private static Dictionary<string, Image> activeImages = new Dictionary<string, Image>();

    private void Awake()
    {
        ObjectiveStart();
        instance = this;
        continueButton = instance.GetComponentInChildren<Button>();
    }

    private void ObjectiveStart()
    {
        objectivePanel = GameObject.FindGameObjectWithTag("Objective Panel");
        objective = GameObject.FindGameObjectWithTag("Objective");
        ObjectiveObj = objective.GetComponent<TextMeshProUGUI>();
        objectivePanel.GetComponent<CanvasGroup>().alpha = 0f;
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

    private void FixDialogueRunner()
    {
        continueButton.interactable = true;
        continueButton.enabled = true;
    }

    [YarnCommand("disappear")]
    public static void DisappearCharacter(string targetName = "")
    {
        GameObject targetObject;

        if (string.IsNullOrEmpty(targetName))
        {
            if (instance.disappearCharacter == null)
            {
                Debug.LogError("No target specified and no default disappearCharacter set!");
                return;
            }
            targetObject = instance.disappearCharacter.gameObject;
        }
        else
        {
            // Try to find the target by name
            targetObject = GameObject.Find(targetName);
            if (targetObject == null)
            {
                Debug.LogError($"No GameObject found with name: {targetName}");
                return;
            }
        }

        Image image = targetObject.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = 0f;
            image.color = color;
        }
        else
        {
            Debug.LogError($"No Image component found on target: {targetObject.name}");
        }
    }

    [YarnCommand("reappear")]
    public static void ReappearCharacter(string targetName = "")
    {
        GameObject targetObject;

        if (string.IsNullOrEmpty(targetName))
        {
            if (instance.disappearCharacter == null)
            {
                Debug.LogError("No target specified and no default disappearCharacter set!");
                return;
            }
            targetObject = instance.disappearCharacter.gameObject;
        }
        else
        {
            // Try to find the target by name
            targetObject = GameObject.Find(targetName);
            if (targetObject == null)
            {
                Debug.LogError($"No GameObject found with name: {targetName}");
                return;
            }
        }

        Image image = targetObject.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = 1f; // Use 1f instead of 255f (alpha is 0-1, not 0-255)
            image.color = color;
        }
        else
        {
            Debug.LogError($"No Image component found on target: {targetObject.name}");
        }
    }

    [YarnCommand("sprite")]
    public static void ShowImage(string spriteTag, string positionName, string prefabName = "")
    {
        if (instance == null)
        {
            Debug.LogError("No StaticImageTagManager instance found in scene!");
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

    [YarnCommand("progress")]
    public static void ProgressUpdate(string text)
    {
        SistemaInventario.Instance.AddProgress(text);
    }

    [YarnCommand("objective")]
    public static void ObjectiveUpdate(string objectivetext)
    {
        if (instance.ObjectiveObj == null)
        {
            Debug.LogError("StaticImageTagManager instance or ObjectiveObj not found!");
            return;
        }
        instance.ObjectiveObj.text = objectivetext;
    }

    // Add these at the top with other class variables
    public event System.Action OnDialogueDarken;
    public event System.Action OnDialogueSuperDarken;
    public event System.Action OnDialogueBrighten;

    // Modify the existing YarnCommand methods to trigger the events:

    [YarnCommand("darken")]
    public static void DialogueDarken()
    {
        instance.blackScreen.SetActive(true);
        instance.StartCoroutine(instance.FadeToBlack());
        instance.FixDialogueRunner();
        instance.OnDialogueDarken?.Invoke(); // Add this line
    }

    [YarnCommand("introdarken")]
    public static void DialogueSuperDarken()
    {
        instance.blackScreen.SetActive(true);
        instance.StartCoroutine(instance.FadeToUltraBlack());
        instance.FixDialogueRunner();
        instance.OnDialogueSuperDarken?.Invoke(); // Add this line
    }

    [YarnCommand("brighten")]
    public static void DialogueBrighten()
    {
        instance.StartCoroutine(instance.FadeToWhite());
        instance.DisableRaycaster();
        instance.OnDialogueBrighten?.Invoke(); // Add this line
    }
    private IEnumerator FadeToBlack()
    {
        Image blackScreenImage = blackScreen.GetComponent<Image>();
        Color currentColor = blackScreenImage.color;

        Color targetColor = new Color(currentColor.r, currentColor.g, currentColor.b, 170f / 255);

        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Lerp from current color to semi-transparent black
            blackScreenImage.color = Color.Lerp(currentColor, targetColor, t);
            yield return null;
        }
        instance.EnableRaycaster();
    }
    private IEnumerator FadeToUltraBlack()
    {
        Image blackScreenImage = blackScreen.GetComponent<Image>();
        Color currentColor = blackScreenImage.color;

        Color targetColor = new Color(currentColor.r, currentColor.g, currentColor.b, 170f / 255);

        float duration = 1.0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Lerp from current color to semi-transparent black
            blackScreenImage.color = Color.Lerp(currentColor, targetColor, t);
            yield return null;
        }
        instance.EnableRaycaster();
    }

    private IEnumerator FadeToWhite()
    {
        Image blackScreenImage = blackScreen.GetComponent<Image>();
        Color currentColor = blackScreenImage.color;
        Color targetColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            blackScreenImage.color = Color.Lerp(currentColor, targetColor, t);
            yield return null;
        }
        instance.blackScreen.SetActive(false);
    }

    [YarnCommand("sceneload")]
    public static void DialogueScene(string scenetext)
    {
        SceneManager.LoadScene(scenetext);
    }

    [YarnCommand("blackout")]
    public static void Blackout(float duration = 1.0f)
    {
        instance.blackScreen.SetActive(true);
        instance.StartCoroutine(instance.BlackoutEffect(duration));
        instance.FixDialogueRunner();
    }

    private IEnumerator BlackoutEffect(float duration)
    {
        Image blackScreenImage = blackScreen.GetComponent<Image>();
        Color startColor = new Color(0f, 0f, 0f, 0f); // Start completely transparent
        Color targetColor = new Color(0f, 0f, 0f, 1f); // End completely opaque black

        // Fade to black (first half of duration)
        float elapsedTime = 0f;
        float halfDuration = duration / 2f;

        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / halfDuration);
            blackScreenImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // Ensure we reach full black
        blackScreenImage.color = targetColor;

        // Wait at full black for a moment (optional, you can remove this if you want immediate brightening)
        yield return new WaitForSeconds(0.1f);

        // Fade back to transparent (second half of duration)
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / halfDuration);
            blackScreenImage.color = Color.Lerp(targetColor, startColor, t);
            yield return null;
        }

        // Ensure we reach full transparency
        blackScreenImage.color = startColor;
        blackScreen.SetActive(false);
        instance.DisableRaycaster();
    }

    [YarnCommand("fadein")]
    public static void FadeInImage(string positionName, float fadeDuration = 1.0f)
    {
        if (activeImages.TryGetValue(positionName, out Image image))
        {
            instance.StartCoroutine(instance.FadeImage(image, 0f, 1f, fadeDuration));
        }
    }

    private IEnumerator FadeImage(Image image, float startAlpha, float targetAlpha, float duration)
    {
        // Store original color
        Color originalColor = image.color;

        // Set starting alpha
        originalColor.a = startAlpha;
        image.color = originalColor;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Lerp the alpha value
            Color newColor = image.color;
            newColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            image.color = newColor;

            yield return null;
        }

        // Ensure final alpha is set exactly
        Color finalColor = image.color;
        finalColor.a = targetAlpha;
        image.color = finalColor;
    }

    [YarnCommand("playsound")]
    public static void PlaySound(string soundName)
    {
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

    public void EnableRaycaster()
    {
        graphicRaycaster.enabled = true;
    }

    public void DisableRaycaster()
    {
        graphicRaycaster.enabled = false;
    }


    [YarnCommand("finalereappear")]
    public static IEnumerator FinalReappearCharacter(string targetName = "")
    {
        // Wait for 5 seconds
        yield return new WaitForSeconds(5f);

        GameObject targetObject;

        if (string.IsNullOrEmpty(targetName))
        {
            if (instance.disappearCharacter == null)
            {
                Debug.LogError("No target specified and no default disappearCharacter set!");
                yield break;
            }
            targetObject = instance.disappearCharacter.gameObject;
        }
        else
        {
            // Try to find the target by name
            targetObject = GameObject.Find(targetName);
            if (targetObject == null)
            {
                Debug.LogError($"No GameObject found with name: {targetName}");
                yield break;
            }
        }

        Image image = targetObject.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = 1f; // Use 1f instead of 255f (alpha is 0-1, not 0-255)
            image.color = color;
        }
        else
        {
            Debug.LogError($"No Image component found on target: {targetObject.name}");
        }
    }

}