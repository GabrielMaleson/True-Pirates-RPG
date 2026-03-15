using UnityEngine;

public class CharacterComponent : MonoBehaviour
{
    public PartyMemberState partyMemberState;

    [Header("Animation")]
    public Animator characterAnimator; // Reference for battle animations

    [Header("Runtime Stats (Read Only)")]
    [SerializeField] private int currentLevel;
    [SerializeField] private int currentHP;
    [SerializeField] private int currentAP;
    [SerializeField] private int currentEXP;
    [SerializeField] private int nextLevelEXP;

    private void Start()
    {
        if (partyMemberState != null)
        {
            UpdateRuntimeStats();
        }

        // If animator not assigned, try to get it
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
    }

    private void UpdateRuntimeStats()
    {
        if (partyMemberState != null)
        {
            currentLevel = partyMemberState.level;
            currentHP = partyMemberState.currentHP;
            currentAP = partyMemberState.currentAP;
            currentEXP = partyMemberState.currentExperience;
            nextLevelEXP = partyMemberState.template != null ?
                partyMemberState.template.GetExpForLevel(partyMemberState.level) : 100;
        }
    }

    public void RefreshStats()
    {
        UpdateRuntimeStats();
    }

    public string GetCharacterName()
    {
        return partyMemberState != null ? partyMemberState.CharacterName : "Unknown";
    }

    public int GetLevel()
    {
        return partyMemberState != null ? partyMemberState.level : 1;
    }

    public int GetCurrentHP()
    {
        return partyMemberState != null ? partyMemberState.currentHP : 0;
    }

    public int GetMaxHP()
    {
        return partyMemberState != null ? partyMemberState.MaxHP : 0;
    }

    public float GetHealthPercent()
    {
        if (partyMemberState == null) return 0;
        return (float)partyMemberState.currentHP / partyMemberState.MaxHP;
    }

    public int GetCurrentAP()
    {
        return partyMemberState != null ? partyMemberState.currentAP : 0;
    }

    public int GetMaxAP()
    {
        return partyMemberState != null ? partyMemberState.MaxAP : 0;
    }

    public float GetAPPercent()
    {
        if (partyMemberState == null) return 0;
        return (float)partyMemberState.currentAP / partyMemberState.MaxAP;
    }

    public int GetCurrentEXP()
    {
        return partyMemberState != null ? partyMemberState.currentExperience : 0;
    }

    public int GetNextLevelEXP()
    {
        if (partyMemberState?.template == null) return 100;
        return partyMemberState.template.GetExpForLevel(partyMemberState.level);
    }

    public float GetEXPPercent()
    {
        if (partyMemberState?.template == null) return 0;
        int nextLevel = GetNextLevelEXP();
        if (nextLevel == 0) return 0;
        return (float)partyMemberState.currentExperience / nextLevel;
    }

    // Play animation for battle system
    public void PlayAnimation(string animationName)
    {
        if (characterAnimator != null)
        {
            characterAnimator.Play(animationName);
        }
    }

    public void PlayAnimation(AnimationClip clip)
    {
        if (characterAnimator != null && clip != null)
        {
            characterAnimator.Play(clip.name);
        }
    }

    private void OnValidate()
    {
        if (partyMemberState != null)
        {
            currentLevel = partyMemberState.level;
            currentHP = partyMemberState.currentHP;
            currentAP = partyMemberState.currentAP;
            currentEXP = partyMemberState.currentExperience;
            nextLevelEXP = partyMemberState.template != null ?
                partyMemberState.template.GetExpForLevel(partyMemberState.level) : 100;
        }
    }
}