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

    [Header("Prefabs")]
    public GameObject partyMemberUIPrefab;
    public GameObject enemyUIPrefab;

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
    private bool isInitialized = false;

    private void Start()
    {
        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();

        if (combatSystem != null)
        {
            combatSystem.onTurnStarted += OnTurnStarted;
            combatSystem.onCharacterUpdated += OnCharacterUpdated;
            combatSystem.onCombatEnded += OnCombatEnded;
        }

        StartCoroutine(InitializeAfterCombatSystem());
    }

    private IEnumerator InitializeAfterCombatSystem()
    {
        while (combatSystem == null)
        {
            combatSystem = FindFirstObjectByType<CombatSystem>();
            yield return new WaitForSeconds(0.1f);
        }

        while (combatSystem.partyMembers == null || combatSystem.partyMembers.Count == 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        CreateAllCharacterUI();
        isInitialized = true;

        CharacterData currentChar = combatSystem.GetCurrentCharacter();
        if (currentChar != null)
        {
            OnTurnStarted(currentChar);
        }
    }

    private void CreateAllCharacterUI()
    {
        foreach (Transform child in partyUIParent) Destroy(child.gameObject);
        foreach (Transform child in enemyUIParent) Destroy(child.gameObject);
        partyUIDictionary.Clear();
        enemyUIDictionary.Clear();
        partyVisualDictionary.Clear();
        enemyVisualDictionary.Clear();

        FindAllVisualObjects();

        foreach (var member in combatSystem.partyMembers)
        {
            if (member != null && member.currentHP > 0)
            {
                GameObject uiObj = Instantiate(partyMemberUIPrefab, partyUIParent);
                CharacterUI characterUI = uiObj.GetComponent<CharacterUI>();

                GameObject visualObj = null;
                if (partyVisualDictionary.ContainsKey(member))
                {
                    visualObj = partyVisualDictionary[member];
                }

                if (characterUI != null)
                {
                    characterUI.Initialize(member, visualObj, this);
                    partyUIDictionary[member] = characterUI;
                }
            }
        }

        foreach (var enemy in combatSystem.enemies)
        {
            if (enemy != null && enemy.currentHP > 0)
            {
                GameObject uiObj = Instantiate(enemyUIPrefab, enemyUIParent);
                EnemyUI enemyUI = uiObj.GetComponent<EnemyUI>();

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

        foreach (var characterComp in allCharacters)
        {
            if (characterComp != null && characterComp.characterData != null)
            {
                CharacterData data = characterComp.characterData;

                if (combatSystem.partyMembers.Contains(data))
                {
                    partyVisualDictionary[data] = characterComp.gameObject;
                }
                else if (combatSystem.enemies.Contains(data))
                {
                    enemyVisualDictionary[data] = characterComp.gameObject;

                    if (characterComp.GetComponent<TargetSelector>() == null)
                    {
                        TargetSelector selector = characterComp.gameObject.AddComponent<TargetSelector>();
                        selector.Initialize(this);
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

            if (partyUIDictionary.ContainsKey(character))
                partyUIDictionary[character].HideActionButtons();

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
                if (selectedAttack != null && selectedTargets.Count > 0)
                {
                    AttackFile attackToUse = selectedAttack;
                    List<CharacterData> targetsToUse = new List<CharacterData>(selectedTargets);

                    selectedAttack = null;
                    selectedTargets.Clear();

                    combatSystem.SelectPlayerAction(attackToUse, targetsToUse);

                    DisableAllTargeting();
                    OnCharacterUpdated(currentCharacter);

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
    }

    private void OnTurnStarted(CharacterData character)
    {
        currentCharacter = character;
        turnText.text = $"{character.characterName}'s Turn";
        HideAllActionButtons();

        if (!isInitialized) return;

        if (combatSystem.partyMembers.Contains(character))
        {
            if (partyUIDictionary.ContainsKey(character))
            {
                partyUIDictionary[character].ShowActionButtons();
                statusText.text = "Select an action";
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