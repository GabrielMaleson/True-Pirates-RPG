using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class EquipmentCharacterCard : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Personagem")]
    public Image characterIcon;
    public TextMeshProUGUI characterNameText;

    [Header("Slot Acessório")]
    public Image accessoryIcon;
    public TextMeshProUGUI accessoryNameText;
    public Button accessorySlotButton;

    [Header("Slot Armadura")]
    public Image armorIcon;
    public TextMeshProUGUI armorNameText;
    public Button armorSlotButton;

    private PartyMemberState _member;
    private PartyMenuManager _menuManager;

    // ── Targeting ─────────────────────────────────────────────────────────────
    private System.Action<PartyMemberState> _onSelectedCallback;
    private bool _isTargetable = false;
    private Outline _outline;
    private Coroutine _pulseCoroutine;

    public void SetTargetable(bool targetable, System.Action<PartyMemberState> onSelected)
    {
        _isTargetable       = targetable;
        _onSelectedCallback = onSelected;

        if (_outline == null)
        {
            _outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            _outline.effectColor = Color.yellow;
        }
        _outline.enabled = false;

        if (!targetable && _pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isTargetable || _outline == null) return;
        _outline.enabled = true;
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(PulseOutline());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_outline == null) return;
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        _outline.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isTargetable || _onSelectedCallback == null) return;
        _onSelectedCallback.Invoke(_member);
    }

    private IEnumerator PulseOutline()
    {
        while (true)
        {
            float t    = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float size = Mathf.Lerp(2f, 5f, t);
            _outline.effectDistance = new Vector2(size, size);
            yield return null;
        }
    }

    public void Initialize(PartyMemberState member, PartyMenuManager manager)
    {
        _member   = member;
        _menuManager = manager;

        if (characterIcon != null)
        {
            Sprite icon = member.PartyIcon ?? member.BattlePortrait;
            if (icon != null) { characterIcon.sprite = icon; characterIcon.gameObject.SetActive(true); }
            else characterIcon.gameObject.SetActive(false);
        }

        if (characterNameText != null)
            characterNameText.text = member.CharacterName;

        if (accessorySlotButton != null)
            accessorySlotButton.onClick.AddListener(() => _menuManager.StartEquipFromSlot(_member, EquipmentSlot.Acessorio));

        if (armorSlotButton != null)
            armorSlotButton.onClick.AddListener(() => _menuManager.StartEquipFromSlot(_member, EquipmentSlot.Armadura));

        Refresh();
    }

    public void Refresh()
    {
        if (_member == null) return;

        if (accessoryNameText != null)
            accessoryNameText.text = _member.accessory != null ? _member.accessory.nomeDoItem : "nenhum";
        if (accessoryIcon != null)
        {
            bool hasAccessory = _member.accessory?.icone != null;
            accessoryIcon.gameObject.SetActive(hasAccessory);
            if (hasAccessory) accessoryIcon.sprite = _member.accessory.icone;
        }

        if (armorNameText != null)
            armorNameText.text = _member.armor != null ? _member.armor.nomeDoItem : "nenhuma";
        if (armorIcon != null)
        {
            bool hasArmor = _member.armor?.icone != null;
            armorIcon.gameObject.SetActive(hasArmor);
            if (hasArmor) armorIcon.sprite = _member.armor.icone;
        }
    }

    private void OnDestroy()
    {
        if (accessorySlotButton != null) accessorySlotButton.onClick.RemoveAllListeners();
        if (armorSlotButton != null)  armorSlotButton.onClick.RemoveAllListeners();
    }
}
