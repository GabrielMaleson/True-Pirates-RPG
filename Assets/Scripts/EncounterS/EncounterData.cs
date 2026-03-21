using System.Collections.Generic;
using UnityEngine;

public class EncounterData : MonoBehaviour
{
    [Header("Encounter File")]
    public EncounterFile encounterFile;
    [Header("Battle Trigger")]
    public string battleTriggerProgress; // The progress tag that triggered this battle
    [Header("Player Data")]
    public List<PartyMemberState> playerPartyMembers = new List<PartyMemberState>();
    public SistemaInventario playerInventory;

    [Header("Enemy Data")]
    public List<PartyMemberState> enemyPartyMembers = new List<PartyMemberState>();
    public List<GameObject> enemyPrefabs = new List<GameObject>();

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

            totalExpReward = encounterFile.baseExpReward;
        }
    }

    public void ApplyCombatRewards()
    {
        if (playerInventory != null)
        {
            playerInventory.ModificadorMoedas(totalGoldReward);

            if (encounterFile != null)
            {
                foreach (var item in encounterFile.guaranteedDrops)
                {
                    if (item != null)
                        playerInventory.AdicionarItem(item, 1);
                }

                foreach (var drop in encounterFile.randomDrops)
                {
                    int roll = Random.Range(0, 100);
                    if (roll < drop.dropChance)
                    {
                        int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                        playerInventory.AdicionarItem(drop.item, quantity);
                    }
                }
                if (encounterFile.progress)
                {
                    playerInventory.AddProgress(encounterFile.progressAdd);
                }
            }
        }
    }

    public void ReactivateOriginalPlayer(Vector3 position)
    {
        if (playerInventory != null)
        {
            playerInventory.gameObject.SetActive(true);
            playerInventory.transform.position = position;
        }
    }
}