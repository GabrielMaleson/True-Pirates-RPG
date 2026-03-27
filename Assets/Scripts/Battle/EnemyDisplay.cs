using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ── Default container (shown when enemy is NOT attacking) ──────────────────
    [Header("Default Portrait")]
    public GameObject defaultContainer;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public GameObject defeatedOverlay;

    // ── Selected container (shown ONLY while the enemy is executing an attack) ─
    [Header("Selected Portrait")]
    public GameObject selectedContainer;
    public Image selectedPortraitImage;
    public TextMeshProUGUI selectedHpText;
    public Image selectedHealthBar;

    // ── Damage indicator ───────────────────────────────────────────────────────
    [Header("Damage Indicator")]
    public TextMeshProUGUI damageText;

    // ── Targeting ──────────────────────────────────────────────────────────────
    [Header("Targeting")]
    public Image portraitBackground;  // alpha 75 % normally, 100 % when targetable
    public Outline targetableOutline; // Outline component on the background — pulsates on hover

    // ── Legacy (kept so existing prefab references don't break) ───────────────
    [Header("Target Button (Legacy)")]
    public GameObject targetButtonObject;
    public TargetButton targetButton;

    // ── Private state ──────────────────────────────────────────────────────────
    private PartyMemberState memberState;
    private GameObject characterVisualObject;
    private TargetSelector targetSelector;
    private System.Action onClickCallback;
    private bool isTargetable;
    private Coroutine pulseCoroutine;
    private Coroutine damageCoroutine;
    private int hoverCounter; // Track how many children are being hovered

    private const float DefaultBgAlpha = 0.75f;
    private const float DefaultBgColor = 0.56f;

    // ──────────────────────────────────────────────────────────────────────────

    public void Initialize(PartyMemberState state, GameObject visualObject)
    {
        memberState = state;
        characterVisualObject = visualObject;

        if (nameText != null) nameText.text = state.CharacterName;

        Sprite portrait = null;
        if (state.template != null)
            portrait = state.template.battlePortrait != null
                ? state.template.battlePortrait
                : state.template.partyIcon;

        ApplyPortrait(portraitImage, portrait);
        ApplyPortrait(selectedPortraitImage, portrait);

        InitBar(healthBar);
        InitBar(selectedHealthBar);

        SetSelected(false);
        SetBackgroundAlpha(DefaultBgAlpha);
        SetBackgroundColor(DefaultBgColor);

        if (damageText != null) damageText.gameObject.SetActive(false);
        if (targetableOutline != null) { targetableOutline.enabled = false; targetableOutline.effectDistance = new Vector2(2f, 2f); }
        if (targetButtonObject != null) targetButtonObject.SetActive(false);

        UpdateDisplay();

        // Ensure the root rect catches clicks everywhere (including over child images with Raycast Target off)
        Image rootHitbox = GetComponent<Image>();
        if (rootHitbox == null) rootHitbox = gameObject.AddComponent<Image>();
        rootHitbox.color = Color.clear;
        rootHitbox.raycastTarget = true;

        // Make sure all children that need to trigger hover have raycast targets enabled
        SetupRaycastTargetsForChildren();

        if (characterVisualObject != null)
        {
            targetSelector = characterVisualObject.GetComponent<TargetSelector>();
            if (targetSelector != null)
                targetSelector.SetEnemyUI(this);
        }
    }

    // ── Turn-state ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Swaps between Default and Selected containers.
    /// Called by CombatUIManager via onAttackStarted / onAttackFinished —
    /// so enemies only show "selected" while actively executing an attack.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (defaultContainer != null) defaultContainer.SetActive(!selected);
        if (selectedContainer != null) selectedContainer.SetActive(selected);
    }

    // ── Display ────────────────────────────────────────────────────────────────

    public void UpdateDisplay()
    {
        if (memberState == null) return;

        float hpPct = Mathf.Clamp01((float)memberState.currentHP / memberState.MaxHP);
        string hpStr = $"{memberState.currentHP}/{memberState.MaxHP}";

        if (hpText != null) hpText.text = hpStr;
        if (healthBar != null) healthBar.fillAmount = hpPct;
        if (selectedHpText != null) selectedHpText.text = hpStr;
        if (selectedHealthBar != null) selectedHealthBar.fillAmount = hpPct;
    }

    // ── Targeting ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by TargetSelector when this enemy becomes a valid target.
    /// Brightens background and makes the whole card clickable.
    /// </summary>
    public void ShowTargetButton(TargetSelector selector)
    {
        isTargetable = true;
        onClickCallback = selector.OnTargetSelected;
        SetBackgroundAlpha(1f);
        SetBackgroundColor(0.809f);
        if (targetButtonObject != null) targetButtonObject.SetActive(false); // legacy hidden
    }

    public void HideTargetButton()
    {
        isTargetable = false;
        onClickCallback = null;
        StopPulse();
        SetBackgroundAlpha(DefaultBgAlpha);
        SetBackgroundColor(DefaultBgColor);
        if (targetButtonObject != null) targetButtonObject.SetActive(false);
    }

    // IPointerClickHandler
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTargetable && onClickCallback != null)
            onClickCallback.Invoke();
    }

    // IPointerEnterHandler — hovering starts the pulsating outline
    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverCounter++;

        if (!isTargetable || targetableOutline == null) return;

        // Only start the outline if this is the first hover (no children already being hovered)
        if (hoverCounter >= 1)
        {
            targetableOutline.enabled = true;
            if (pulseCoroutine == null)
                pulseCoroutine = StartCoroutine(PulseOutline());
        }
    }

    // IPointerExitHandler — leaving stops and hides the outline
    public void OnPointerExit(PointerEventData eventData)
    {
        hoverCounter--;

        // Only stop the outline when no children are being hovered
        if (hoverCounter <= 0)
        {
            hoverCounter = 0;
            StopPulse();
        }
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        if (targetableOutline != null)
        {
            targetableOutline.enabled = false;
            targetableOutline.effectDistance = new Vector2(2f, 2f);
        }
    }

    private IEnumerator PulseOutline()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float size = Mathf.Lerp(2f, 5f, t);
            targetableOutline.effectDistance = new Vector2(size, size);
            yield return null;
        }
    }

    // ── Helper to set up children for event propagation ───────────────────────

    private void SetupRaycastTargetsForChildren()
    {
        // Get all Image components in children
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            // Skip the root clear image and any images that should not block clicks
            if (img == GetComponent<Image>()) continue;

            // Enable raycast target for all images to ensure they capture hover events
            img.raycastTarget = true;
        }

        // Also enable raycast for TextMeshProUGUI components
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in allTexts)
        {
            text.raycastTarget = true;
        }

        // Make sure all child GameObjects have a GraphicRaycaster-compatible component
        // If any child doesn't have an image but needs to be clickable, add one
        RectTransform[] allChildren = GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform child in allChildren)
        {
            // Skip the root object
            if (child == transform) continue;

            // If this child doesn't have any Graphic component, add a small transparent image
            Graphic graphic = child.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image transparentImage = child.gameObject.AddComponent<Image>();
                transparentImage.color = Color.clear;
                transparentImage.raycastTarget = true;
            }
        }
    }

    // ── Damage indicator ───────────────────────────────────────────────────────

    public void ShowDamage(int amount)
    {
        if (damageText == null) return;
        if (damageCoroutine != null) StopCoroutine(damageCoroutine);
        damageCoroutine = StartCoroutine(DamagePopRoutine(amount));
        StartCoroutine(ShakePortrait());
    }

    private IEnumerator DamagePopRoutine(int amount)
    {
        damageText.text = $"-{amount}";
        damageText.color = new Color(1f, 0.2f, 0.2f, 1f);
        damageText.gameObject.SetActive(true);

        RectTransform rt = damageText.rectTransform;
        Vector2 start = rt.anchoredPosition;
        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = start + Vector2.down * (50f * t);
            float alpha = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
            Color c = damageText.color; c.a = alpha; damageText.color = c;
            yield return null;
        }

        damageText.gameObject.SetActive(false);
        rt.anchoredPosition = start;
        damageCoroutine = null;
    }

    private IEnumerator ShakePortrait()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) yield break;
        Vector2 origin = rt.anchoredPosition;
        float duration = 0.3f;
        float elapsed = 0f;
        float magnitude = 5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rt.anchoredPosition = origin + new Vector2(
                Random.Range(-magnitude, magnitude),
                Random.Range(-magnitude * 0.4f, magnitude * 0.4f));
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ── Defeated ───────────────────────────────────────────────────────────────

    public void SetDefeated()
    {
        if (defeatedOverlay != null) defeatedOverlay.SetActive(true);

        Image bg = GetComponent<Image>();
        if (bg != null) bg.color = Color.gray;

        GrayOutPortrait(portraitImage);
        GrayOutPortrait(selectedPortraitImage);

        if (healthBar != null) healthBar.fillAmount = 0f;
        if (selectedHealthBar != null) selectedHealthBar.fillAmount = 0f;

        HideTargetButton();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void InitBar(Image bar)
    {
        if (bar == null) return;
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = 0;
    }

    private static void ApplyPortrait(Image img, Sprite sprite)
    {
        if (img == null) return;
        if (sprite != null) { img.sprite = sprite; img.gameObject.SetActive(true); }
        else img.gameObject.SetActive(false);
    }

    private void SetBackgroundAlpha(float alpha)
    {
        if (portraitBackground == null) return;
        Color c = portraitBackground.color;
        c.a = alpha;
        portraitBackground.color = c;
    }
    private void SetBackgroundColor(float color)
    {
        if (portraitBackground == null) return;
        Color c = portraitBackground.color;
        c.b = color;
        portraitBackground.color = c;
    }

    private static void GrayOutPortrait(Image img)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = 0.5f;
        img.color = c;
    }
}