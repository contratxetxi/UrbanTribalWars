Shader "Custom/FogOfWarShader"
{
    Properties
    {
        // No hace falta _MainTex si sólo quieres una niebla negra;
        // lo dejamos por si quisieras un color de fondo.
        _MainTex ("Main Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
    }
    SubShader
    {
        // Importante: para transparencia
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // Activar blending alfa
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Leemos la máscara (canal R, escala de grises)
                float mask = tex2D(_MaskTex, i.uv).r;

                // Aquí definimos cuánta opacidad queremos según la máscara
                //  - 0 = zona no explorada => opaca
                //  - 1 = zona explorada => transparente
                float alpha = 1.0 - mask;  // si mask=1 => alpha=0 (transparente)

                // Color base de la niebla: negro (puedes ponerlo gris u otro color)
                float3 fogColor = float3(0, 0, 0);

                // Devolvemos RGBA: el color negro + alpha calculado
                return float4(fogColor, alpha);
            }
            ENDCG
        }
    }
}
