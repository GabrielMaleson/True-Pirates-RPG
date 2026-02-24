using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButton : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI apCostText;
    public Image iconImage;
    public Button button;

    private AttackFile attack;
    private System.Action onClick;

    public void Initialize(AttackFile attackFile, System.Action callback)
    {
        attack = attackFile;
        onClick = callback;

        // Set UI elements
        actionNameText.text = attackFile.attackName;
        apCostText.text = $"AP: {attackFile.actionPointCost}";

        // If you have icons in AttackFile, you can set them here
        // iconImage.sprite = attackFile.icon;

        button.onClick.AddListener(OnClick);
    }

    public void InitializeAsWait(System.Action callback)
    {
        actionNameText.text = "Wait";
        apCostText.text = "";
        onClick = callback;

        button.onClick.AddListener(OnClick);
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