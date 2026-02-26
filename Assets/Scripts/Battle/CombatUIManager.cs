using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatUIManager : MonoBehaviour
{
    [Header("References")]
    public CombatSystem combatSystem;

    [Header("UI Parents")]
    public Transform partyUIParent;
    public Transform enemyUIParent;
    public Transform targetButtonParent; // New: where target buttons will spawn

    [Header("Prefabs")]
    public GameObject partyMemberUIPrefab;
    public GameObject enemyUIPrefab;
    public GameObject targetButtonPrefab; // New: prefab for target buttons

    [Header("Text Displays")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject waitPanel;
    public GameObject targetPanel; // New: panel that contains target buttons

    private Dictionary<CharacterData, CharacterUI> partyUIDictionary = new Dictionary<CharacterData, CharacterUI>();
    private Dictionary<CharacterData, EnemyUI> enemyUIDictionary = new Dictionary<CharacterData, EnemyUI>();
    private Dictionary<CharacterData, GameObject> partyVisualDictionary = new Dictionary<CharacterData, GameObject>();
    private Dictionary<CharacterData, GameObject> enemyVisualDictionary = new Dictionary<CharacterData, GameObject>();

    private CharacterData currentCharacter;
    private AttackFile selectedAttack;
    private int remainingTargetsToSelect = 0;
    private List<CharacterData> selectedTargets = new List<CharacterData>();
    private bool isInitialized = false;
    private List<GameObject> activeTargetButtons = new List<GameObject>();

    private void Start()
    {
        Debug.Log("=== CombatUIManager Start ===");

        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();

        // Subscribe to events IMMEDIATELY
        if (combatSystem != null)
        {
            combatSystem.onTurnStarted += OnTurnStarted;
            combatSystem.onCharacterUpdated += OnCharacterUpdated;
            combatSystem.onCombatEnded += OnCombatEnded;
            Debug.Log("Subscribed to CombatSystem events");
        }

        // Start initialization
        StartCoroutine(InitializeAfterCombatSystem());
    }

    private IEnumerator InitializeAfterCombatSystem()
    {
        Debug.Log("Starting initialization...");

        // Wait for combatSystem to be ready
        while (combatSystem == null)
        {
            combatSystem = FindFirstObjectByType<CombatSystem>();
            yield return new WaitForSeconds(0.1f);
        }

        // Wait for party members to be populated
        while (combatSystem.partyMembers == null || combatSystem.partyMembers.Count == 0)
        {
            Debug.Log("Waiting for party members to be initialized...");
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($"CombatSystem has {combatSystem.partyMembers.Count} party members. Creating UI...");

        // Create UI for all characters
        CreateAllCharacterUI();
        isInitialized = true;

        // Manually trigger OnTurnStarted for the current character if one exists
        CharacterData currentChar = combatSystem.GetCurrentCharacter();
        if (currentChar != null)
        {
            Debug.Log($"Manually triggering OnTurnStarted for {currentChar.characterName}");
            OnTurnStarted(currentChar);
        }
    }

    private void CreateAllCharacterUI()
    {
        // Clear existing
        foreach (Transform child in partyUIParent) Destroy(child.gameObject);
        foreach (Transform child in enemyUIParent) Destroy(child.gameObject);
        partyUIDictionary.Clear();
        enemyUIDictionary.Clear();
        partyVisualDictionary.Clear();
        enemyVisualDictionary.Clear();

        // Find all visual objects in the scene FIRST
        FindAllVisualObjects();

        Debug.Log($"Creating UI for {combatSystem.partyMembers.Count} party members and {combatSystem.enemies.Count} enemies");

        // Create party member UI (using CharacterUI)
        foreach (var member in combatSystem.partyMembers)
        {
            if (member != null && member.currentHP > 0)
            {
                GameObject uiObj = Instantiate(partyMemberUIPrefab, partyUIParent);
                CharacterUI characterUI = uiObj.GetComponent<CharacterUI>();

                // Find the visual object for this party member
                GameObject visualObj = null;
                if (partyVisualDictionary.ContainsKey(member))
                {
                    visualObj = partyVisualDictionary[member];
                    Debug.Log($"Found visual for {member.characterName}: {visualObj.name}");
                }

                if (characterUI != null)
                {
                    characterUI.Initialize(member, visualObj, this);
                    partyUIDictionary[member] = characterUI;
                    Debug.Log($"Added {member.characterName} to partyUIDictionary");
                }
            }
        }

        // Create enemy UI (using EnemyUI)
        foreach (var enemy in combatSystem.enemies)
        {
            if (enemy != null && enemy.currentHP > 0)
            {
                GameObject uiObj = Instantiate(enemyUIPrefab, enemyUIParent);
                EnemyUI enemyUI = uiObj.GetComponent<EnemyUI>();

                // Find the visual object for this enemy
                GameObject visualObj = null;
                if (enemyVisualDictionary.ContainsKey(enemy))
                {
                    visualObj = enemyVisualDictionary[enemy];
                }

                if (enemyUI != null)
                {
                    enemyUI.Initialize(enemy, visualObj);
                    enemyUIDictionary[enemy] = enemyUI;
                }
            }
        }
    }

    private void FindAllVisualObjects()
    {
        CharacterComponent[] allCharacters = FindObjectsByType<CharacterComponent>(FindObjectsSortMode.None);
        Debug.Log($"Found {allCharacters.Length} CharacterComponents in scene");

        foreach (var characterComp in allCharacters)
        {
            if (characterComp != null && characterComp.characterData != null)
            {
                CharacterData data = characterComp.characterData;

                if (combatSystem.partyMembers.Contains(data))
                {
                    partyVisualDictionary[data] = characterComp.gameObject;
                    Debug.Log($"Party visual: {data.characterName}");
                }
                else if (combatSystem.enemies.Contains(data))
                {
                    enemyVisualDictionary[data] = characterComp.gameObject;
                    Debug.Log($"Enemy visual: {data.characterName}");

                    // Add TargetSelector to enemy if it doesn't have one
                    if (characterComp.GetComponent<TargetSelector>() == null)
                    {
                        TargetSelector selector = characterComp.gameObject.AddComponent<TargetSelector>();
                        selector.Initialize(targetButtonPrefab, targetButtonParent, this);
                        Debug.Log($"Added TargetSelector to {data.characterName}");
                    }
                }
            }
        }
    }

    public void OnActionSelected(AttackFile attack, CharacterData character)
    {
        if (!isInitialized) return;
        if (character != currentCharacter) return;

        selectedAttack = attack;
        selectedTargets.Clear();

        if (attack.effects.Count > 0)
        {
            TargetType targetType = attack.effects[0].targetType;
            remainingTargetsToSelect = attack.effects[0].numberOfTargets;

            statusText.text = $"Select {remainingTargetsToSelect} target(s)";

            // Hide action buttons while targeting
            if (partyUIDictionary.ContainsKey(character))
                partyUIDictionary[character].HideActionButtons();

            // Show target panel
            if (targetPanel != null)
                targetPanel.SetActive(true);

            // Enable targeting on appropriate characters
            if (targetType == TargetType.Ally)
            {
                EnableTargetingOnCharacters(combatSystem.partyMembers);
            }
            else
            {
                EnableTargetingOnCharacters(combatSystem.enemies);
            }
        }
    }

    private void EnableTargetingOnCharacters(List<CharacterData> characters)
    {
        // Clear any existing target buttons
        ClearTargetButtons();

        foreach (var character in characters)
        {
            if (character.currentHP <= 0) continue;

            // Find the TargetSelector on the character's visual GameObject
            GameObject visualObj = null;
            if (combatSystem.partyMembers.Contains(character) && partyVisualDictionary.ContainsKey(character))
            {
                visualObj = partyVisualDictionary[character];
            }
            else if (combatSystem.enemies.Contains(character) && enemyVisualDictionary.ContainsKey(character))
            {
                visualObj = enemyVisualDictionary[character];
            }

            if (visualObj != null)
            {
                TargetSelector selector = visualObj.GetComponent<TargetSelector>();
                if (selector != null)
                {
                    selector.EnableTargeting(character, OnTargetSelected);
                }
            }
        }
    }

    private void ClearTargetButtons()
    {
        foreach (var btn in activeTargetButtons)
        {
            Destroy(btn);
        }
        activeTargetButtons.Clear();
    }

    public void CreateTargetButton(CharacterData target, System.Action<CharacterData> callback)
    {
        if (targetButtonPrefab != null && targetButtonParent != null)
        {
            GameObject btnObj = Instantiate(targetButtonPrefab, targetButtonParent);
            TargetButton btn = btnObj.GetComponent<TargetButton>();

            if (btn != null)
            {
                btn.Initialize(target, () => callback(target));
                activeTargetButtons.Add(btnObj);
            }
        }
    }

    private void DisableAllTargeting()
    {
        ClearTargetButtons();

        if (targetPanel != null)
            targetPanel.SetActive(false);

        foreach (var visualObj in partyVisualDictionary.Values)
        {
            if (visualObj != null)
            {
                TargetSelector selector = visualObj.GetComponent<TargetSelector>();
                if (selector != null)
                    selector.DisableTargeting();
            }
        }

        foreach (var visualObj in enemyVisualDictionary.Values)
        {
            if (visualObj != null)
            {
                TargetSelector selector = visualObj.GetComponent<TargetSelector>();
                if (selector != null)
                    selector.DisableTargeting();
            }
        }
    }

    private void OnTargetSelected(CharacterData target)
    {
        if (!selectedTargets.Contains(target))
        {
            selectedTargets.Add(target);
            remainingTargetsToSelect--;

            statusText.text = $"Selected {selectedTargets.Count}. {remainingTargetsToSelect} more needed";

            if (remainingTargetsToSelect <= 0)
            {
                // All targets selected, execute action
                combatSystem.SelectPlayerAction(selectedAttack, selectedTargets);

                // Disable targeting
                DisableAllTargeting();

                // Clear selection
                selectedAttack = null;
                selectedTargets.Clear();

                // Update UI
                OnCharacterUpdated(currentCharacter);

                // If character still has AP, show actions again
                if (currentCharacter.currentAP > 0)
                {
                    if (partyUIDictionary.ContainsKey(currentCharacter))
                        partyUIDictionary[currentCharacter].ShowActionButtons();
                }
                else
                {
                    waitPanel.SetActive(true);
                    statusText.text = "No AP remaining...";
                }
            }
        }
    }

    private void OnTurnStarted(CharacterData character)
    {
        Debug.Log($"OnTurnStarted called for {character.characterName}");

        currentCharacter = character;

        // Update turn text
        turnText.text = $"{character.characterName}'s Turn";

        // Hide all action buttons first
        HideAllActionButtons();

        if (!isInitialized)
        {
            Debug.Log("UI not initialized yet, will show buttons after initialization");
            return;
        }

        if (combatSystem.partyMembers.Contains(character))
        {
            Debug.Log($"It's player {character.characterName}'s turn. Party UI Dictionary has {partyUIDictionary.Count} entries");

            if (partyUIDictionary.ContainsKey(character))
            {
                Debug.Log($"Found UI for {character.characterName}, showing action buttons");
                partyUIDictionary[character].ShowActionButtons();
                statusText.text = "Select an action";
            }
            else
            {
                Debug.LogError($"No UI found for party member {character.characterName} in dictionary!");
            }
            waitPanel.SetActive(false);
        }
        else
        {
            waitPanel.SetActive(true);
            statusText.text = $"{character.characterName} is thinking...";
        }
    }

    private void HideAllActionButtons()
    {
        foreach (var ui in partyUIDictionary.Values)
            ui.HideActionButtons();
    }

    public void ForceShowButtonsForCharacter(string characterName)
    {
        Debug.Log($"ForceShowButtonsForCharacter: {characterName}");

        foreach (var kvp in partyUIDictionary)
        {
            if (kvp.Key.characterName == characterName)
            {
                Debug.Log($"Found match, calling ShowActionButtons");
                kvp.Value.ShowActionButtons();
                return;
            }
        }

        Debug.LogError($"No character named {characterName} found in partyUIDictionary");
    }

    private void OnCharacterUpdated(CharacterData character)
    {
        if (!isInitialized) return;

        if (partyUIDictionary.ContainsKey(character))
            partyUIDictionary[character].UpdateDisplay();
        if (enemyUIDictionary.ContainsKey(character))
            enemyUIDictionary[character].UpdateDisplay();

        if (character.currentHP <= 0)
        {
            if (partyUIDictionary.ContainsKey(character))
                partyUIDictionary[character].SetDefeated();
            if (enemyUIDictionary.ContainsKey(character))
                enemyUIDictionary[character].SetDefeated();

            if (character == currentCharacter)
            {
                DisableAllTargeting();
                HideAllActionButtons();
            }
        }
    }

    private void OnCombatEnded(CombatState result)
    {
        waitPanel.SetActive(false);
        HideAllActionButtons();
        DisableAllTargeting();

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
        }
    }
}