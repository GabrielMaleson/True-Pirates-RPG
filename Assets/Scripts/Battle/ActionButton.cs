using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButton : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI apCostText;
    public Button button;
    public Image image;
    private AttackFile attack;
    private DadosItem item;
    private System.Action onClick;

    public void Initialize(AttackFile attackFile, System.Action callback)
    {
        attack = attackFile;
        onClick = callback;

        actionNameText.text = attackFile.attackName;
        apCostText.text = $"AP: {attackFile.actionPointCost}";
        apCostText.gameObject.SetActive(true);

        button.onClick.AddListener(OnClick);
    }

    public void Initialize(DadosItem itemData, System.Action callback)
    {
        item = itemData;
        image.sprite = itemData.icone;
        onClick = callback;

        actionNameText.text = itemData.nomeDoItem;
        apCostText.text = ""; // Items don't use AP
        apCostText.gameObject.SetActive(false);

        button.onClick.AddListener(OnClick);
    }

    public void InitializeAsBack(System.Action callback)
    {
        actionNameText.text = "Back";
        apCostText.text = "";
        apCostText.gameObject.SetActive(false);
        onClick = callback;

        button.onClick.AddListener(OnClick);
    }

    public void InitializeAsWait(System.Action callback)
    {
        actionNameText.text = "Wait";
        apCostText.text = "";
        apCostText.gameObject.SetActive(false);
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