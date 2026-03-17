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

    private void Awake()
    {
        // If animator not assigned, try to get it
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        // If we still don't have an animator, add one
        if (characterAnimator == null)
        {
            characterAnimator = gameObject.AddComponent<Animator>();
        }

        // Apply animator controller from party member state if available
        UpdateAnimatorController();
    }

    private void Start()
    {
        if (partyMemberState != null)
        {
            UpdateRuntimeStats();
        }
    }

    // Call this method when entering battle to ensure animator controller is set
    public void PrepareForBattle()
    {
        UpdateAnimatorController();
    }

    private void UpdateAnimatorController()
    {
        // If we have a party member state with a template that has an animator controller,
        // apply it to the animator
        if (partyMemberState?.template != null && partyMemberState.template.animatorController != null)
        {
            // Check if the current controller is different
            if (characterAnimator.runtimeAnimatorController != partyMemberState.template.animatorController)
            {
                characterAnimator.runtimeAnimatorController = partyMemberState.template.animatorController;
                Debug.Log($"Animator controller set for {partyMemberState.CharacterName}");
            }
        }
        else
        {
            Debug.LogWarning($"No animator controller found for {partyMemberState?.CharacterName ?? "Unknown"}");
        }
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