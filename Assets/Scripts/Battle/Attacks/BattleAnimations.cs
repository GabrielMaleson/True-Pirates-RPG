using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Battle Animation", menuName = "RPG/Battle Animation")]
public class BattleAnimationData : ScriptableObject
{
    [Header("Animation Text")]
    public string attackTextFormat = "{user} uses {attack}!"; // Format with {user} and {attack} placeholders
    public float textDisplayDuration = 1.5f;
    public Color textColor = Color.white;
    public float textSize = 36f;

    [Header("Text Background")]
    public bool useBackground = true; // Whether to use a background image
    public float backgroundPadding = 20f; // Padding around the text
    public Color backgroundColor = new Color(0, 0, 0, 0.7f); // Semi-transparent black

    [Header("User Animation")]
    public AnimationClip userAnimation;
    public float userAnimationDelay = 0f; // Delay before user animation starts

    [Header("Target Overlay Animation")]
    public GameObject targetOverlayPrefab; // Animation that plays on top of target
    public float overlayDuration = 0.5f;
    public Vector3 overlayOffset = Vector3.zero;

    [Header("Hit Effect")]
    public GameObject hitVFX;
    public AudioClip hitSound;
    public float hitDelay = 0.5f; // Delay after user animation before hit effect
    public float hitEffectDuration = 0.5f;

    [Header("Target Shake")]
    public bool shakeTarget = true;
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.2f;

    [Header("Timing")]
    public float preDelay = 0.2f;
    public float postDelay = 0.3f;

    [Header("UI References")]
    private static TextMeshProUGUI animationText; // Static reference to the animation text UI
    private static Image textBackground; // Static reference to the background image
    private static MonoBehaviour coroutineRunner; // Static reference to a MonoBehaviour that can run coroutines

    // Initialize the static references (call this from CombatSystem or UIManager)
    public static void Initialize(TextMeshProUGUI textUI, MonoBehaviour runner)
    {
        animationText = textUI;
        coroutineRunner = runner;

        // Get the background image (assumed to be the parent or a sibling)
        if (animationText != null)
        {
            // Try to get background from parent
            textBackground = animationText.transform.parent?.GetComponent<Image>();
        }
    }

    public IEnumerator PlayAnimation(PartyMemberState user, List<PartyMemberState> targets, System.Action onComplete = null)
    {
        // Pre-delay
        yield return new WaitForSeconds(preDelay);

        // 1. Show attack text (like Final Fantasy)
        if (animationText != null && coroutineRunner != null)
        {
            string displayText = attackTextFormat
                .Replace("{user}", user.CharacterName)
                .Replace("{attack}", name);

            animationText.text = displayText;
            animationText.color = textColor;
            animationText.fontSize = textSize;

            // Toggle on the background first
            if (textBackground != null && useBackground)
            {
                textBackground.color = backgroundColor;
                textBackground.gameObject.SetActive(true);
            }

            // Then show the text
            animationText.gameObject.SetActive(true);

            // Update background size to fit text
            UpdateBackgroundSize();

            // Text stays for duration - use coroutine runner
            coroutineRunner.StartCoroutine(HideTextAfterDelay(textDisplayDuration));
        }

        // 2. User stands in place and plays animation using CharacterComponent
        if (userAnimation != null && user.transform != null)
        {
            // Wait for user animation delay if specified
            if (userAnimationDelay > 0)
                yield return new WaitForSeconds(userAnimationDelay);

            CharacterComponent characterComp = user.transform.GetComponent<CharacterComponent>();
            if (characterComp != null)
            {
                // Ensure animator controller is set before playing
                characterComp.PrepareForBattle();
                characterComp.PlayAnimation(userAnimation);
            }
            else
            {
                Animator animator = user.transform.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Play(userAnimation.name);
                }
            }
        }

        // 3. Wait for hit delay (allow user animation to play)
        yield return new WaitForSeconds(hitDelay);

        // 4. For each target, play overlay animation and hit effect
        foreach (var target in targets)
        {
            if (target == null || target.transform == null) continue;

            Transform targetTransform = target.transform;

            // Play overlay animation on target (separate GameObject that appears on top)
            if (targetOverlayPrefab != null)
            {
                GameObject overlay = Object.Instantiate(targetOverlayPrefab, targetTransform.position + overlayOffset, Quaternion.identity);
                // Parent to target to follow if needed
                overlay.transform.SetParent(targetTransform);

                // Destroy after duration
                Object.Destroy(overlay, overlayDuration);
            }

            // Shake target (if enabled) - use coroutine runner
            if (shakeTarget && coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(ShakeTarget(targetTransform, shakeIntensity, shakeDuration));
            }

            // Hit VFX
            if (hitVFX != null)
            {
                GameObject hit = Object.Instantiate(hitVFX, targetTransform.position, Quaternion.identity);
                Object.Destroy(hit, hitEffectDuration);
            }

            // Hit sound — reproduzido via SFXManager (2D) para garantir audibilidade
            if (hitSound != null)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.Play(hitSound);
                else
                    AudioSource.PlayClipAtPoint(hitSound, Camera.main?.transform.position ?? Vector3.zero);
            }
        }

        // 5. Wait for post-delay
        yield return new WaitForSeconds(postDelay);

        // 6. Call completion callback
        onComplete?.Invoke();
    }

    private void UpdateBackgroundSize()
    {
        if (textBackground == null || animationText == null) return;

        // Get the text's preferred size
        float textWidth = animationText.preferredWidth;
        float textHeight = animationText.preferredHeight;

        // Set the background size with padding
        RectTransform bgRect = textBackground.GetComponent<RectTransform>();
        if (bgRect != null)
        {
            bgRect.sizeDelta = new Vector2(textWidth + backgroundPadding, textHeight + backgroundPadding + 500);
        }

        // Ensure background is behind text
        textBackground.transform.SetSiblingIndex(animationText.transform.GetSiblingIndex() - 1);
    }

    private IEnumerator HideTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Hide text first
        if (animationText != null)
        {
            animationText.gameObject.SetActive(false);
        }

        // Then hide background
        if (textBackground != null && useBackground)
        {
            textBackground.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShakeTarget(Transform target, float intensity, float duration)
    {
        if (target == null) yield break;

        Vector3 originalPosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            target.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPosition;
    }
}