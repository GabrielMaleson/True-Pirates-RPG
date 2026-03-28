using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public TextMeshProUGUI actionNameText;
    public TextMeshProUGUI apCostText;
    public Button button;
    public Image image;

    private AttackFile attack;
    private DadosItem item;
    private System.Action onClick;
    private System.Action<AttackFile> onHoverEnter;
    private System.Action onHoverExit;

    // ── Initializers ───────────────────────────────────────────────────────────

    public void Initialize(AttackFile attackFile, System.Action callback,
        System.Action<AttackFile> hoverEnter = null, System.Action hoverExit = null)
    {
        attack       = attackFile;
        onClick      = callback;
        onHoverEnter = hoverEnter;
        onHoverExit  = hoverExit;
        if (image != null) image.sprite = attack.icon;

        actionNameText.text = attackFile.attackName;
        apCostText.text     = $"AP: {attackFile.actionPointCost}";
        apCostText.gameObject.SetActive(true);

        button.onClick.AddListener(OnClick);
    }

    public void Initialize(DadosItem itemData, System.Action callback)
    {
        item    = itemData;
        onClick = callback;

        if (image != null) image.sprite = itemData.icone;
        actionNameText.text = itemData.nomeDoItem;
        apCostText.text     = "";
        apCostText.gameObject.SetActive(false);

        button.onClick.AddListener(OnClick);
    }

    public void InitializeAsBack(System.Action callback)
    {
        onClick             = callback;
        actionNameText.text = "Voltar";
        apCostText.text     = "";
        apCostText.gameObject.SetActive(false);

        button.onClick.AddListener(OnClick);
    }

    public void InitializeAsWait(System.Action callback)
    {
        onClick             = callback;
        actionNameText.text = "Esperar";
        apCostText.text     = "";
        apCostText.gameObject.SetActive(false);

        button.onClick.AddListener(OnClick);
    }

    // ── Pointer events ─────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (attack != null) onHoverEnter?.Invoke(attack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (attack != null) onHoverExit?.Invoke();
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private void OnClick() => onClick?.Invoke();

    private void OnDestroy() => button.onClick.RemoveAllListeners();
}
