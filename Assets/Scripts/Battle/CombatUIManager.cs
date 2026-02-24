using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CombatUIManager : MonoBehaviour
{
    [Header("References")]
    public CombatSystem combatSystem;
    public GameObject actionButtonPrefab;
    public GameObject targetButtonPrefab;
    public Transform actionButtonParent;
    public Transform targetButtonParent;

    [Header("Party Display")]
    public Transform partyMemberContainer;
    public GameObject partyMemberUIPrefab;

    [Header("Text Displays")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI apText;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject actionPanel;
    public GameObject targetPanel;
    public GameObject waitPanel;

    private CharacterData currentCharacter;
    private AttackFile selectedAttack;
    private List<CharacterData> currentTargets = new List<CharacterData>();

    private void Start()
    {
        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();

        // Subscribe to combat events
        combatSystem.onTurnStarted += OnTurnStarted;
        combatSystem.onCombatEnded += OnCombatEnded;
        combatSystem.onActionExecuted += OnActionExecuted;
    }

    private void OnTurnStarted(CharacterData character)
    {
        currentCharacter = character;
        UpdateUI();

        if (combatSystem.partyMembers.Contains(character))
        {
            // It's player's turn - show action buttons
            ShowPlayerActions(character);
        }
        else
        {
            // It's enemy's turn - show waiting message
            ShowEnemyTurn(character);
        }
    }

    private void UpdateUI()
    {
        if (currentCharacter != null)
        {
            turnText.text = $"{currentCharacter.characterName}'s Turn";
            apText.text = $"AP: {currentCharacter.currentAP}/{currentCharacter.maxAP}";
        }

        // Update party member displays
        UpdatePartyDisplay();
    }

    private void UpdatePartyDisplay()
    {
        // Clear existing party member UI
        foreach (Transform child in partyMemberContainer)
        {
            Destroy(child.gameObject);
        }

        // Create UI for each party member
        foreach (var member in combatSystem.partyMembers)
        {
            GameObject memberUI = Instantiate(partyMemberUIPrefab, partyMemberContainer);
            var memberDisplay = memberUI.GetComponent<PartyMemberDisplay>();
            if (memberDisplay != null)
            {
                memberDisplay.Initialize(member);
            }
        }
    }

    private void ShowPlayerActions(CharacterData character)
    {
        actionPanel.SetActive(true);
        targetPanel.SetActive(false);
        waitPanel.SetActive(false);

        // Clear existing action buttons
        foreach (Transform child in actionButtonParent)
        {
            Destroy(child.gameObject);
        }

        // Create buttons for each available attack that costs <= current AP
        foreach (var attack in character.availableAttacks)
        {
            if (attack.actionPointCost <= character.currentAP)
            {
                CreateActionButton(attack);
            }
        }

        // Add a "Wait" button to end turn
        CreateWaitButton();
    }

    private void CreateActionButton(AttackFile attack)
    {
        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonParent);
        ActionButton button = buttonObj.GetComponent<ActionButton>();

        if (button != null)
        {
            button.Initialize(attack, () => OnActionSelected(attack));
        }
    }

    private void CreateWaitButton()
    {
        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonParent);
        ActionButton button = buttonObj.GetComponent<ActionButton>();

        if (button != null)
        {
            button.InitializeAsWait(() => combatSystem.EndPlayerTurn());
        }
    }

    private void OnActionSelected(AttackFile attack)
    {
        selectedAttack = attack;

        // Determine valid targets based on attack's first effect
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

        // Clear existing target buttons
        foreach (Transform child in targetButtonParent)
        {
            Destroy(child.gameObject);
        }

        // Get valid targets
        List<CharacterData> validTargets = targetType == TargetType.Ally
            ? combatSystem.partyMembers
            : combatSystem.enemies;

        // Filter out downed characters
        validTargets = validTargets.FindAll(t => t.currentHP > 0);

        // Create buttons for each valid target
        foreach (var target in validTargets)
        {
            CreateTargetButton(target, numberOfTargets);
        }

        // Add a cancel button
        CreateCancelButton();
    }

    private void CreateTargetButton(CharacterData target, int maxTargets)
    {
        GameObject buttonObj = Instantiate(targetButtonPrefab, targetButtonParent);
        TargetButton button = buttonObj.GetComponent<TargetButton>();

        if (button != null)
        {
            button.Initialize(target, () => OnTargetSelected(target, maxTargets));
        }
    }

    private void CreateCancelButton()
    {
        GameObject buttonObj = Instantiate(targetButtonPrefab, targetButtonParent);
        TargetButton button = buttonObj.GetComponent<TargetButton>();

        if (button != null)
        {
            button.InitializeAsCancel(() => {
                targetPanel.SetActive(false);
                actionPanel.SetActive(true);
                currentTargets.Clear();
            });
        }
    }

    private void OnTargetSelected(CharacterData target, int maxTargets)
    {
        currentTargets.Add(target);

        if (currentTargets.Count >= maxTargets)
        {
            // We have enough targets, execute the action
            ExecuteSelectedAction();
        }
        else
        {
            // Need to select more targets
            statusText.text = $"Select {maxTargets - currentTargets.Count} more target(s)";
        }
    }

    private void ExecuteSelectedAction()
    {
        // In a real implementation, you'd need to map targets to effects
        // For simplicity, we're using the first effect's target count

        if (combatSystem != null && currentCharacter != null && selectedAttack != null)
        {
            combatSystem.SelectPlayerAction(selectedAttack, currentTargets);
        }

        // Reset selection
        currentTargets.Clear();
        targetPanel.SetActive(false);

        // Update UI
        UpdateUI();

        // If character still has AP, show actions again
        if (currentCharacter.currentAP > 0)
        {
            ShowPlayerActions(currentCharacter);
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
        // Show floating combat text or effects
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
        if (result == CombatState.VICTORY)
        {
            statusText.text = "Victory!";
        }
        else if (result == CombatState.DEFEAT)
        {
            statusText.text = "Defeat...";
        }

        // Disable all panels
        actionPanel.SetActive(false);
        targetPanel.SetActive(false);
        waitPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (combatSystem != null)
        {
            combatSystem.onTurnStarted -= OnTurnStarted;
            combatSystem.onCombatEnded -= OnCombatEnded;
            combatSystem.onActionExecuted -= OnActionExecuted;
        }
    }
}