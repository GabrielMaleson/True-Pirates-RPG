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

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    public void SetTarget(CharacterData targetCharacter, System.Action callback)
    {
        target = targetCharacter;
        onClick = callback;
        targetNameText.text = targetCharacter.characterName;
    }

    private void OnClick()
    {
        onClick?.Invoke();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}