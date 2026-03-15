using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image portraitImage; // NEW: Character portrait
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public TextMeshProUGUI apText;
    public Image apBar;
    public GameObject defeatedOverlay;

    [Header("Target Indicator")]
    public GameObject targetIndicator;
    public TargetButton targetButton;

    private PartyMemberState memberState;
    private GameObject characterVisualObject;
    private CombatUIManager uiManager;

    public void Initialize(PartyMemberState state, GameObject visualObject, CombatUIManager manager)
    {
        memberState = state;
        characterVisualObject = visualObject;
        uiManager = manager;

        nameText.text = state.CharacterName;

        // Set portrait from CharacterData template
        if (portraitImage != null)
        {
            if (state.template != null && state.template.battlePortrait != null)
            {
                portraitImage.sprite = state.template.battlePortrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                // Fallback to party icon if battle portrait not available
                if (state.template != null && state.template.partyIcon != null)
                {
                    portraitImage.sprite = state.template.partyIcon;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }
        }

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
        if (memberState != null)
        {
            hpText.text = $"{memberState.currentHP}/{memberState.MaxHP}";
            if (healthBar != null)
                healthBar.fillAmount = (float)memberState.currentHP / memberState.MaxHP;

            if (apText != null)
                apText.text = $"AP: {memberState.currentAP}/{memberState.MaxAP}";
            if (apBar != null)
                apBar.fillAmount = (float)memberState.currentAP / memberState.MaxAP;
        }
    }

    public void ShowTargetIndicator(TargetSelector selector)
    {
        if (targetIndicator != null && targetButton != null)
        {
            targetButton.SetTarget(memberState, selector.OnTargetSelected);
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

        // Gray out portrait as well
        if (portraitImage != null)
        {
            Color portraitColor = portraitImage.color;
            portraitColor.a = 0.5f;
            portraitImage.color = portraitColor;
        }

        HideTargetIndicator();
    }
}