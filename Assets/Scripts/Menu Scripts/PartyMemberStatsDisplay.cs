using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyMemberStatsDisplay : MonoBehaviour
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
    public TextMeshProUGUI weaponText;
    public TextMeshProUGUI armorText;

    [Header("Colors")]
    public Color hpColor = Color.red;
    public Color apColor = Color.blue;
    public Color expColor = Color.green;

    private PartyMemberState memberState;

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
        if (weaponText != null)
            weaponText.text = memberState.weapon != null ? memberState.weapon.nomeDoItem : "None";
        if (armorText != null)
            armorText.text = memberState.armor != null ? memberState.armor.nomeDoItem : "None";
    }
}