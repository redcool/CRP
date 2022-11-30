#if !defined(CRP_LIT_INPUT_HLSL)
#define CRP_LIT_INPUT_HLSL
    #include "Libs/Common.hlsl"

TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
TEXTURE2D(_PBRMask);SAMPLER(sampler_PBRMask);
TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);

// CBUFFER_START(UnityPerMaterial)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(half4,_Color)
    UNITY_DEFINE_INSTANCED_PROP(float,_Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float,_Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float,_Occlusion)
    
    UNITY_DEFINE_INSTANCED_PROP(float,_CullOff)
    UNITY_DEFINE_INSTANCED_PROP(float4,_EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float,_FresnelIntensity)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
#define _Color UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Color)
#define _MainTex_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_MainTex_ST)
#define _Metallic UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Metallic)
#define _Smoothness UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Smoothness)
#define _Occlusion UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Occlusion)
#define _CullOff UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_CullOff)
#define _EmissionColor UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_EmissionColor)
#define _FresnelIntensity UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_FresnelIntensity)
// CBUFFER_END

half3 GetEmission(float2 uv){
    return SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,uv) * _EmissionColor;
}
#endif //CRP_LIT_INPUT_HLSL