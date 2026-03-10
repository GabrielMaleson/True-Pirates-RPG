using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    private CombatUIManager uiManager;
    private System.Action<PartyMemberState> onTargetSelected;
    private PartyMemberState memberState;
    private bool isActive = false;
    private CharacterUI characterUI;
    private EnemyUI enemyUI;

    public void Initialize(CombatUIManager manager)
    {
        uiManager = manager;

        CharacterComponent comp = GetComponent<CharacterComponent>();
        if (comp != null)
            memberState = comp.partyMemberState;
    }

    public void SetCharacterUI(CharacterUI ui)
    {
        characterUI = ui;
    }

    public void SetEnemyUI(EnemyUI ui)
    {
        enemyUI = ui;
    }

    public void EnableTargeting(PartyMemberState target, System.Action<PartyMemberState> callback)
    {
        memberState = target;
        onTargetSelected = callback;
        isActive = true;

        // Show target indicator on the appropriate UI
        if (characterUI != null)
        {
            characterUI.ShowTargetIndicator(this);
        }
        else if (enemyUI != null)
        {
            enemyUI.ShowTargetButton(this);
        }
    }

    public void DisableTargeting()
    {
        isActive = false;
        onTargetSelected = null;

        // Hide target indicator
        if (characterUI != null)
        {
            characterUI.HideTargetIndicator();
        }
        else if (enemyUI != null)
        {
            enemyUI.HideTargetButton();
        }
    }

    public void OnTargetSelected()
    {
        if (isActive && onTargetSelected != null)
        {
            onTargetSelected.Invoke(memberState);
        }
    }
}