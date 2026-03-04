using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public TextMeshProUGUI apText;
    public Image apBar;
    public GameObject defeatedOverlay;

    [Header("Target Indicator")]
    public GameObject targetIndicator; // Visual indicator when this character is targetable

    private CharacterData characterData;
    private GameObject characterVisualObject;
    private CombatUIManager uiManager;

    public void Initialize(CharacterData data, GameObject visualObject, CombatUIManager manager)
    {
        characterData = data;
        characterVisualObject = visualObject;
        uiManager = manager;

        nameText.text = data.characterName;
        UpdateDisplay();

        // Hide target indicator initially
        if (targetIndicator != null)
            targetIndicator.SetActive(false);

        // Connect to TargetSelector on visual object
        if (characterVisualObject != null)
        {
            TargetSelector selector = characterVisualObject.GetComponent<TargetSelector>();
            if (selector != null)
            {
                selector.SetCharacterUI(this);
            }
        }
    }

    public void UpdateDisplay()
    {
        if (characterData != null)
        {
            hpText.text = $"{characterData.currentHP}/{characterData.hp}";
            if (healthBar != null)
                healthBar.fillAmount = (float)characterData.currentHP / characterData.hp;

            if (apText != null)
                apText.text = $"AP: {characterData.currentAP}/{characterData.maxAP}";
            if (apBar != null)
                apBar.fillAmount = (float)characterData.currentAP / characterData.maxAP;
        }
    }

    public void ShowTargetIndicator(TargetSelector selector)
    {
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(true);
        }
    }

    public void HideTargetIndicator()
    {
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(false);
        }
    }

    public void SetDefeated()
    {
        if (defeatedOverlay != null)
            defeatedOverlay.SetActive(true);

        Image bg = GetComponent<Image>();
        if (bg != null)
            bg.color = Color.gray;

        HideTargetIndicator();
    }
}