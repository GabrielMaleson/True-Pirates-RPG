using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyMemberButton : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public Button button;
    public Image highlightBorder;

    private CharacterData characterData;
    private PartyMenuManager menuManager;

    public void Initialize(CharacterData character, PartyMenuManager manager)
    {
        characterData = character;
        menuManager = manager;

        nameText.text = character.characterName;
        UpdateHealthDisplay();

        button.onClick.AddListener(OnClick);

        // Initially not highlighted
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(false);
    }

    private void UpdateHealthDisplay()
    {
        if (characterData != null)
        {
            hpText.text = $"{characterData.currentHP}/{characterData.hp}";
            if (healthBar != null)
                healthBar.fillAmount = (float)characterData.currentHP / characterData.hp;
        }
    }

    private void OnClick()
    {
        if (menuManager != null)
        {
            menuManager.OnPartyMemberSelected(characterData);
        }
    }

    public void SetHighlight(bool highlighted)
    {
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(highlighted);
    }

    public CharacterData GetCharacterData()
    {
        return characterData;
    }

    private void Update()
    {
        // Continuously update for real-time changes
        if (characterData != null)
        {
            UpdateHealthDisplay();
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}