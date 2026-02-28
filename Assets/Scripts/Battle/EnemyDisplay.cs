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
    public GameObject targetButtonObject;
    public TargetButton targetButton;

    private CharacterData characterData;
    private GameObject characterVisualObject;
    private TargetSelector targetSelector;

    public void Initialize(CharacterData data, GameObject visualObject)
    {
        characterData = data;
        characterVisualObject = visualObject;
        nameText.text = data.characterName;
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
            targetSelector = selector;
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