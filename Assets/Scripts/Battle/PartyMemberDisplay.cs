using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyMemberDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public TextMeshProUGUI apText;
    public Image apBar;

    private CharacterData character;

    public void Initialize(CharacterData characterData)
    {
        character = characterData;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (character != null)
        {
            nameText.text = character.characterName;
            hpText.text = $"{character.currentHP}/{character.hp}";
            healthBar.fillAmount = (float)character.currentHP / character.hp;

            apText.text = $"AP: {character.currentAP}/{character.maxAP}";
            apBar.fillAmount = (float)character.currentAP / character.maxAP;
        }
    }

    private void Update()
    {
        // Continuously update for real-time changes
        if (character != null)
        {
            UpdateDisplay();
        }
    }
}