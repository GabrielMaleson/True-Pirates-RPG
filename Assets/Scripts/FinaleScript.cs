using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FinaleSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image blackOverlay; // Full-screen black overlay
    [SerializeField] private TextMeshProUGUI creditsText; // Credits text

    [Header("Black Screen Settings")]
    [SerializeField] private float blackScreenHoldTime = 2f; // Time to hold black screen
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Slide Up Settings")]
    [SerializeField] private float slideUpDuration = 1f; // Duration of slide up animation

    [Header("Credits Settings")]
    [SerializeField] private string creditsContent = ""; // Credits text content
    [SerializeField] private float scrollSpeed = 50f; // How fast credits scroll
    [SerializeField] private float creditsStartDelay = 0.5f; // Delay before credits start
    [SerializeField] private float creditsEndPosition = 1000f; // Y position where credits end
    [SerializeField] private bool loopCredits = false; // Loop credits when done
    [SerializeField] private float loopResetDelay = 2f; // Delay before looping

    [Header("Optional Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent onFinaleComplete;
    [SerializeField] private UnityEngine.Events.UnityEvent onCreditsLoop;

    // Private variables
    private RectTransform overlayRect;
    private RectTransform creditsRect;
    private bool isFinalePlaying = false;
    private bool isRolling = false;
    private bool isWaitingForReset = false;
    private float creditsStartYPosition;
    private Coroutine currentCreditsCoroutine;

    private void Awake()
    {
        // Setup black overlay
        if (blackOverlay == null)
        {
            Debug.LogError("Black overlay Image not assigned!");
            return;
        }

        overlayRect = blackOverlay.GetComponent<RectTransform>();

        // Ensure overlay is full screen
        blackOverlay.rectTransform.anchorMin = Vector2.zero;
        blackOverlay.rectTransform.anchorMax = Vector2.one;
        blackOverlay.rectTransform.offsetMin = Vector2.zero;
        blackOverlay.rectTransform.offsetMax = Vector2.zero;

        // Setup credits text
        if (creditsText == null)
        {
            Debug.LogError("Credits Text not assigned!");
            return;
        }

        creditsRect = creditsText.GetComponent<RectTransform>();

        // Set initial credits text
        if (!string.IsNullOrEmpty(creditsContent))
        {
            SetCreditsText(creditsContent);
        }

        // Store starting position
        creditsStartYPosition = creditsRect.anchoredPosition.y;

        // Initially hide credits (will be shown after slide up)
        creditsText.gameObject.SetActive(false);

        // Ensure black overlay is visible at start
        SetOverlayFill(1f);
        StartFinale();
    }

    // Set the fill amount of the overlay (0 = invisible, 1 = full screen black)
    private void SetOverlayFill(float fillAmount)
    {
        if (blackOverlay == null) return;

        // For bottom-to-top fill, we adjust the anchorMin.y
        // At fill=0: anchorMin.y = 1 (top) - overlay is invisible (at top)
        // At fill=1: anchorMin.y = 0 (bottom) - overlay covers entire screen
        overlayRect.anchorMin = new Vector2(0, 1 - fillAmount);
        overlayRect.anchorMax = Vector2.one;

        // Reset offsets to ensure full width
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
    }

    public void StartFinale()
    {
        if (isFinalePlaying)
        {
            Debug.LogWarning("Finale is already playing!");
            return;
        }

        StartCoroutine(FinaleSequence());
    }

    private IEnumerator FinaleSequence()
    {
        isFinalePlaying = true;

        // Step 1: Ensure screen is fully black
        SetOverlayFill(1f);

        // Step 2: Hold on black screen
        yield return new WaitForSeconds(blackScreenHoldTime);

        // Step 3: Slide the black screen up
        yield return StartCoroutine(SlideBlackScreenUp());

        // Step 4: Show and start credits
        yield return new WaitForSeconds(creditsStartDelay);

        // Show credits text
        creditsText.gameObject.SetActive(true);

        // Reset credits position to start
        Vector2 anchoredPosition = creditsRect.anchoredPosition;
        anchoredPosition.y = creditsStartYPosition;
        creditsRect.anchoredPosition = anchoredPosition;

        // Start credits roll
        StartCreditsRoll();

        // Step 5: Wait for credits to complete
        yield return StartCoroutine(WaitForCreditsComplete());

        // Step 6: Finale complete
        onFinaleComplete?.Invoke();
        isFinalePlaying = false;
    }

    private IEnumerator SlideBlackScreenUp()
    {
        if (overlayRect == null)
        {
            Debug.LogWarning("Cannot slide - overlayRect is null");
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < slideUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideUpDuration;
            float curveValue = slideCurve.Evaluate(t);

            // Slide the black screen upward by adjusting anchor positions
            // Start with full screen (anchorMin.y = 0, anchorMax.y = 1)
            // End with screen pushed above view (anchorMin.y = 1, anchorMax.y = 1)
            float newAnchorMinY = Mathf.Lerp(0f, 1f, curveValue);
            float newAnchorMaxY = Mathf.Lerp(1f, 1f, curveValue);

            overlayRect.anchorMin = new Vector2(0, newAnchorMinY);
            overlayRect.anchorMax = new Vector2(1, newAnchorMaxY);

            // Ensure offsets are zero for full width
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            yield return null;
        }

        // Ensure final position is completely off-screen
        overlayRect.anchorMin = new Vector2(0, 1);
        overlayRect.anchorMax = new Vector2(1, 1);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
    }

    private void StartCreditsRoll()
    {
        if (currentCreditsCoroutine != null)
        {
            StopCoroutine(currentCreditsCoroutine);
        }

        currentCreditsCoroutine = StartCoroutine(CreditsRollRoutine());
    }

    private IEnumerator CreditsRollRoutine()
    {
        isRolling = true;
        isWaitingForReset = false;

        while (isRolling && !isWaitingForReset)
        {
            // Move credits upward
            Vector2 anchoredPosition = creditsRect.anchoredPosition;
            anchoredPosition.y += scrollSpeed * Time.deltaTime;
            creditsRect.anchoredPosition = anchoredPosition;

            // Check if credits have reached the end
            if (creditsRect.anchoredPosition.y >= creditsEndPosition)
            {
                isRolling = false;

                if (loopCredits)
                {
                    yield return StartCoroutine(ResetAndLoopCredits());
                }
                else
                {
                    // Credits complete
                    break;
                }
            }

            yield return null;
        }

        currentCreditsCoroutine = null;
    }

    private IEnumerator ResetAndLoopCredits()
    {
        isWaitingForReset = true;
        yield return new WaitForSeconds(loopResetDelay);

        // Reset position to start
        Vector2 anchoredPosition = creditsRect.anchoredPosition;
        anchoredPosition.y = creditsStartYPosition;
        creditsRect.anchoredPosition = anchoredPosition;

        isRolling = true;
        isWaitingForReset = false;
        onCreditsLoop?.Invoke();

        // Continue rolling
        StartCreditsRoll();
    }

    private IEnumerator WaitForCreditsComplete()
    {
        // Wait until credits are no longer rolling and not in loop
        while (isRolling || isWaitingForReset)
        {
            yield return null;
        }

        // Small extra delay after credits complete
        yield return new WaitForSeconds(0.5f);
    }

    public void StopCreditsRoll()
    {
        isRolling = false;
        isWaitingForReset = false;

        if (currentCreditsCoroutine != null)
        {
            StopCoroutine(currentCreditsCoroutine);
            currentCreditsCoroutine = null;
        }
    }

    public void ResetCredits()
    {
        StopCreditsRoll();

        Vector2 anchoredPosition = creditsRect.anchoredPosition;
        anchoredPosition.y = creditsStartYPosition;
        creditsRect.anchoredPosition = anchoredPosition;
    }

    public void SetCreditsText(string text)
    {
        if (creditsText != null)
        {
            creditsText.text = text;
        }
    }

    public void SetCreditsText(string[] lines)
    {
        if (creditsText != null)
        {
            creditsText.text = string.Join("\n", lines);
        }
    }

    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    public void SetEndPosition(float position)
    {
        creditsEndPosition = position;
    }

    // Auto-calculate end position based on text height
    public void AutoSetEndPosition(float offset = 0f)
    {
        if (creditsText != null)
        {
            float textHeight = creditsText.preferredHeight;
            creditsEndPosition = textHeight + offset;
        }
    }

    // Public method to manually trigger finale (for button calls)
    public void PlayFinale()
    {
        StartFinale();
    }

    // Public method to stop finale early
    public void StopFinale()
    {
        isFinalePlaying = false;
        StopAllCoroutines();
        StopCreditsRoll();
    }

    // Reset all visuals (useful for replaying or testing)
    public void ResetFinale()
    {
        StopAllCoroutines();
        StopCreditsRoll();

        // Reset black overlay to full screen
        if (overlayRect != null)
        {
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            SetOverlayFill(1f);
        }

        // Reset credits
        if (creditsRect != null)
        {
            Vector2 anchoredPosition = creditsRect.anchoredPosition;
            anchoredPosition.y = creditsStartYPosition;
            creditsRect.anchoredPosition = anchoredPosition;
        }

        // Hide credits until needed
        if (creditsText != null)
        {
            creditsText.gameObject.SetActive(false);
        }

        isFinalePlaying = false;
    }

    // Check if finale is currently playing
    public bool IsFinalePlaying()
    {
        return isFinalePlaying;
    }

    // Check if credits are currently rolling
    public bool IsCreditsRolling()
    {
        return isRolling;
    }
}