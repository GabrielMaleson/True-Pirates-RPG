using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    private GameObject targetButtonPrefab;
    private Transform targetButtonParent;
    private CombatUIManager uiManager;
    private System.Action<CharacterData> onTargetSelected;
    private CharacterData characterData;
    private bool isActive = false;

    public void Initialize(GameObject prefab, Transform parent, CombatUIManager manager)
    {
        targetButtonPrefab = prefab;
        targetButtonParent = parent;
        uiManager = manager;

        CharacterComponent comp = GetComponent<CharacterComponent>();
        if (comp != null)
            characterData = comp.characterData;
    }

    public void EnableTargeting(CharacterData target, System.Action<CharacterData> callback)
    {
        characterData = target;
        onTargetSelected = callback;
        isActive = true;

        // Create a button in the UI panel
        if (uiManager != null)
        {
            uiManager.CreateTargetButton(characterData, OnTargetSelected);
        }
    }

    public void DisableTargeting()
    {
        isActive = false;
        onTargetSelected = null;
    }

    private void OnTargetSelected(CharacterData target)
    {
        if (isActive && onTargetSelected != null)
        {
            onTargetSelected.Invoke(target);
        }
    }
}