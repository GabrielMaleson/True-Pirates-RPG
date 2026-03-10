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
    public GameObject targetIndicator;
    public TargetButton targetButton;

    private PartyMemberState memberState;
    private GameObject characterVisualObject;
    private CombatUIManager uiManager;
    private TargetSelector selector;

    public void Initialize(PartyMemberState state, GameObject visualObject, CombatUIManager manager)
    {
        memberState = state;
        characterVisualObject = visualObject;
        uiManager = manager;

        nameText.text = state.CharacterName;
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

        HideTargetIndicator();
    }
}