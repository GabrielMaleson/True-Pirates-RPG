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

    private const float DefaultBgAlpha = 0.75f;

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

        if (targetableOutline  != null) { targetableOutline.enabled = false; targetableOutline.effectDistance = new Vector2(2f, 2f); }
        if (targetButtonObject != null) targetButtonObject.SetActive(false);

        UpdateDisplay();

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
        if (defaultContainer  != null) defaultContainer.SetActive(!selected);
        if (selectedContainer != null) selectedContainer.SetActive(selected);
    }

    // ── Display ────────────────────────────────────────────────────────────────

    public void UpdateDisplay()
    {
        if (memberState == null) return;

        float hpPct  = Mathf.Clamp01((float)memberState.currentHP / memberState.MaxHP);
        string hpStr = $"{memberState.currentHP}/{memberState.MaxHP}";

        if (hpText            != null) hpText.text                  = hpStr;
        if (healthBar         != null) healthBar.fillAmount          = hpPct;
        if (selectedHpText    != null) selectedHpText.text           = hpStr;
        if (selectedHealthBar != null) selectedHealthBar.fillAmount  = hpPct;
    }

    // ── Targeting ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by TargetSelector when this enemy becomes a valid target.
    /// Brightens background and makes the whole card clickable.
    /// </summary>
    public void ShowTargetButton(TargetSelector selector)
    {
        isTargetable    = true;
        onClickCallback = selector.OnTargetSelected;
        SetBackgroundAlpha(1f);
        if (targetButtonObject != null) targetButtonObject.SetActive(false); // legacy hidden
    }

    public void HideTargetButton()
    {
        isTargetable    = false;
        onClickCallback = null;
        StopPulse();
        SetBackgroundAlpha(DefaultBgAlpha);
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
        if (!isTargetable || targetableOutline == null) return;
        targetableOutline.enabled = true;
        pulseCoroutine = StartCoroutine(PulseOutline());
    }

    // IPointerExitHandler — leaving stops and hides the outline
    public void OnPointerExit(PointerEventData eventData)
    {
        StopPulse();
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (targetableOutline != null) { targetableOutline.enabled = false; targetableOutline.effectDistance = new Vector2(2f, 2f); }
    }

    private IEnumerator PulseOutline()
    {
        while (true)
        {
            float t    = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float size = Mathf.Lerp(2f, 5f, t);
            targetableOutline.effectDistance = new Vector2(size, size);
            yield return null;
        }
    }

    // ── Defeated ───────────────────────────────────────────────────────────────

    public void SetDefeated()
    {
        if (defeatedOverlay != null) defeatedOverlay.SetActive(true);

        Image bg = GetComponent<Image>();
        if (bg != null) bg.color = Color.gray;

        GrayOutPortrait(portraitImage);
        GrayOutPortrait(selectedPortraitImage);

        if (healthBar         != null) healthBar.fillAmount         = 0f;
        if (selectedHealthBar != null) selectedHealthBar.fillAmount = 0f;

        HideTargetButton();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void InitBar(Image bar)
    {
        if (bar == null) return;
        bar.type        = Image.Type.Filled;
        bar.fillMethod  = Image.FillMethod.Horizontal;
        bar.fillOrigin  = 0;
    }

    private static void ApplyPortrait(Image img, Sprite sprite)
    {
        if (img == null) return;
        if (sprite != null) { img.sprite = sprite; img.gameObject.SetActive(true); }
        else                  img.gameObject.SetActive(false);
    }

    private void SetBackgroundAlpha(float alpha)
    {
        if (portraitBackground == null) return;
        Color c = portraitBackground.color;
        c.a = alpha;
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
