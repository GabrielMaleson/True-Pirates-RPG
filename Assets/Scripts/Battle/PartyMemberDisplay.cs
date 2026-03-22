using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar; // Set Image Type to Filled, Fill Method Horizontal
    public TextMeshProUGUI apText;
    public Image apBar; // Set Image Type to Filled, Fill Method Horizontal
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

        if (nameText != null) nameText.text = state.CharacterName;

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

        // Ensure bars are set to fill type
        if (healthBar != null)
        {
            healthBar.type = Image.Type.Filled;
            healthBar.fillMethod = Image.FillMethod.Horizontal;
        }

        if (apBar != null)
        {
            apBar.type = Image.Type.Filled;
            apBar.fillMethod = Image.FillMethod.Horizontal;
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
            // Update HP text and bar
            if (hpText != null) hpText.text = $"{memberState.currentHP}/{memberState.MaxHP}";
            if (healthBar != null)
            {
                float hpPercent = (float)memberState.currentHP / memberState.MaxHP;
                healthBar.fillAmount = Mathf.Clamp01(hpPercent);
            }

            // Update AP text and bar
            if (apText != null)
                apText.text = $"AP: {memberState.currentAP}/{memberState.MaxAP}";
            if (apBar != null)
            {
                float apPercent = (float)memberState.currentAP / memberState.MaxAP;
                apBar.fillAmount = Mathf.Clamp01(apPercent);
            }
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

        // Set bars to 0
        if (healthBar != null)
            healthBar.fillAmount = 0f;
        if (apBar != null)
            apBar.fillAmount = 0f;

        HideTargetIndicator();
    }
}