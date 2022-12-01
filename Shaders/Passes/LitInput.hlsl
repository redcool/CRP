#if !defined(CRP_LIT_INPUT_HLSL)
#define CRP_LIT_INPUT_HLSL
    #include "Libs/Common.hlsl"

TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
TEXTURE2D(_PBRMask);SAMPLER(sampler_PBRMask);
TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
TEXTURE2D(_DetailMap);SAMPLER(sampler_DetailMap);
TEXTURE2D(_DetailNormalMap);SAMPLER(sampler_DetailNormalMap);

// CBUFFER_START(UnityPerMaterial)
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(half4,_Color)
    UNITY_DEFINE_INSTANCED_PROP(float,_Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float,_Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float,_Occlusion)
    
    UNITY_DEFINE_INSTANCED_PROP(float,_CullOff)
    UNITY_DEFINE_INSTANCED_PROP(float4,_EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float4,_EmissionMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float,_FresnelIntensity)
    UNITY_DEFINE_INSTANCED_PROP(float,_NormalMapScale)

    UNITY_DEFINE_INSTANCED_PROP(float4,_NormalMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4,_DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float,_DetailMapScale)
    UNITY_DEFINE_INSTANCED_PROP(float4,_DetailNormalMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float,_DetailNormalMapScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
#define _MainTex_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_MainTex_ST)
#define _Color UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Color)
#define _Metallic UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Metallic)
#define _Smoothness UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Smoothness)
#define _Occlusion UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Occlusion)

#define _CullOff UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_CullOff)
#define _EmissionColor UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_EmissionColor)
#define _EmissionMap_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_EmissionMap_ST)
#define _FresnelIntensity UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_FresnelIntensity)
#define _NormalMapScale UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_NormalMapScale)

#define _NormalMap_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_NormalMap_ST)
#define _DetailMap_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_DetailMap_ST)
#define _DetailMapScale UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_DetailMapScale)

#define _DetailNormalMap_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_DetailNormalMap_ST)
#define _DetailNormalMapScale UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_DetailNormalMapScale)
// CBUFFER_END

half3 GetEmission(float2 uv){
    #if defined(_EMISSION_MAP_ON)
    uv = TRANSFORM_TEX(uv,_EmissionMap);
    return SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,uv) * _EmissionColor;
    #endif
    return 0;
}

float ApplyDetailMap(inout half3 albedo,float2 uv){
    #if defined(_DETAIL_MAP_ON)
        uv = TRANSFORM_TEX(uv,_DetailMap);
        half4 detailTex = SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,uv);
        float detailMask = pow(detailTex.w,0.5); //restore srgb( to gamma)
        half3 detailAlbedo = detailTex.xyz * _DetailMapScale;
        albedo *= lerp(1,detailAlbedo,detailMask);
        return detailMask;
    #endif
    return 0;
}

void ApplyDetailNormal(inout float3 tn,float2 uv,float mask){
    #if defined(_DETAIL_MAP_ON)
        float2 duv = TRANSFORM_TEX(uv,_DetailNormalMap);
        float3 dtn = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap,sampler_DetailNormalMap,duv),_DetailNormalMapScale);
        tn = lerp(tn,BlendNormalRNM(tn,dtn),mask);
    #endif
}

void ApplyNormal(inout float3 normal,float2 uv,float3 tSpace0,float3 tSpace1,float3 tSpace2,float detailMask){
    #if defined(_NORMAL_MAP_ON)
        float2 tnUV = TRANSFORM_TEX(uv,_NormalMap);
        float3 tn = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,tnUV),_NormalMapScale);
        ApplyDetailNormal(tn/**/,uv,detailMask);
        normal = normalize(TransformToTangent(tSpace0,tSpace1,tSpace2,tn));
    #endif
}
#endif //CRP_LIT_INPUT_HLSL