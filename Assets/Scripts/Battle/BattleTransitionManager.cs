using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum BattleTransitionType
{
    VerticalReflectedWipe    = 0, // Duas barras do topo e base convergindo ao centro
    ChessThenCircles         = 1, // Xadrez, depois círculos expandem de cada casa
    CirclesChessMoreCircles  = 2, // Círculo global + xadrez + círculos locais
    EnclosingTriangles       = 3, // Quatro triângulos fechando do exterior ao centro
    SpinningSpiral           = 4, // Espiral giratória varrendo a tela
    Gooey                    = 5, // Células orgânicas de Voronoi expandindo
    Trapped                  = 6, // Barras de grade fechando das bordas ao centro
}

public class BattleTransitionManager : MonoBehaviour
{
    public static BattleTransitionManager Instance { get; private set; }

    private const int   TexSize           = 256;
    private const float TransitionSeconds = 0.7f;

    private Texture2D[] gradientTextures;
    private Material    transitionMaterial;
    private RawImage    transitionImage;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupOverlay();
            GenerateAllGradients();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Retorna a instância existente ou cria uma nova.</summary>
    public static BattleTransitionManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        return new GameObject("BattleTransitionManager").AddComponent<BattleTransitionManager>();
    }

    // ── Setup ──────────────────────────────────────────────────────────────

    private void SetupOverlay()
    {
        var canvasGO = new GameObject("TransitionCanvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<CanvasScaler>();

        var imageGO = new GameObject("TransitionImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        transitionImage = imageGO.AddComponent<RawImage>();

        var rt          = transitionImage.rectTransform;
        rt.anchorMin    = Vector2.zero;
        rt.anchorMax    = Vector2.one;
        rt.offsetMin    = Vector2.zero;
        rt.offsetMax    = Vector2.zero;

        var shader = Shader.Find("Custom/BattleTransition");
        if (shader == null)
        {
            Debug.LogError("[BattleTransitionManager] Shader 'Custom/BattleTransition' não encontrado. Verifique se o arquivo está em Assets/Shaders/.");
            return;
        }

        transitionMaterial        = new Material(shader);
        transitionImage.material  = transitionMaterial;
        transitionImage.color     = Color.white;
        transitionImage.enabled   = false;
    }

    // ── Gradient Generation ────────────────────────────────────────────────

    private void GenerateAllGradients()
    {
        var values = (BattleTransitionType[])Enum.GetValues(typeof(BattleTransitionType));
        gradientTextures = new Texture2D[values.Length];
        foreach (var t in values)
            gradientTextures[(int)t] = GenerateGradient(t);
    }

    private static Texture2D GenerateGradient(BattleTransitionType type)
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.R8, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var pixels = new Color[TexSize * TexSize];
        for (int iy = 0; iy < TexSize; iy++)
        {
            for (int ix = 0; ix < TexSize; ix++)
            {
                float x = (ix + 0.5f) / TexSize;
                float y = (iy + 0.5f) / TexSize;
                float v = GradientValue(type, x, y);
                pixels[iy * TexSize + ix] = new Color(v, v, v, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static float GradientValue(BattleTransitionType type, float x, float y)
    {
        switch (type)
        {
            // ── Vertical Reflected Wipe ──────────────────────────────────────
            // Borda superior e inferior = 0 (ficam pretas primeiro).
            // Centro vertical = 1 (fica preto por último).
            case BattleTransitionType.VerticalReflectedWipe:
                return 1f - 2f * Mathf.Abs(y - 0.5f);

            // ── Chess then Circles ───────────────────────────────────────────
            // Casas negras do tabuleiro: círculos expandem do centro (0) para fora (0.5).
            // Casas brancas: círculos expandem depois (0.5 → 1).
            case BattleTransitionType.ChessThenCircles:
            {
                int   N        = 8;
                int   gx       = Mathf.FloorToInt(x * N);
                int   gy       = Mathf.FloorToInt(y * N);
                float lx       = x * N - gx - 0.5f;
                float ly       = y * N - gy - 0.5f;
                float localD   = Mathf.Sqrt(lx * lx + ly * ly) / 0.707f;
                bool  isDark   = (gx + gy) % 2 == 0;
                return isDark ? localD * 0.5f : 0.5f + localD * 0.5f;
            }

            // ── Circles, Chess and more Circles ─────────────────────────────
            // Círculo global (distância ao centro da tela) misturado com xadrez
            // e círculos locais por célula.
            case BattleTransitionType.CirclesChessMoreCircles:
            {
                float globalD = Mathf.Sqrt((x - 0.5f) * (x - 0.5f) + (y - 0.5f) * (y - 0.5f)) / 0.707f;
                int   N       = 8;
                int   gx      = Mathf.FloorToInt(x * N);
                int   gy      = Mathf.FloorToInt(y * N);
                float lx      = x * N - gx - 0.5f;
                float ly      = y * N - gy - 0.5f;
                float localD  = Mathf.Sqrt(lx * lx + ly * ly) / 0.707f;
                bool  isDark  = (gx + gy) % 2 == 0;
                return Mathf.Clamp01(globalD * 0.25f + (isDark ? 0f : 0.35f) + localD * 0.4f);
            }

            // ── Enclosing Triangles ──────────────────────────────────────────
            // Distância radial inverte (bordas = 0, centro = 1) com variação
            // angular de 4 picos criando uma forma de triângulos.
            case BattleTransitionType.EnclosingTriangles:
            {
                float dx     = x - 0.5f;
                float dy     = y - 0.5f;
                float angle  = Mathf.Atan2(dy, dx);
                float r      = Mathf.Sqrt(dx * dx + dy * dy) / 0.707f;
                float tri    = Mathf.Abs(Mathf.Sin(angle * 2f)) * 0.3f;
                return Mathf.Clamp01(1f - r + tri - 0.15f);
            }

            // ── Spinning Spiral ──────────────────────────────────────────────
            // Gradiente espiral: ângulo normalizado + distância radial * voltas.
            // Resulta em uma varredura em espiral ao redor da tela.
            case BattleTransitionType.SpinningSpiral:
            {
                float dx    = x - 0.5f;
                float dy    = y - 0.5f;
                float angle = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 1f) % 1f;
                float r     = Mathf.Sqrt(dx * dx + dy * dy) / 0.707f;
                return (angle + r * 3f) % 1f;
            }

            // ── Gooey ────────────────────────────────────────────────────────
            // Distância ao centro de célula Voronoi mais próxima.
            // Células expandem do centro para fora e se fundem.
            case BattleTransitionType.Gooey:
            {
                int   N       = 4;
                float minDist = float.MaxValue;
                for (int gxi = -1; gxi <= N + 1; gxi++)
                {
                    for (int gyi = -1; gyi <= N + 1; gyi++)
                    {
                        float cx = (gxi + GradHash(gxi, gyi, 0)) / N;
                        float cy = (gyi + GradHash(gxi, gyi, 1)) / N;
                        float d  = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                        if (d < minDist) minDist = d;
                    }
                }
                return Mathf.Clamp01(Mathf.Sqrt(minDist) * N / 1.5f);
            }

            // ── Trapped ──────────────────────────────────────────────────────
            // Grade de barras fechando das bordas ao centro.
            // Bordas = 0 (primeiro), centro = 1 (último).
            case BattleTransitionType.Trapped:
            {
                float edge  = Mathf.Min(Mathf.Min(x, 1f - x), Mathf.Min(y, 1f - y));
                float hBars = Mathf.Abs(Mathf.Sin(y * Mathf.PI * 12f));
                float vBars = Mathf.Abs(Mathf.Sin(x * Mathf.PI * 12f));
                return Mathf.Clamp01(edge * 2f + Mathf.Max(hBars, vBars) * 0.3f - 0.05f);
            }

            default:
                return 0.5f;
        }
    }

    // Simple integer hash → float [0,1]
    private static float GradHash(int x, int y, int seed)
    {
        int n = x * 1619 + y * 31337 + seed * 1013904223;
        n = (n << 13) ^ n;
        return (float)((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia a transição de entrada (tela → preto) e chama onBlack quando concluída.
    /// </summary>
    public void StartTransitionThen(BattleTransitionType type, Action onBlack)
    {
        if (transitionImage == null || transitionMaterial == null)
        {
            onBlack?.Invoke();
            return;
        }
        StartCoroutine(TransitionRoutine(type, 0f, 1f, onBlack));
    }

    /// <summary>
    /// Reproduz a transição reversa (preto → tela) para revelar a cena de combate.
    /// </summary>
    public void PlayReverseTransition(BattleTransitionType type)
    {
        if (transitionImage == null || transitionMaterial == null) return;
        StartCoroutine(TransitionRoutine(type, 1f, 0f, null));
    }

    // ── Internal ───────────────────────────────────────────────────────────

    private IEnumerator TransitionRoutine(BattleTransitionType type, float from, float to, Action onComplete)
    {
        int index = (int)type;
        if (index < 0 || index >= gradientTextures.Length || gradientTextures[index] == null)
        {
            Debug.LogWarning($"[BattleTransitionManager] Textura de gradiente para '{type}' não disponível.");
            onComplete?.Invoke();
            yield break;
        }

        transitionMaterial.SetTexture("_GradientTex", gradientTextures[index]);
        transitionMaterial.SetFloat("_Cutoff", from);
        transitionImage.enabled = true;

        float elapsed = 0f;
        while (elapsed < TransitionSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / TransitionSeconds);
            transitionMaterial.SetFloat("_Cutoff", Mathf.Lerp(from, to, t));
            yield return null;
        }

        transitionMaterial.SetFloat("_Cutoff", to);

        if (to <= 0f)
            transitionImage.enabled = false;

        onComplete?.Invoke();
    }
}
