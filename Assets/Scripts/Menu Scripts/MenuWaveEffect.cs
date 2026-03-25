using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ondas oceânicas animadas em mosaico que sobem continuamente e somem no topo.
/// Adicione a qualquer GameObject na cena do título.
/// Rotacione o GameObject -90° no eixo Z para que as ondas viajem para a direita.
/// Ajuste sortingOrder para ficar abaixo do Canvas dos botões.
/// </summary>
public class MenuWaveEffect : MonoBehaviour
{
    [Header("Rolagem")]
    public float scrollSpeed = 0.18f;   // velocidade de subida das bandas
    public float waveSpeed   = 0.42f;   // velocidade de animação interna da onda

    [Header("Grade de Pixels")]
    [Range(0.01f, 0.12f)]
    public float pixelSize   = 0.032f;  // tamanho de cada bloco de pixel em UV

    [Header("Bandas")]
    public float numBands    = 3.5f;    // quantas bandas visíveis simultâneas
    [Range(0f, 0.6f)]
    public float gapRatio    = 0.30f;   // proporção de lacuna (invisível) em cada banda
    [Range(0.02f, 0.5f)]
    public float frontWidth  = 0.16f;   // largura da zona de frente (escalonada)

    [Header("Canvas")]
    public int sortingOrder  = 5;       // abaixo do Canvas dos botões

    private Material waveMaterial;

    private void Start()
    {
        var shader = Shader.Find("Custom/MenuWave");
        if (shader == null)
        {
            Debug.LogError("[MenuWaveEffect] Shader 'Custom/MenuWave' não encontrado. Verifique Assets/Shaders/MenuWave.shader.");
            return;
        }

        waveMaterial = new Material(shader);
        PushProperties();

        var canvasGO = new GameObject("WaveCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvasGO.AddComponent<CanvasScaler>();

        var imageGO = new GameObject("WaveImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        var img = imageGO.AddComponent<RawImage>();
        img.material = waveMaterial;
        img.color    = Color.white;

        var rt       = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (waveMaterial != null)
            PushProperties();
    }

    private void PushProperties()
    {
        waveMaterial.SetFloat("_ScrollSpeed", scrollSpeed);
        waveMaterial.SetFloat("_WaveSpeed",   waveSpeed);
        waveMaterial.SetFloat("_PixelSize",   pixelSize);
        waveMaterial.SetFloat("_NumBands",    numBands);
        waveMaterial.SetFloat("_GapRatio",    gapRatio);
        waveMaterial.SetFloat("_FrontWidth",  frontWidth);
    }
}
