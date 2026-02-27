using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public GameObject defeatedOverlay;

    [Header("Target Button")]
    public GameObject targetButtonObject; // Already exists in hierarchy, starts inactive
    public TargetButton targetButton; // Reference to the TargetButton component

    private CharacterData characterData;
    private GameObject characterVisualObject;
    private TargetSelector targetSelector;

    public void Initialize(CharacterData data, GameObject visualObject)
    {
        characterData = data;
        characterVisualObject = visualObject;
        nameText.text = data.characterName;
        UpdateDisplay();

        // Find the TargetSelector on the visual object and tell it about this UI
        if (characterVisualObject != null)
        {
            targetSelector = characterVisualObject.GetComponent<TargetSelector>();
            if (targetSelector != null)
            {
                targetSelector.SetEnemyUI(this);
            }
        }

        // Ensure target button starts inactive
        if (targetButtonObject != null)
        {
            targetButtonObject.SetActive(false);
        }
    }

    public void UpdateDisplay()
    {
        if (characterData != null)
        {
            hpText.text = $"{characterData.currentHP}/{characterData.hp}";
            if (healthBar != null)
                healthBar.fillAmount = (float)characterData.currentHP / characterData.hp;
        }
    }

    public void ShowTargetButton(TargetSelector selector)
    {
        if (targetButtonObject != null && targetButton != null)
        {
            // Update the button's target (in case it changed)
            targetButton.SetTarget(characterData, selector.OnTargetSelected);
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

        HideTargetButton();
    }
}