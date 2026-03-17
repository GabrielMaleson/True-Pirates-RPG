using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar; // Set Image Type to Filled, Fill Method Horizontal
    public GameObject defeatedOverlay;

    [Header("Target Button")]
    public GameObject targetButtonObject;
    public TargetButton targetButton;

    private PartyMemberState memberState;
    private GameObject characterVisualObject;
    private TargetSelector targetSelector;

    public void Initialize(PartyMemberState state, GameObject visualObject)
    {
        memberState = state;
        characterVisualObject = visualObject;
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

        // Ensure health bar is set to fill type
        if (healthBar != null)
        {
            healthBar.type = Image.Type.Filled;
            healthBar.fillMethod = Image.FillMethod.Horizontal;
        }

        UpdateDisplay();

        if (characterVisualObject != null)
        {
            targetSelector = characterVisualObject.GetComponent<TargetSelector>();
            if (targetSelector != null)
            {
                targetSelector.SetEnemyUI(this);
            }
        }

        if (targetButtonObject != null)
        {
            targetButtonObject.SetActive(false);
        }
    }

    public void UpdateDisplay()
    {
        if (memberState != null)
        {
            // Update HP text and bar
            hpText.text = $"{memberState.currentHP}/{memberState.MaxHP}";
            if (healthBar != null)
            {
                float hpPercent = (float)memberState.currentHP / memberState.MaxHP;
                healthBar.fillAmount = Mathf.Clamp01(hpPercent);
            }
        }
    }

    public void ShowTargetButton(TargetSelector selector)
    {
        if (targetButtonObject != null && targetButton != null)
        {
            targetSelector = selector;
            targetButton.SetTarget(memberState, selector.OnTargetSelected);
            targetButtonObject.SetActive(true);
        }
    }

    public void HideTargetButton()
    {
        if (targetButtonObject != null)
        {
            targetButtonObject.SetActive(false);
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

        // Set health bar to 0
        if (healthBar != null)
            healthBar.fillAmount = 0f;

        HideTargetButton();
    }
}