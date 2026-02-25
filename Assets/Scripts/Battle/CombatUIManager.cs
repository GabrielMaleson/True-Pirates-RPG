using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatUIManager : MonoBehaviour
{
    [Header("References")]
    public CombatSystem combatSystem;

    [Header("UI Prefabs")]
    public GameObject partyMemberUIPrefab;    // UI element for party display
    public GameObject enemyUIPrefab;          // UI element for enemy display
    public GameObject actionButtonPrefab;     // Button for actions
    public GameObject targetButtonPrefab;     // Button for targeting

    [Header("UI Parents")]
    public Transform partyUIParent;    // Where party UI elements go (Grid Layout Group)
    public Transform enemyUIParent;     // Where enemy UI elements go (Grid Layout Group)
    public Transform actionButtonParent;
    public Transform targetButtonParent;

    [Header("Text Displays")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI apText;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject actionPanel;
    public GameObject targetPanel;
    public GameObject waitPanel;

    private Dictionary<CharacterData, CharacterUI> partyUIDictionary = new Dictionary<CharacterData, CharacterUI>();
    private Dictionary<CharacterData, EnemyUI> enemyUIDictionary = new Dictionary<CharacterData, EnemyUI>();
    private CharacterData currentCharacter;
    private AttackFile selectedAttack;
    private List<CharacterData> currentTargets = new List<CharacterData>();

    private void Start()
    {
        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();

        // Subscribe to events
        combatSystem.onTurnStarted += OnTurnStarted;
        combatSystem.onCharacterUpdated += OnCharacterUpdated;
        combatSystem.onCombatEnded += OnCombatEnded;
        combatSystem.onActionExecuted += OnActionExecuted;

        // Create UI for all characters
        CreateAllCharacterUI();
    }

    private void CreateAllCharacterUI()
    {
        // Clear existing
        foreach (Transform child in partyUIParent) Destroy(child.gameObject);
        foreach (Transform child in enemyUIParent) Destroy(child.gameObject);
        partyUIDictionary.Clear();
        enemyUIDictionary.Clear();

        // Create party member UI
        foreach (var member in combatSystem.partyMembers)
        {
            if (member != null && member.currentHP > 0)
            {
                GameObject uiObj = Instantiate(partyMemberUIPrefab, partyUIParent);
                CharacterUI characterUI = uiObj.GetComponent<CharacterUI>();
                characterUI.Initialize(member, true);
                partyUIDictionary[member] = characterUI;
            }
        }

        // Create enemy UI
        foreach (var enemy in combatSystem.enemies)
        {
            if (enemy != null && enemy.currentHP > 0)
            {
                GameObject uiObj = Instantiate(enemyUIPrefab, enemyUIParent);
                EnemyUI enemyUI = uiObj.GetComponent<EnemyUI>();
                enemyUI.Initialize(enemy, true);
                enemyUIDictionary[enemy] = enemyUI;
            }
        }
    }

    private void OnTurnStarted(CharacterData character)
    {
        currentCharacter = character;

        // Update turn text
        turnText.text = $"{character.characterName}'s Turn";
        apText.text = $"AP: {character.currentAP}/{character.maxAP}";

        // Show appropriate panel
        if (combatSystem.partyMembers.Contains(character))
        {
            ShowPlayerActions(character);
        }
        else
        {
            ShowEnemyTurn(character);
        }
    }

    private void OnCharacterUpdated(CharacterData character)
    {
        // Update UI for this character
        if (partyUIDictionary.ContainsKey(character))
            partyUIDictionary[character].UpdateDisplay();
        if (enemyUIDictionary.ContainsKey(character))
            enemyUIDictionary[character].UpdateDisplay();

        // Update AP text if it's current character
        if (character == currentCharacter)
        {
            apText.text = $"AP: {character.currentAP}/{character.maxAP}";
        }

        // Check if character died
        if (character.currentHP <= 0)
        {
            if (partyUIDictionary.ContainsKey(character))
                partyUIDictionary[character].SetDefeated();
            if (enemyUIDictionary.ContainsKey(character))
                enemyUIDictionary[character].SetDefeated();
        }
    }

    private void ShowPlayerActions(CharacterData character)
    {
        Debug.Log($"=== Showing Player Actions for {character.characterName} ===");
        Debug.Log($"Available attacks: {character.availableAttacks.Count}");
        Debug.Log($"Current AP: {character.currentAP}/{character.maxAP}");

        actionPanel.SetActive(true);
        targetPanel.SetActive(false);
        waitPanel.SetActive(false);

        // Clear old buttons
        foreach (Transform child in actionButtonParent)
        {
            Destroy(child.gameObject);
        }

        int buttonsCreated = 0;

        // Create action buttons
        foreach (var attack in character.availableAttacks)
        {
            if (attack == null)
            {
                Debug.LogWarning("Null attack found in availableAttacks");
                continue;
            }

            Debug.Log($"Checking attack: {attack.attackName}, AP cost: {attack.actionPointCost}, Current AP: {character.currentAP}");

            if (attack.actionPointCost <= character.currentAP)
            {
                Debug.Log($"Creating button for: {attack.attackName}");

                if (actionButtonPrefab == null)
                {
                    Debug.LogError("actionButtonPrefab is null!");
                    return;
                }

                GameObject btnObj = Instantiate(actionButtonPrefab, actionButtonParent);
                ActionButton btn = btnObj.GetComponent<ActionButton>();

                if (btn == null)
                {
                    Debug.LogError("ActionButton component missing on prefab!");
                    continue;
                }

                btn.Initialize(attack, () => OnActionSelected(attack));
                buttonsCreated++;
            }
        }

        Debug.Log($"Created {buttonsCreated} action buttons");

        // Add Wait button
        if (actionButtonPrefab != null)
        {
            GameObject waitBtn = Instantiate(actionButtonPrefab, actionButtonParent);
            ActionButton waitBtnComponent = waitBtn.GetComponent<ActionButton>();
            if (waitBtnComponent != null)
            {
                waitBtnComponent.InitializeAsWait(() => combatSystem.EndPlayerTurn());
                Debug.Log("Created Wait button");
            }
        }
    }

    private void OnActionSelected(AttackFile attack)
    {
        selectedAttack = attack;
        currentTargets.Clear();

        if (attack.effects.Count > 0)
        {
            TargetType targetType = attack.effects[0].targetType;
            ShowTargetSelection(targetType, attack.effects[0].numberOfTargets);
        }
    }

    private void ShowTargetSelection(TargetType targetType, int numberOfTargets)
    {
        actionPanel.SetActive(false);
        targetPanel.SetActive(true);
        statusText.text = $"Select {numberOfTargets} target(s)";

        // Clear old target buttons
        foreach (Transform child in targetButtonParent)
            Destroy(child.gameObject);

        // Get valid targets
        List<CharacterData> validTargets = targetType == TargetType.Ally
            ? combatSystem.partyMembers
            : combatSystem.enemies;
        validTargets = validTargets.FindAll(t => t.currentHP > 0);

        // Create target buttons
        foreach (var target in validTargets)
        {
            GameObject btnObj = Instantiate(targetButtonPrefab, targetButtonParent);
            TargetButton btn = btnObj.GetComponent<TargetButton>();
            btn.Initialize(target, () => OnTargetSelected(target, numberOfTargets));
        }

        // Add cancel button
        GameObject cancelBtn = Instantiate(targetButtonPrefab, targetButtonParent);
        cancelBtn.GetComponent<TargetButton>().InitializeAsCancel(() => {
            targetPanel.SetActive(false);
            actionPanel.SetActive(true);
            currentTargets.Clear();
            statusText.text = "";
        });
    }

    private void OnTargetSelected(CharacterData target, int maxTargets)
    {
        if (!currentTargets.Contains(target))
            currentTargets.Add(target);

        if (currentTargets.Count >= maxTargets)
        {
            combatSystem.SelectPlayerAction(selectedAttack, currentTargets);
            currentTargets.Clear();
            targetPanel.SetActive(false);

            // Update UI
            OnCharacterUpdated(currentCharacter);

            // Show actions again if仍有 AP
            if (currentCharacter.currentAP > 0)
                ShowPlayerActions(currentCharacter);
            else
                waitPanel.SetActive(true);
        }
        else
        {
            statusText.text = $"Select {maxTargets - currentTargets.Count} more target(s)";
        }
    }

    private void ShowEnemyTurn(CharacterData enemy)
    {
        actionPanel.SetActive(false);
        targetPanel.SetActive(false);
        waitPanel.SetActive(true);
        statusText.text = $"{enemy.characterName} is thinking...";
    }

    private void OnActionExecuted(CharacterData user, AttackFile attack, List<CharacterData> targets)
    {
        StartCoroutine(ShowCombatFeedback(user, attack, targets));
    }

    private System.Collections.IEnumerator ShowCombatFeedback(CharacterData user, AttackFile attack, List<CharacterData> targets)
    {
        statusText.text = $"{user.characterName} used {attack.attackName}!";
        yield return new WaitForSeconds(1f);
        statusText.text = "";
    }

    private void OnCombatEnded(CombatState result)
    {
        actionPanel.SetActive(false);
        targetPanel.SetActive(false);
        waitPanel.SetActive(false);

        if (result == CombatState.VICTORY)
            statusText.text = "Victory!";
        else if (result == CombatState.DEFEAT)
            statusText.text = "Defeat...";
    }

    private void OnDestroy()
    {
        if (combatSystem != null)
        {
            combatSystem.onTurnStarted -= OnTurnStarted;
            combatSystem.onCharacterUpdated -= OnCharacterUpdated;
            combatSystem.onCombatEnded -= OnCombatEnded;
            combatSystem.onActionExecuted -= OnActionExecuted;
        }
    }
}