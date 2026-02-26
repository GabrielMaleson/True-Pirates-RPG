using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TargetSelector : MonoBehaviour
{
    [Header("Target Button")]
    public GameObject targetButtonObject;
    public TargetButton targetButton;

    [Header("References")]
    public CharacterComponent characterComponent;

    private CharacterData characterData;
    private Action<CharacterData> onTargetSelectedCallback;
    private bool isActive = false;

    private void Start()
    {
        if (characterComponent == null)
            characterComponent = GetComponent<CharacterComponent>();

        if (characterComponent != null)
            characterData = characterComponent.characterData;

        if (targetButton == null && targetButtonObject != null)
            targetButton = targetButtonObject.GetComponent<TargetButton>();

        // Initially hide the target button
        if (targetButtonObject != null)
            targetButtonObject.SetActive(false);
    }

    public void EnableTargeting(Action<CharacterData> callback)
    {
        onTargetSelectedCallback = callback;
        isActive = true;

        if (targetButtonObject != null)
        {
            targetButtonObject.SetActive(true);

            if (targetButton != null && characterData != null)
            {
                targetButton.Initialize(characterData, OnTargetSelected);
            }
        }
    }

    public void DisableTargeting()
    {
        isActive = false;
        onTargetSelectedCallback = null;

        if (targetButtonObject != null)
            targetButtonObject.SetActive(false);
    }

    private void OnTargetSelected()
    {
        if (isActive && characterData != null && onTargetSelectedCallback != null)
        {
            onTargetSelectedCallback.Invoke(characterData);
        }
    }

    private void OnDestroy()
    {
        onTargetSelectedCallback = null;
    }
}