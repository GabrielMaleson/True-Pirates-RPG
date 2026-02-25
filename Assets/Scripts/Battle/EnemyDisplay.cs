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

    private CharacterData character;
    private bool isParty;
    private Color originalBorderColor;

    public void Initialize(CharacterData characterData, bool isPartyMember)
    {
        character = characterData;
        isParty = !isPartyMember;

        nameText.text = characterData.characterName;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (character != null)
        {
            hpText.text = $"{character.currentHP}/{character.hp}";
            if (healthBar != null)
                healthBar.fillAmount = (float)character.currentHP / character.hp;
        }
    }

    public void SetDefeated()
    {
        if (defeatedOverlay != null)
            defeatedOverlay.SetActive(true);

        Image bg = GetComponent<Image>();
        if (bg != null)
            bg.color = Color.gray;
    }
}