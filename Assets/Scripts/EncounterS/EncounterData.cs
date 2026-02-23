using System.Collections.Generic;
using UnityEngine;

public class EncounterData : MonoBehaviour
{
    [Header("Enemy Data")]
    public List<CharacterData> enemyCharacters = new List<CharacterData>();
    public int totalExpReward = 0;
    public int totalGoldReward = 0;
    public EncounterFile encounterFile;

    [Header("Player Data")]
    public CharacterData playerCharacter;
    public GameObject originalPlayer;
    public SistemaInventario playerInventory;

    [Header("Encounter Tracking")]
    public GameObject encounterStarterObject;
    public bool combatVictory = false;

    // Store player stats before combat to restore after
    private Dictionary<string, object> preCombatStats = new Dictionary<string, object>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void StorePlayerPreCombatStats()
    {
        if (playerCharacter != null)
        {
            preCombatStats["hp"] = playerCharacter.currentHP;
            preCombatStats["level"] = playerCharacter.level;
            preCombatStats["exp"] = playerCharacter.currentExperience;
        }
    }

    public void RestorePlayerStats()
    {
        if (playerCharacter != null && preCombatStats.ContainsKey("hp"))
        {
            playerCharacter.currentHP = (int)preCombatStats["hp"];
        }
    }

    public void ApplyCombatRewards()
    {
        if (playerCharacter != null)
        {
            // Apply experience
            playerCharacter.GainExperience(totalExpReward);

            // Apply gold to inventory
            if (playerInventory != null)
            {
                playerInventory.ModificadorMoedas(totalGoldReward);
            }

            // Apply item drops
            if (encounterFile != null && playerInventory != null)
            {
                // Guaranteed drops
                foreach (var item in encounterFile.guaranteedDrops)
                {
                    if (item != null)
                    {
                        playerInventory.AdicionarItem(item, 1);
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
                    }
                }

                // Enemy-specific drops
                foreach (var enemy in enemyCharacters)
                {
                    // Check if this enemy has specific drops in the encounter file
                    foreach (var enemyData in encounterFile.enemies)
                    {
                        if (enemyData.characterData.characterName == enemy.characterName)
                        {
                            foreach (var item in enemyData.enemySpecificDrops)
                            {
                                playerInventory.AdicionarItem(item, 1);
                            }
                        }
                    }
                }
            }
        }
    }

    public void ReactivateOriginalPlayer(Vector3 position)
    {
        if (originalPlayer != null)
        {
            // Restore player stats
            if (playerCharacter != null)
            {
                CharacterData playerChar = originalPlayer.GetComponent<CharacterData>();
                if (playerChar != null)
                {
                    // Copy back the updated stats
                    playerChar.currentHP = playerCharacter.currentHP;
                    playerChar.level = playerCharacter.level;
                    playerChar.currentExperience = playerCharacter.currentExperience;
                    playerChar.availableAttacks = new List<AttackFile>(playerCharacter.availableAttacks);
                }
            }

            originalPlayer.SetActive(true);
            originalPlayer.transform.position = position;

            Debug.Log($"Player reactivated at position {position} with HP: {playerCharacter?.currentHP}");
        }
    }

    private void OnDestroy()
    {
        // Clean up runtime scriptable objects
        if (playerCharacter != null && playerCharacter.name.Contains("(Clone)"))
        {
            Destroy(playerCharacter);
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