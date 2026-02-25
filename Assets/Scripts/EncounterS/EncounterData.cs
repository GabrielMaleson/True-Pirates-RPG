using System.Collections.Generic;
using UnityEngine;

public class EncounterData : MonoBehaviour
{
    [Header("Encounter File")]
    public EncounterFile encounterFile;

    [Header("Player Data")]
    public List<CharacterData> playerPartyMembers = new List<CharacterData>();
    public SistemaInventario playerInventory;

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

    public void CalculateRewards()
    {
        if (encounterFile != null)
        {
            totalGoldReward = encounterFile.baseGoldReward;

            // Calculate EXP from all enemies
            foreach (var enemyData in encounterFile.enemies)
            {
                totalExpReward += enemyData.characterData.expValue;
            }
            totalExpReward += encounterFile.baseExpReward;
        }
    }

    public void ApplyCombatRewards()
    {
        if (playerInventory != null)
        {
            // Apply gold
            playerInventory.ModificadorMoedas(totalGoldReward);

            // Apply item drops from encounter file
            if (encounterFile != null)
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
    }
}