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

    [Header("Battle Music")]
    public AudioClip battleMusic; // Add this field

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

    /// <summary>
    /// Reconstrói a lista de inimigos a partir do EncounterFile para a tentativa de retry.
    /// Deve ser chamado antes de recarregar a cena de combate após uma derrota.
    /// </summary>
    public void ResetEnemiesForRetry()
    {
        if (encounterFile == null) return;

        enemyPartyMembers.Clear();
        enemyPrefabs.Clear();
        combatVictory = false;

        foreach (var enemyData in encounterFile.enemies)
        {
            if (enemyData.characterData == null) continue;
            PartyMemberState enemyState = new PartyMemberState(enemyData.characterData, enemyData.level);
            if (enemyData.overrideHP > 0)
                enemyState.currentHP = enemyData.overrideHP;
            enemyPartyMembers.Add(enemyState);
            enemyPrefabs.Add(enemyData.enemyPrefab);
        }

        Debug.Log($"[EncounterData] Inimigos reiniciados para retry — {enemyPartyMembers.Count} inimigo(s).");
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