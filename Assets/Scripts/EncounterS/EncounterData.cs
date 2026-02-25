using System.Collections.Generic;
using UnityEngine;

public class EncounterData : MonoBehaviour
{
    [Header("Encounter File")]
    public EncounterFile encounterFile;

    [Header("Player Data")]
    public List<CharacterData> playerPartyMembers = new List<CharacterData>();
    public SistemaInventario playerInventory;

    [Header("Enemy Data - IMPORTANT")]
    public List<CharacterData> enemyCharacters = new List<CharacterData>(); // Store the copied character data
    public List<GameObject> enemyPrefabs = new List<GameObject>(); // Store which prefab to use for each enemy
    public List<int> enemyLevels = new List<int>(); // Store enemy levels
    public List<int> enemyOverrideHP = new List<int>(); // Store HP overrides

    [Header("Encounter Tracking")]
    public GameObject encounterStarterObject;
    public bool combatVictory = false;

    [Header("Rewards")]
    public int totalExpReward = 0;
    public int totalGoldReward = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadEncounterFromFile(EncounterFile file)
    {
        Debug.Log($"=== Loading Encounter From File: {file.name} ===");

        encounterFile = file;

        // Clear existing enemy data
        enemyCharacters.Clear();
        enemyPrefabs.Clear();
        enemyLevels.Clear();
        enemyOverrideHP.Clear();

        // Load each enemy from the file
        for (int i = 0; i < file.enemies.Count; i++)
        {
            var enemyData = file.enemies[i];

            if (enemyData.characterData == null)
            {
                Debug.LogError($"Enemy {i} has no CharacterData!");
                continue;
            }

            Debug.Log($"Loading enemy {i}: {enemyData.characterData.characterName}, Prefab: {enemyData.enemyPrefab?.name}, Level: {enemyData.level}");

            // Create a copy of the character data
            CharacterData enemyCopy = CreateCharacterDataCopy(enemyData.characterData);

            // Store the data
            enemyCharacters.Add(enemyCopy);
            enemyPrefabs.Add(enemyData.enemyPrefab);
            enemyLevels.Add(enemyData.level);
            enemyOverrideHP.Add(enemyData.overrideHP);

            // Calculate EXP reward
            totalExpReward += enemyData.characterData.expValue;
        }

        // Add base rewards
        totalGoldReward = file.baseGoldReward;
        totalExpReward += file.baseExpReward;

        Debug.Log($"Loaded {enemyCharacters.Count} enemies. Total EXP: {totalExpReward}, Gold: {totalGoldReward}");
    }

    private CharacterData CreateCharacterDataCopy(CharacterData source)
    {
        CharacterData copy = ScriptableObject.CreateInstance<CharacterData>();

        copy.characterName = source.characterName;
        copy.level = source.level;
        copy.currentHP = source.currentHP;
        copy.expValue = source.expValue;

        copy.baseHP = source.baseHP;
        copy.baseAttack = source.baseAttack;
        copy.baseDefense = source.baseDefense;
        copy.maxAP = source.maxAP;

        copy.hpGrowth = source.hpGrowth;
        copy.attackGrowth = source.attackGrowth;
        copy.defenseGrowth = source.defenseGrowth;

        copy.unlockableAttacks = new List<UnlockableAttack>(source.unlockableAttacks);
        copy.availableAttacks = new List<AttackFile>(source.availableAttacks);

        copy.CalculateStatsForLevel();

        return copy;
    }

    public void ApplyCombatRewards()
    {
        if (playerInventory != null)
        {
            // Apply gold
            playerInventory.ModificadorMoedas(totalGoldReward);
            Debug.Log($"Added {totalGoldReward} gold to player");

            // Apply item drops from encounter file
            if (encounterFile != null)
            {
                // Guaranteed drops
                foreach (var item in encounterFile.guaranteedDrops)
                {
                    if (item != null)
                    {
                        playerInventory.AdicionarItem(item, 1);
                        Debug.Log($"Added guaranteed drop: {item.nomeDoItem}");
                    }
                }

                // Random drops
                foreach (var drop in encounterFile.randomDrops)
                {
                    int roll = Random.Range(0, 100);
                    if (roll < drop.dropChance)
                    {
                        int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                        playerInventory.AdicionarItem(drop.item, quantity);
                        Debug.Log($"Added random drop: {drop.item.nomeDoItem} x{quantity}");
                    }
                }
            }
        }
    }

    public void ReactivateOriginalPlayer(Vector3 position)
    {
        if (playerInventory != null)
        {
            // Update party members with combat results
            playerInventory.UpdatePartyMembersFromCombat(playerPartyMembers);

            // Reactivate player GameObject
            playerInventory.gameObject.SetActive(true);
            playerInventory.transform.position = position;

            Debug.Log($"Player reactivated at {position} with updated party stats");
        }
    }

    private void OnDestroy()
    {
        // Clean up runtime scriptable objects
        foreach (var member in playerPartyMembers)
        {
            if (member != null && member.name.Contains("(Clone)"))
            {
                Destroy(member);
            }
        }

        foreach (var enemy in enemyCharacters)
        {
            if (enemy != null && enemy.name.Contains("(Clone)"))
            {
                Destroy(enemy);
            }
        }
    }
}