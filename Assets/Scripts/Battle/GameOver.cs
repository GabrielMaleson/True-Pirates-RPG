using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla a tela de Game Over carregada de forma aditiva sobre a cena de exploração.
///
/// Setup na cena GameOver:
///   - Canvas com Image (preto, full-screen, Raycast Target = true)
///   - TextMeshPro "GAME OVER" centralizado
///   - TextMeshPro para a frase motivacional — arraste em quoteText
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

    [Header("Frase Motivacional")]
    public TMP_Text quoteText;

    [Header("Nomes das Cenas")]
    public string combatSceneName = "Combat";
    public string menuSceneName   = "Menu";

    private static readonly string[] quotes = new string[]
    {
        "Todo grande aventureiro já caiu antes de conquistar.",
        "A derrota de hoje é a lição de amanhã.",
        "Piratas não desistem — eles mudam o rumo.",
        "O mar não derrota quem continua remando.",
        "Cada fracasso é um mapa para o tesouro.",
        "Até as tempestades mais fortes passam.",
        "A coragem não é não ter medo — é continuar mesmo assim.",
        "Nenhuma lenda foi escrita sem tropeços.",
        "O horizonte sempre aparece para quem não vira a popa.",
        "Cair faz parte da viagem. Levantar é a aventura.",
        "Os maiores capitães já naufragaram antes de comandar frotas.",
        "A batalha perdida não é o fim da guerra.",
        "Força não é nunca cair — é sempre se erguer.",
        "O vento contra é o que afila as velas.",
        "Todo tesouro exige uma jornada difícil.",
    };

    private void Start()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);

        if (quoteText != null)
            quoteText.text = quotes[Random.Range(0, quotes.Length)];
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
