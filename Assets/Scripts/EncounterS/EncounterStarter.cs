using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterStarter : MonoBehaviour
{
    [Header("Encounter Configuration")]
    public EncounterFile encounterFile;

    [Tooltip("If true, uses the manual enemy list instead of the EncounterFile")]
    public bool useManualEnemies = false;

    [Tooltip("Manual enemy list (for quick testing)")]
    public List<ExtraEnemies> manualEnemies = new List<ExtraEnemies>();

    [System.Serializable]
    public class ExtraEnemies
    {
        public CharacterData characterData;
        public int level = 1;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartEncounter(collision.gameObject);
        }
    }

    public void StartEncounter(GameObject playerObject)
    {
        // Store reference to THIS GameObject to disable it later
        GameObject encounterStarterObject = this.gameObject;

        // Get or create EncounterData
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData == null)
        {
            GameObject dataObj = new GameObject("EncounterData");
            DontDestroyOnLoad(dataObj);
            encounterData = dataObj.AddComponent<EncounterData>();
        }

        // Store the encounter starter GameObject reference for later disabling
        encounterData.encounterStarterObject = encounterStarterObject;

        // Store reference to original player GameObject
        encounterData.originalPlayer = playerObject;

        // Store player data from SistemaInventario
        SistemaInventario inventory = playerObject.GetComponent<SistemaInventario>();
        if (inventory != null)
        {
            encounterData.playerInventory = inventory;
        }

        // Store player CharacterData
        CharacterData playerCharacter = playerObject.GetComponent<CharacterData>();
        if (playerCharacter != null)
        {
            encounterData.playerCharacter = CreateCharacterDataCopy(playerCharacter);
        }

        // Store enemy data
        encounterData.enemyCharacters.Clear();

        if (!useManualEnemies && encounterFile != null)
        {
            // Use EncounterFile data
            foreach (var enemyData in encounterFile.enemies)
            {
                CharacterData enemyCopy = CreateCharacterDataCopy(enemyData.characterData);

                // Apply level override
                if (enemyData.level > 1)
                {
                    // Level up to specified level
                    while (enemyCopy.level < enemyData.level)
                    {
                        enemyCopy.LevelUp();
                    }
                }

                // Override HP if specified
                if (enemyData.overrideHP > 0)
                {
                    enemyCopy.currentHP = enemyData.overrideHP;
                    enemyCopy.hp = enemyData.overrideHP;
                }

                // Add additional attacks
                foreach (var attack in enemyData.additionalAttacks)
                {
                    if (!enemyCopy.availableAttacks.Contains(attack))
                    {
                        enemyCopy.availableAttacks.Add(attack);
                    }
                }

                encounterData.enemyCharacters.Add(enemyCopy);

                // Calculate rewards
                encounterData.totalExpReward += encounterFile.baseExpReward + enemyData.characterData.expValue;
                encounterData.totalGoldReward += encounterFile.baseGoldReward;
            }

            // Store encounter file reference for drops
            encounterData.encounterFile = encounterFile;
        }
        else
        {
            // Use manual enemies (legacy support)
            foreach (var extraEnemy in manualEnemies)
            {
                if (extraEnemy.characterData != null)
                {
                    CharacterData enemyCopy = CreateCharacterDataCopy(extraEnemy.characterData);

                    // Apply level
                    while (enemyCopy.level < extraEnemy.level)
                    {
                        enemyCopy.LevelUp();
                    }

                    encounterData.enemyCharacters.Add(enemyCopy);
                    encounterData.totalExpReward += enemyCopy.expValue;
                }
            }
        }

        // Create scene manager and load combat
        GameObject sceneObj = new GameObject("PreviousScene");
        sceneObj.AddComponent<PreviousScene>();
        sceneObj.GetComponent<PreviousScene>().UnloadScene();

        SceneManager.LoadScene("Combat", LoadSceneMode.Additive);
    }

    private CharacterData CreateCharacterDataCopy(CharacterData source)
    {
        // Create a runtime copy of CharacterData
        CharacterData copy = ScriptableObject.CreateInstance<CharacterData>();

        // Copy basic info
        copy.characterName = source.characterName;
        copy.level = source.level;
        copy.currentHP = source.currentHP;
        copy.expValue = source.expValue;

        // Copy stats
        copy.baseHP = source.baseHP;
        copy.baseAttack = source.baseAttack;
        copy.baseDefense = source.baseDefense;
        copy.maxAP = source.maxAP;

        // Copy growth curves
        copy.hpGrowth = source.hpGrowth;
        copy.attackGrowth = source.attackGrowth;
        copy.defenseGrowth = source.defenseGrowth;

        // Copy attacks
        copy.unlockableAttacks = new List<UnlockableAttack>(source.unlockableAttacks);
        copy.availableAttacks = new List<AttackFile>(source.availableAttacks);

        // Calculate stats for current level
        copy.CalculateStatsForLevel();

        return copy;
    }
}