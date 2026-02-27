using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image healthBar;
    public TextMeshProUGUI apText;
    public Image apBar;
    public GameObject defeatedOverlay;

    [Header("Action Button Panel")]
    public Transform actionButtonPanel;
    public GameObject actionButtonPrefab;

    private CharacterData characterData;
    private GameObject characterVisualObject;
    private CombatUIManager uiManager;
    private List<GameObject> activeActionButtons = new List<GameObject>();

    public void Initialize(CharacterData data, GameObject visualObject, CombatUIManager manager)
    {
        characterData = data;
        characterVisualObject = visualObject;
        uiManager = manager;

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

            if (apText != null)
                apText.text = $"AP: {characterData.currentAP}/{characterData.maxAP}";
            if (apBar != null)
                apBar.fillAmount = (float)characterData.currentAP / characterData.maxAP;
        }
    }

    public void ShowActionButtons()
    {
        ClearActionButtons();

        if (actionButtonPanel == null || actionButtonPrefab == null || characterData == null)
            return;

        foreach (var attack in characterData.availableAttacks)
        {
            if (attack == null) continue;

            if (attack.actionPointCost <= characterData.currentAP)
            {
                GameObject btnObj = Instantiate(actionButtonPrefab, actionButtonPanel);
                ActionButton btn = btnObj.GetComponent<ActionButton>();

                if (btn != null)
                {
                    btn.Initialize(attack, () => OnActionSelected(attack));
                    activeActionButtons.Add(btnObj);
                }
            }
        }

        if (actionButtonPrefab != null)
        {
            GameObject waitBtn = Instantiate(actionButtonPrefab, actionButtonPanel);
            ActionButton waitBtnComponent = waitBtn.GetComponent<ActionButton>();
            if (waitBtnComponent != null)
            {
                waitBtnComponent.InitializeAsWait(() => {
                    if (uiManager != null && uiManager.combatSystem != null)
                        uiManager.combatSystem.EndPlayerTurn();
                });
                activeActionButtons.Add(waitBtn);
            }
        }
    }

    public void HideActionButtons()
    {
        foreach (var btn in activeActionButtons)
        {
            btn.SetActive(false);
        }
    }

    public void ClearActionButtons()
    {
        foreach (var btn in activeActionButtons)
        {
            Destroy(btn);
        }
        activeActionButtons.Clear();
    }

    private void OnActionSelected(AttackFile attack)
    {
        if (uiManager != null)
        {
            uiManager.OnActionSelected(attack, characterData);
        }
    }

    public void SetDefeated()
    {
        if (defeatedOverlay != null)
            defeatedOverlay.SetActive(true);

        Image bg = GetComponent<Image>();
        if (bg != null)
            bg.color = Color.gray;

        HideActionButtons();
    }
}