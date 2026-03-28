using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentCharacterCard : MonoBehaviour
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
