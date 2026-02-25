using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TargetButton : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public Button button;
    public Image background;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightedColor = Color.yellow;

    private CharacterData target;
    private System.Action onClick;

    public void Initialize(CharacterData targetCharacter, System.Action callback)
    {
        target = targetCharacter;
        onClick = callback;

        // Set UI elements
        targetNameText.text = targetCharacter.characterName;
        UpdateHPDisplay();

        button.onClick.AddListener(OnClick);
    }

    public void InitializeAsCancel(System.Action callback)
    {
        targetNameText.text = "Cancel";
        hpText.text = "";
        healthBar.gameObject.SetActive(false);
        onClick = callback;

        button.onClick.AddListener(OnClick);

        // Make cancel button red
        if (background != null)
            background.color = Color.red;
    }

    private void UpdateHPDisplay()
    {
        if (target != null)
        {
            hpText.text = $"{target.currentHP}/{target.hp}";
            if (healthBar != null)
            {
                healthBar.fillAmount = (float)target.currentHP / target.hp;
            }
        }
    }

    private void OnPointerEnter()
    {
        if (background != null)
            background.color = highlightedColor;
    }

    private void OnPointerExit()
    {
        if (background != null)
            background.color = normalColor;
    }

    private void OnClick()
    {
        onClick?.Invoke();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }
}