using UnityEngine;

/// <summary>
/// Gerencia o fluxo de menus da tela de título.
/// ESC fecha qualquer painel aberto; se nada estiver aberto, abre configurações.
/// </summary>
public class TitleMenuManager : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject configPanel;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (configPanel != null && configPanel.activeSelf)
        {
            CloseSettings();
            return;
        }
    }

    public void ToggleSettings()
    {
        if (configPanel == null) return;
        bool willOpen = !configPanel.activeSelf;
        SFXManager.Instance?.Play(willOpen ? SFXManager.Instance.uiForward : SFXManager.Instance.uiBackward);
        configPanel.SetActive(willOpen);
    }

    public void CloseSettings()
    {
        if (configPanel != null && configPanel.activeSelf)
        {
            SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
            configPanel.SetActive(false);
        }
    }

    public void StartNewGame()
    {
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
        SaveLoadManager.Instance?.NewGame();
    }

    public void LoadGame()
    {
        if (SaveLoadManager.Instance == null || !SaveLoadManager.Instance.SaveExists())
        {
            Debug.LogWarning("[TitleMenuManager] Nenhum save encontrado.");
            return;
        }
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);
        SaveLoadManager.Instance.LoadGame();
    }

    public void QuitGame()
    {
        SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
        Application.Quit();
    }
}
