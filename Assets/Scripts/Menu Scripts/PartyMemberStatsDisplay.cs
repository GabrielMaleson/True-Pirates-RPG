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
    public Image hpBarFill; // Image with fill method
    public TextMeshProUGUI hpText;

    [Header("AP Display")]
    public Image apBarFill; // Image with fill method
    public TextMeshProUGUI apText;

    [Header("Stats")]
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;

    [Header("EXP Display")]
    public Image expBarFill; // Image with fill method
    public TextMeshProUGUI expText;

    [Header("Colors")]
    public Color hpColor = Color.red;
    public Color apColor = Color.blue;
    public Color expColor = Color.green;

    private CharacterData characterData;

    public void Initialize(CharacterData character)
    {
        characterData = character;

        // Set name and level
        if (nameText != null)
            nameText.text = character.characterName;

        if (levelText != null)
            levelText.text = $"Lv. {character.level}";

        // Set bar colors
        if (hpBarFill != null) hpBarFill.color = hpColor;
        if (apBarFill != null) apBarFill.color = apColor;
        if (expBarFill != null) expBarFill.color = expColor;

        // Initial update
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (characterData == null) return;

        // Update HP
        float hpPercent = (float)characterData.currentHP / characterData.hp;
        if (hpBarFill != null)
            hpBarFill.fillAmount = Mathf.Clamp01(hpPercent);
        if (hpText != null)
            hpText.text = $"{characterData.currentHP}/{characterData.hp}";

        // Update AP
        float apPercent = (float)characterData.currentAP / characterData.maxAP;
        if (apBarFill != null)
            apBarFill.fillAmount = Mathf.Clamp01(apPercent);
        if (apText != null)
            apText.text = $"{characterData.currentAP}/{characterData.maxAP}";

        // Update stats
        if (attackText != null)
            attackText.text = $"ATK: {characterData.attack}";
        if (defenseText != null)
            defenseText.text = $"DEF: {characterData.defense}";

        // Update EXP
        int nextLevelExp = characterData.level * 100;
        float expPercent = (float)characterData.currentExperience / nextLevelExp;
        if (expBarFill != null)
            expBarFill.fillAmount = Mathf.Clamp01(expPercent);
        if (expText != null)
            expText.text = $"{characterData.currentExperience}/{nextLevelExp}";
    }

    private void Update()
    {
        // Continuously update for real-time changes (healing, damage, etc.)
        UpdateDisplay();
    }
}