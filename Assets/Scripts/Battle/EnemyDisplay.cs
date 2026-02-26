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

    private CharacterData characterData;
    private GameObject characterVisualObject;

    public void Initialize(CharacterData data, GameObject visualObject)
    {
        characterData = data;
        characterVisualObject = visualObject;
        nameText.text = data.characterName;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (characterData != null)
        {
            hpText.text = $"{characterData.currentHP}/{characterData.hp}";
            if (healthBar != null)
                healthBar.fillAmount = (float)characterData.currentHP / characterData.hp;
        }
    }

    public void SetDefeated()
    {
        if (defeatedOverlay != null)
            defeatedOverlay.SetActive(true);

        // Gray out the background
        Image bg = GetComponent<Image>();
        if (bg != null)
            bg.color = Color.gray;
    }
}