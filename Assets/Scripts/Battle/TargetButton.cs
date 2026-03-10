using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TargetButton : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI targetNameText;
    public Button button;

    private PartyMemberState target;
    private System.Action onClick;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    public void SetTarget(PartyMemberState targetCharacter, System.Action callback)
    {
        target = targetCharacter;
        onClick = callback;
        targetNameText.text = targetCharacter.CharacterName;
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