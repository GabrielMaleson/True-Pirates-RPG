using UnityEngine;

public class CharacterComponent : MonoBehaviour
{
    public CharacterData characterData;

    // Optional: Pass through methods for convenience
    public string CharacterName => characterData != null ? characterData.characterName : "";
    public int CurrentHP
    {
        get => characterData != null ? characterData.currentHP : 0;
        set { if (characterData != null) characterData.currentHP = value; }
    }
    public int CurrentEXP
    {
        get => characterData != null ? characterData.currentExperience : 0;
        set { if (characterData != null) characterData.currentExperience = value; }
    }
    public int CurrentLevel
    {
        get => characterData != null ? characterData.level : 0;
        set { if (characterData != null) characterData.level = value; }
    }

    // Add other pass-through properties as needed
}