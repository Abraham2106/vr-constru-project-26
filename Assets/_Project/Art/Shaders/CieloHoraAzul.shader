// Cielo de hora azul: degradado azul profundo -> violeta -> franja naranja en el horizonte,
// mas intensa hacia donde se oculto el sol. Barato (sin texturas) para el Quest.
Shader "Vivienda/Cielo Hora Azul"
{
    Properties
    {
        _ZenithColor ("Cenit", Color) = (0.05, 0.08, 0.24, 1)
        _MidColor ("Medio", Color) = (0.24, 0.20, 0.42, 1)
        _HorizonColor ("Horizonte", Color) = (0.95, 0.52, 0.32, 1)
        _GroundColor ("Suelo", Color) = (0.06, 0.06, 0.08, 1)
        _SunDirection ("Direccion del ocaso (xz)", Vector) = (0.57, 0, 0.82, 0)
        _GlowWidth ("Ancho del resplandor", Range(0.02, 0.6)) = 0.18
        _GlowStrength ("Fuerza del resplandor", Range(0, 2)) = 1.0
        _Exposure ("Exposicion", Range(0, 3)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            half4 _ZenithColor, _MidColor, _HorizonColor, _GroundColor;
            float4 _SunDirection;
            half _GlowWidth, _GlowStrength, _Exposure;

            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                // Cielo: horizonte -> medio -> cenit.
                half3 sky = lerp(_MidColor.rgb, _ZenithColor.rgb, saturate(pow(saturate(h), 0.55)));
                // Franja calida cerca del horizonte, mas fuerte hacia el ocaso.
                float2 sunXZ = normalize(_SunDirection.xz + 1e-4);
                float facing = saturate(dot(normalize(d.xz + 1e-4), sunXZ) * 0.5 + 0.5);
                float band = exp(-saturate(h) / _GlowWidth);
                half3 glow = _HorizonColor.rgb * band * _GlowStrength * lerp(0.35, 1.0, facing * facing);
                half3 col = sky * (1 - band * 0.6) + glow;
                // Bajo el horizonte: suelo oscuro con transicion suave.
                col = lerp(col, _GroundColor.rgb, saturate(-h * 12));
                return half4(col * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
