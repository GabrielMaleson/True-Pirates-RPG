using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PartyMemberStatsDisplay : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Basic Info")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;

    [Header("HP Display")]
    public Image hpBarFill;
    public TextMeshProUGUI hpText;

    [Header("AP Display")]
    public Image apBarFill;
    public TextMeshProUGUI apText;

    [Header("Stats")]
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;

    [Header("EXP Display")]
    public Image expBarFill;
    public TextMeshProUGUI expText;

    [Header("Equipment Display")]
    public TextMeshProUGUI accessoryText;
    public TextMeshProUGUI armorText;

    [Header("Colors")]
    public Color hpColor = Color.red;
    public Color apColor = Color.blue;
    public Color expColor = Color.green;

    private PartyMemberState memberState;

    // ── Targeting (use item / equip) ──────────────────────────────────────────
    private System.Action<PartyMemberState> _onSelectedCallback;
    private bool _isTargetable = false;
    private Outline _outline;
    private Coroutine _pulseCoroutine;

    public void SetTargetable(bool targetable, System.Action<PartyMemberState> onSelected)
    {
        _isTargetable        = targetable;
        _onSelectedCallback  = onSelected;

        if (_outline == null)
        {
            _outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            _outline.effectColor = Color.yellow;
        }
        _outline.enabled = false;

        if (!targetable && _pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isTargetable || _outline == null) return;
        _outline.enabled = true;
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(PulseOutline());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_outline == null) return;
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        _outline.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isTargetable || _onSelectedCallback == null) return;
        _onSelectedCallback.Invoke(memberState);
    }

    private IEnumerator PulseOutline()
    {
        while (true)
        {
            float t    = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float size = Mathf.Lerp(2f, 5f, t);
            _outline.effectDistance = new Vector2(size, size);
            yield return null;
        }
    }

    public void Initialize(PartyMemberState state)
    {
        memberState = state;

        // Set portrait
        if (portraitImage != null)
        {
            if (state.BattlePortrait != null)
                portraitImage.sprite = state.BattlePortrait;
            else if (state.PartyIcon != null)
                portraitImage.sprite = state.PartyIcon;
        }

        // Set bar colors
        if (hpBarFill != null) hpBarFill.color = hpColor;
        if (apBarFill != null) apBarFill.color = apColor;
        if (expBarFill != null) expBarFill.color = expColor;

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (memberState == null) return;

        // Basic info
        if (nameText != null)
            nameText.text = memberState.CharacterName;

        if (levelText != null)
            levelText.text = $"Lv. {memberState.level}";

        // HP
        float hpPercent = (float)memberState.currentHP / memberState.MaxHP;
        if (hpBarFill != null)
            hpBarFill.fillAmount = Mathf.Clamp01(hpPercent);
        if (hpText != null)
            hpText.text = $"{memberState.currentHP}/{memberState.MaxHP}";

        // AP
        float apPercent = (float)memberState.currentAP / memberState.MaxAP;
        if (apBarFill != null)
            apBarFill.fillAmount = Mathf.Clamp01(apPercent);
        if (apText != null)
            apText.text = $"{memberState.currentAP}/{memberState.MaxAP}";

        // Stats
        if (attackText != null)
            attackText.text = $"ATK: {memberState.Attack}";
        if (defenseText != null)
            defenseText.text = $"DEF: {memberState.Defense}";

        // EXP
        if (memberState.template != null)
        {
            int nextLevelExp = memberState.template.GetExpForLevel(memberState.level);
            float expPercent = (float)memberState.currentExperience / nextLevelExp;
            if (expBarFill != null)
                expBarFill.fillAmount = Mathf.Clamp01(expPercent);
            if (expText != null)
                expText.text = $"{memberState.currentExperience}/{nextLevelExp}";
        }

        // Equipment
        if (accessoryText != null)
            accessoryText.text = memberState.accessory != null ? memberState.accessory.nomeDoItem : "Nenhum";
        if (armorText != null)
            armorText.text = memberState.armor != null ? memberState.armor.nomeDoItem : "None";
    }
}