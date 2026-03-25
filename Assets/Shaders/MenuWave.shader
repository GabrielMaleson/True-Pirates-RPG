Shader "Custom/MenuWave"
{
    // Pixel-wave effect inspired by vin-ni/PixelWave (MIT).
    // Divides the screen into a grid of pixel blocks.
    // As each wave band scrolls upward, columns appear one by one based on
    // a per-column random hash — identical to PixelWave's Fisher-Yates row stagger.
    // Rotate the host GameObject -90° on Z to sweep rightward on screen.

    Properties
    {
        _ScrollSpeed ("Velocidade de Rolagem",   Float)           = 0.18
        _WaveSpeed   ("Velocidade da Onda",      Float)           = 0.42
        _PixelSize   ("Tamanho do Pixel (UV)",   Range(0.01,0.12))= 0.032
        _NumBands    ("Bandas Visíveis",         Float)           = 3.5
        _GapRatio    ("Lacuna Entre Bandas",     Range(0.0,0.6))  = 0.30
        _FrontWidth  ("Largura da Frente",       Range(0.02,0.5)) = 0.16
        _MainTex     ("Base (RGB)",              2D)              = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float _ScrollSpeed, _WaveSpeed, _PixelSize, _NumBands, _GapRatio, _FrontWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // Stable per-column hash (equiv. to PixelWave's shuffleArray seed per row)
            float colHash(float col)
            {
                return frac(sin(col * 127.1) * 43758.5453);
            }

            // Per-band hash for wave shape variation
            float bandHash(float band)
            {
                return frac(sin(band * 311.7) * 78251.3);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float  t  = _Time.y;
                float2 uv = i.uv;

                // ── Pixel grid (PixelWave: xSize/ySize blocks) ─────────────────
                float2 pixelIdx    = floor(uv / _PixelSize);
                float2 pixelCenter = (pixelIdx + 0.5) * _PixelSize;

                // Each X column gets a stable random threshold [0,1]
                // (equiv. to PixelWave's Fisher-Yates: each row has a random order slot)
                float threshold = colHash(pixelIdx.x);

                // ── Scrolling bands (upward) ────────────────────────────────────
                float scrolledY = pixelCenter.y * _NumBands - t * _ScrollSpeed;
                float tileFrac  = frac(scrolledY);   // 0-1 within current band
                float tileIdx   = floor(scrolledY);  // which band

                // Wavy crest: each band has a unique sine displacement
                float bPhase   = bandHash(tileIdx) * 6.2832;
                float waveDisp = sin(pixelCenter.x * 3.8 * 6.2832 - t * _WaveSpeed + bPhase)      * 0.045
                               + sin(pixelCenter.x * 7.1 * 6.2832 + t * _WaveSpeed * 0.65 + bPhase * 1.3) * 0.018;

                // Crest = start of water zone in this tile
                float crest = clamp(_GapRatio + waveDisp, 0.02, 0.92);

                // ── PixelWave stagger logic ─────────────────────────────────────
                // Gap   [0, crest):              invisible
                // Front [crest, crest+FW):       column visible if threshold < progress
                // Body  [crest+FW, 1]:           always visible
                //
                // "progress" = how far into the front zone we are (0→1)
                // → columns with low threshold appear first (leading scatter)

                float distFromCrest = tileFrac - crest;
                float progress      = clamp(distFromCrest / _FrontWidth, 0.0, 1.0);

                float inFront   = step(0.0, distFromCrest) * (1.0 - step(_FrontWidth, distFromCrest));
                float inBody    = step(_FrontWidth, distFromCrest);

                // Column is visible in front zone only when progress exceeds its threshold
                float colVisible = step(threshold, progress);
                float visible    = inFront * colVisible + inBody;

                // ── Colour ─────────────────────────────────────────────────────
                // Foam: early-appearing columns in the front zone (#8BC6F6)
                float isFoam = inFront * step(threshold, progress * 0.45);

                float depth       = clamp(distFromCrest / max(1.0 - crest, 0.01), 0.0, 1.0);
                float3 foamColor  = float3(0.545, 0.776, 0.965); // #8BC6F6
                float3 transColor = float3(0.247, 0.459, 0.729); // #3F75BA
                float3 midColor   = float3(0.204, 0.408, 0.663); // #3468A9
                float3 darkColor  = float3(0.063, 0.125, 0.290); // #10204A
                float3 abyssColor = float3(0.031, 0.047, 0.106); // #080C1B

                float3 waterColor = lerp(
                    lerp(foamColor,  transColor, min(depth * 2.8, 1.0)),
                    lerp(midColor,   abyssColor,  depth),
                    smoothstep(0.25, 0.75, depth)
                );

                float3 col = isFoam > 0.5 ? foamColor : waterColor;

                // ── Screen fade (appear from bottom, vanish before top) ─────────
                float fade = smoothstep(0.0, 0.07, uv.y) * (1.0 - smoothstep(0.70, 1.0, uv.y));

                return fixed4(col, saturate(visible * fade));
            }
            ENDCG
        }
    }
}
