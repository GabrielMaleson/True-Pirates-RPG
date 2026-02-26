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

    [Header("Prefabs")]
    public GameObject partyMemberUIPrefab; // Uses CharacterUI script
    public GameObject enemyUIPrefab;        // Uses EnemyUI script

    [Header("Text Displays")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject waitPanel;

    private Dictionary<CharacterData, CharacterUI> partyUIDictionary = new Dictionary<CharacterData, CharacterUI>();
    private Dictionary<CharacterData, EnemyUI> enemyUIDictionary = new Dictionary<CharacterData, EnemyUI>();
    private Dictionary<CharacterData, GameObject> partyVisualDictionary = new Dictionary<CharacterData, GameObject>();
    private Dictionary<CharacterData, GameObject> enemyVisualDictionary = new Dictionary<CharacterData, GameObject>();

    private CharacterData currentCharacter;
    private AttackFile selectedAttack;
    private int remainingTargetsToSelect = 0;
    private List<CharacterData> selectedTargets = new List<CharacterData>();

    private void Start()
    {
        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();

        // Subscribe to events
        combatSystem.onTurnStarted += OnTurnStarted;
        combatSystem.onCharacterUpdated += OnCharacterUpdated;
        combatSystem.onCombatEnded += OnCombatEnded;

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
        partyVisualDictionary.Clear();
        enemyVisualDictionary.Clear();

        // Find all visual objects in the scene FIRST
        FindAllVisualObjects();
        // Create party member UI (using CharacterUI)
        foreach (var member in combatSystem.partyMembers)
        {
            if (member != null && member.currentHP > 0)
            {
                Debug.Log($"Creating UI for party member: {member.characterName}");
                Debug.Log($"  - HP: {member.currentHP}/{member.hp}");
                Debug.Log($"  - AP: {member.currentAP}/{member.maxAP}");
                Debug.Log($"  - Attacks: {member.availableAttacks.Count}");

                foreach (var attack in member.availableAttacks)
                {
                    Debug.Log($"    * {attack.attackName} (AP: {attack.actionPointCost})");
                }

                GameObject uiObj = Instantiate(partyMemberUIPrefab, partyUIParent);
                CharacterUI characterUI = uiObj.GetComponent<CharacterUI>();

                if (characterUI != null)
                {
                    // FIX: Only pass 2 arguments - CharacterData and CombatUIManager
                    characterUI.Initialize(member, this);
                    partyUIDictionary[member] = characterUI;
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
        // Find all CharacterComponent objects in the scene
        CharacterComponent[] allCharacters = FindObjectsByType<CharacterComponent>(FindObjectsSortMode.None);

        Debug.Log($"Found {allCharacters.Length} CharacterComponents in scene");

        foreach (var characterComp in allCharacters)
        {
            if (characterComp != null && characterComp.characterData != null)
            {
                CharacterData data = characterComp.characterData;
                Debug.Log($"Found character: {data.characterName} on {characterComp.gameObject.name}");

                // Check if this is a party member or enemy
                if (combatSystem.partyMembers.Contains(data))
                {
                    partyVisualDictionary[data] = characterComp.gameObject;
                    Debug.Log($" -> Added to party visuals");
                }
                else if (combatSystem.enemies.Contains(data))
                {
                    enemyVisualDictionary[data] = characterComp.gameObject;
                    Debug.Log($" -> Added to enemy visuals");
                }
            }
        }
    }

    // Public method for CharacterUI to call when an action is selected
    public void OnActionSelected(AttackFile attack, CharacterData character)
    {
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
                    selector.EnableTargeting(OnTargetSelected);
                }
                else
                {
                    Debug.LogWarning($"No TargetSelector on {visualObj.name}");
                }
            }
        }
    }

    private void DisableAllTargeting()
    {
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
        currentCharacter = character;

        // Update turn text
        turnText.text = $"{character.characterName}'s Turn";

        // Hide all action buttons first (only party members have them)
        HideAllActionButtons();

        if (combatSystem.partyMembers.Contains(character))
        {
            // It's player's turn - show action buttons on this character's UI
            if (partyUIDictionary.ContainsKey(character))
            {
                partyUIDictionary[character].ShowActionButtons();
                statusText.text = "Select an action";
            }
            waitPanel.SetActive(false);
        }
        else
        {
            // It's enemy's turn
            waitPanel.SetActive(true);
            statusText.text = $"{character.characterName} is thinking...";
        }
    }

    private void HideAllActionButtons()
    {
        foreach (var ui in partyUIDictionary.Values)
            ui.HideActionButtons();
    }

    private void OnCharacterUpdated(CharacterData character)
    {
        // Update UI for this character
        if (partyUIDictionary.ContainsKey(character))
            partyUIDictionary[character].UpdateDisplay();
        if (enemyUIDictionary.ContainsKey(character))
            enemyUIDictionary[character].UpdateDisplay();

        // Check if character died
        if (character.currentHP <= 0)
        {
            if (partyUIDictionary.ContainsKey(character))
                partyUIDictionary[character].SetDefeated();
            if (enemyUIDictionary.ContainsKey(character))
                enemyUIDictionary[character].SetDefeated();

            // If it was the current character, disable targeting
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