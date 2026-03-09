using UnityEngine;

public class CharacterComponent : MonoBehaviour
{
    public CharacterData characterData;

    [Header("Runtime Stats (Read Only)")]
    [SerializeField] private int currentLevel;
    [SerializeField] private int currentHP;
    [SerializeField] private int currentAP;
    [SerializeField] private int currentEXP;
    [SerializeField] private int nextLevelEXP;

    private void Start()
    {
        if (characterData != null)
        {
            // Initialize runtime stats
            UpdateRuntimeStats();
        }
    }

    private void UpdateRuntimeStats()
    {
        if (characterData != null)
        {
            currentLevel = characterData.level;
            currentHP = characterData.currentHP;
            currentAP = characterData.currentAP;
            currentEXP = characterData.currentExperience;
            nextLevelEXP = characterData.level * 100; // Simple formula, adjust as needed
        }
    }

    // Call this after any changes to character data
    public void RefreshStats()
    {
        UpdateRuntimeStats();
    }

    // Public methods to access character data safely
    public string GetCharacterName()
    {
        return characterData != null ? characterData.characterName : "Unknown";
    }

    public int GetLevel()
    {
        return characterData != null ? characterData.level : 1;
    }

    public int GetCurrentHP()
    {
        return characterData != null ? characterData.currentHP : 0;
    }

    public int GetMaxHP()
    {
        return characterData != null ? characterData.hp : 0;
    }

    public float GetHealthPercent()
    {
        if (characterData == null || characterData.hp == 0) return 0;
        return (float)characterData.currentHP / characterData.hp;
    }

    public int GetCurrentAP()
    {
        return characterData != null ? characterData.currentAP : 0;
    }

    public int GetMaxAP()
    {
        return characterData != null ? characterData.maxAP : 0;
    }

    public float GetAPPercent()
    {
        if (characterData == null || characterData.maxAP == 0) return 0;
        return (float)characterData.currentAP / characterData.maxAP;
    }

    public int GetCurrentEXP()
    {
        return characterData != null ? characterData.currentExperience : 0;
    }

    public int GetNextLevelEXP()
    {
        if (characterData == null) return 100;
        return characterData.level * 100; // Simple formula
    }

    public float GetEXPPercent()
    {
        if (characterData == null) return 0;
        int nextLevel = GetNextLevelEXP();
        if (nextLevel == 0) return 0;
        return (float)characterData.currentExperience / nextLevel;
    }

    public void GainExperience(int amount)
    {
        if (characterData != null)
        {
            int oldLevel = characterData.level;
            characterData.GainExperience(amount);
            UpdateRuntimeStats();

            // Log level up
            if (characterData.level > oldLevel)
            {
                Debug.Log($"{characterData.characterName} leveled up to {characterData.level}!");
            }
        }
    }

    // Called by Unity Editor to update Inspector display
    private void OnValidate()
    {
        if (characterData != null)
        {
            currentLevel = characterData.level;
            currentHP = characterData.currentHP;
            currentAP = characterData.currentAP;
            currentEXP = characterData.currentExperience;
            nextLevelEXP = characterData.level * 100;
        }
    }
}