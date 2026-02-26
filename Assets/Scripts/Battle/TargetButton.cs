using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TargetButton : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI targetNameText;
    public Button button;

    private CharacterData target;
    private System.Action onClick;

    public void Initialize(CharacterData targetCharacter, System.Action callback)
    {
        target = targetCharacter;
        onClick = callback;

        targetNameText.text = targetCharacter.characterName;

        button.onClick.AddListener(OnClick);
    }

    public void InitializeAsCancel(System.Action callback)
    {
        targetNameText.text = "Cancel";
        onClick = callback;

        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        onClick?.Invoke();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }
}