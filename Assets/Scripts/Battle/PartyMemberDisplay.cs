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
    private CombatUIManager uiManager;
    private List<GameObject> activeActionButtons = new List<GameObject>();

    public void Initialize(CharacterData data, CombatUIManager manager)
    {
        characterData = data;
        uiManager = manager;
        nameText.text = data.characterName;
        UpdateDisplay();

        Debug.Log($"CharacterUI initialized for {data.characterName}");
        Debug.Log($"  - Attacks in CharacterData: {data.availableAttacks.Count}");
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
        Debug.Log($"=== ShowActionButtons called for {characterData?.characterName} ===");

        if (characterData == null)
        {
            Debug.LogError("characterData is null!");
            return;
        }

        Debug.Log($"CharacterData attacks count: {characterData.availableAttacks.Count}");
        Debug.Log($"Current AP: {characterData.currentAP}/{characterData.maxAP}");
        Debug.Log($"ActionButtonPanel: {actionButtonPanel != null}");
        Debug.Log($"ActionButtonPrefab: {actionButtonPrefab != null}");

        // Clear old buttons
        ClearActionButtons();

        if (actionButtonPanel == null)
        {
            Debug.LogError("actionButtonPanel is null!");
            return;
        }

        if (actionButtonPrefab == null)
        {
            Debug.LogError("actionButtonPrefab is null!");
            return;
        }

        int buttonsCreated = 0;

        // Create buttons for each available attack that costs <= current AP
        foreach (var attack in characterData.availableAttacks)
        {
            Debug.Log($"Checking attack: {attack?.attackName}");

            if (attack == null)
            {
                Debug.LogWarning("Null attack found in availableAttacks");
                continue;
            }

            Debug.Log($"Attack: {attack.attackName}, AP cost: {attack.actionPointCost}, Can use: {attack.actionPointCost <= characterData.currentAP}");

            if (attack.actionPointCost <= characterData.currentAP)
            {
                Debug.Log($"Creating button for {attack.attackName}");

                GameObject btnObj = Instantiate(actionButtonPrefab, actionButtonPanel);
                ActionButton btn = btnObj.GetComponent<ActionButton>();

                if (btn != null)
                {
                    btn.Initialize(attack, () => OnActionSelected(attack));
                    activeActionButtons.Add(btnObj);
                    buttonsCreated++;
                    Debug.Log($"Successfully created button for {attack.attackName}");
                }
                else
                {
                    Debug.LogError($"ActionButton component missing on instantiated prefab!");
                }
            }
        }

        Debug.Log($"Created {buttonsCreated} action buttons");

        // Add Wait button
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
                Debug.Log("Created Wait button");
            }
        }

        Debug.Log($"Total active buttons after creation: {activeActionButtons.Count}");
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

        // Gray out the UI
        Image bg = GetComponent<Image>();
        if (bg != null)
            bg.color = Color.gray;

        // Hide action buttons
        HideActionButtons();
    }
}