using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Battle Animation", menuName = "RPG/Battle Animation")]
public class BattleAnimationData : ScriptableObject
{
    [Header("Animation Text")]
    public string attackTextFormat = "{user} uses {attack}!"; // Format with {user} and {attack} placeholders
    public float textDisplayDuration = 1.5f;
    public Color textColor = Color.white;
    public float textSize = 36f;

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
    private static MonoBehaviour coroutineRunner; // Static reference to a MonoBehaviour that can run coroutines

    // Initialize the static references (call this from CombatSystem or UIManager)
    public static void Initialize(TextMeshProUGUI textUI, MonoBehaviour runner)
    {
        animationText = textUI;
        coroutineRunner = runner;
    }

    public IEnumerator PlayAnimation(PartyMemberState user, List<PartyMemberState> targets, System.Action onComplete = null)
    {
        // Pre-delay
        yield return new WaitForSeconds(preDelay);

        // 1. Show attack text
        if (animationText != null && coroutineRunner != null)
        {
            string displayText = attackTextFormat
                .Replace("{user}", user.CharacterName)
                .Replace("{attack}", name);

            animationText.text = displayText;
            animationText.color = textColor;
            animationText.fontSize = textSize;
            animationText.gameObject.SetActive(true);

            coroutineRunner.StartCoroutine(HideTextAfterDelay(textDisplayDuration));
        }

        // 2. User stands in place and plays animation using CharacterComponent
        if (userAnimation != null && user.transform != null)
        {
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

            // Hit sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, targetTransform.position);
            }
        }

        // 5. Wait for post-delay
        yield return new WaitForSeconds(postDelay);

        // 6. Call completion callback
        onComplete?.Invoke();
    }

    private IEnumerator HideTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animationText != null)
        {
            animationText.gameObject.SetActive(false);
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