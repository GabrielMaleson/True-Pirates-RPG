using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla a tela de Game Over carregada de forma aditiva sobre a cena de exploração.
///
/// Setup na cena GameOver:
///   - Canvas com Image (preto, full-screen, Raycast Target = true)
///   - TextMeshPro "GAME OVER" centralizado
///   - Dois botões: Retry e Quit
///   - Este script em um GameObject pai — arraste os botões nos campos do inspector
///   - A cena GameOver deve ter seu próprio EventSystem
///
/// Fluxo:
///   Retry  → restaura HP/AP pré-batalha → descarrega GameOver → recarrega Combat
///   Quit   → descarta snapshot → carrega Menu em modo Single (limpa tudo)
/// </summary>
public class GameOver : MonoBehaviour
{
    [Header("Botões")]
    public Button retryButton;
    public Button quitButton;

    [Header("Nomes das Cenas")]
    public string combatSceneName = "Combat";
    public string menuSceneName   = "Menu";

    private void Start()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }

    private void OnRetry()
    {
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();

        // Restaurar HP/AP do grupo para o estado pré-batalha
        if (encounterData != null && BattleSaveManager.Instance != null)
            BattleSaveManager.Instance.RestoreSnapshot(encounterData.playerPartyMembers);

        // Reiniciar a música de batalha
        if (encounterData != null && encounterData.encounterFile != null
            && encounterData.encounterFile.battleMusic != null)
        {
            MusicManager.Instance?.StopMusic();
            MusicManager.Instance?.PlayClip(encounterData.encounterFile.battleMusic);
        }

        // Descarregar Game Over e recarregar a cena de combate
        SceneManager.UnloadSceneAsync("GameOver");
        SceneManager.LoadSceneAsync(combatSceneName, LoadSceneMode.Additive);
    }

    private void OnQuit()
    {
        // Descartar snapshot e voltar ao menu principal
        BattleSaveManager.Instance?.ClearSnapshot();
        MusicManager.Instance?.StopMusic();
        SceneManager.LoadScene(menuSceneName);
    }
}
