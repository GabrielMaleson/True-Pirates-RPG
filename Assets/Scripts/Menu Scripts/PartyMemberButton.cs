using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyMemberButton : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public Button button;
    public Image highlightBorder;

    private PartyMemberState memberState;
    private PartyMenuManager menuManager;

    public void Initialize(PartyMemberState state, PartyMenuManager manager)
    {
        memberState = state;
        menuManager = manager;

        // Set icon
        if (iconImage != null)
        {
            if (state.PartyIcon != null)
            {
                iconImage.sprite = state.PartyIcon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        nameText.text = state.CharacterName;

        button.onClick.AddListener(OnClick);

        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(false);
    }

    private void OnClick()
    {
        if (menuManager != null)
        {
            menuManager.OnPartyMemberSelected(memberState);
        }
    }

    public void SetHighlight(bool highlighted)
    {
        if (highlightBorder != null)
            highlightBorder.gameObject.SetActive(highlighted);
    }

    public PartyMemberState GetMemberState()
    {
        return memberState;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}