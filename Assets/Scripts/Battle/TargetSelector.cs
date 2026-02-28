using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    private CombatUIManager uiManager;
    private System.Action<CharacterData> onTargetSelected;
    private CharacterData characterData;
    private bool isActive = false;
    private EnemyUI enemyUI;

    public void Initialize(CombatUIManager manager)
    {
        uiManager = manager;

        CharacterComponent comp = GetComponent<CharacterComponent>();
        if (comp != null)
            characterData = comp.characterData;
    }

    public void SetEnemyUI(EnemyUI ui)
    {
        enemyUI = ui;
    }

    public void EnableTargeting(CharacterData target, System.Action<CharacterData> callback)
    {
        characterData = target;
        onTargetSelected = callback;
        isActive = true;

        if (enemyUI != null)
        {
            enemyUI.ShowTargetButton(this);
        }
    }

    public void DisableTargeting()
    {
        isActive = false;
        onTargetSelected = null;

        if (enemyUI != null)
        {
            enemyUI.HideTargetButton();
        }
    }

    public void OnTargetSelected()
    {
        if (isActive && onTargetSelected != null)
        {
            onTargetSelected.Invoke(characterData);
        }
    }
}