using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Encounter", menuName = "RPG/Encounter File")]
public class EncounterFile : ScriptableObject
{
    [Header("Encounter Details")]
    public string encounterName;
    public Sprite encounterIcon;
    public bool progress;
    public string progressAdd;

    [Header("Enemies")]
    public List<EncounterEnemyData> enemies = new List<EncounterEnemyData>();

    [Header("Rewards")]
    public int baseGoldReward;
    public int baseExpReward;
    public List<DadosItem> guaranteedDrops = new List<DadosItem>();
    public List<RandomDrop> randomDrops = new List<RandomDrop>();

    [Header("Battle Settings")]
    public bool isBossEncounter;
    public AudioClip battleMusic;
    public Sprite battleBackground;
}

[System.Serializable]
public class EncounterEnemyData
{
    public CharacterData characterData;
    public GameObject enemyPrefab; // <-- ADD THIS
    public int level = 1;
    public int overrideHP; // 0 = use calculated from level
    public List<AttackFile> additionalAttacks = new List<AttackFile>();
    public List<DadosItem> enemySpecificDrops = new List<DadosItem>();
}

[System.Serializable]
public class RandomDrop
{
    public DadosItem item;
    [Range(0, 100)]
    public int dropChance;
    public int minQuantity = 1;
    public int maxQuantity = 1;
}