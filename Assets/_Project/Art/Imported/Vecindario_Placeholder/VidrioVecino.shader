Shader "ProyectoVR/VidrioVecino" {
 Properties { _Reflection("Entorno", Cube) = "" {} _Tint("Tinte", Color) = (0.5,0.55,0.55,1) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   Name "Forward" Tags { "LightMode"="UniversalForward" } Cull Off ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURECUBE(_Reflection); SAMPLER(sampler_Reflection);
   CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
   Varyings vert(Attributes input) { Varyings o; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz); o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=TransformObjectToWorldNormal(input.normalOS); return o; }
   half4 frag(Varyings input):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); float3 v=normalize(GetWorldSpaceViewDir(input.positionWS)); float3 n=normalize(input.normalWS); half3 environment=SAMPLE_TEXTURECUBE(_Reflection,sampler_Reflection,reflect(-v,n)).rgb; return half4(environment*_Tint.rgb,1); }
   ENDHLSL
  }
 }
}
