Shader "Custom/BattleTransition"
{
    Properties
    {
        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _Cutoff ("Cutoff", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _GradientTex;
            float _Cutoff;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float grad  = tex2D(_GradientTex, i.uv).r;
                float alpha = step(grad, _Cutoff); // 1 where grad <= cutoff (black), 0 otherwise
                return fixed4(0, 0, 0, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
