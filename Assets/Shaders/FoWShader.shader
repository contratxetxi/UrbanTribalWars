Shader "Custom/FoWShader"
{
    Properties
    {
        _MainTex ("Fog Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        LOD 100

        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            v2f vert (appdata IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                // Tomamos el color RGBA de la textura.
                // Si alpha=0 => transparente, si alpha=1 => opaco
                fixed4 c = tex2D(_MainTex, IN.uv);
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
