using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TargetButton : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public Button button;

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

    private void OnClick()
    {
        onClick?.Invoke();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }
}