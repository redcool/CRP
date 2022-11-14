#if !defined(CRP_SHADOWS_HLSL)
#define CRP_SHADOWS_HLSL
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#define MAX_SHADOWED_DIR_LIGHT_COUNT 8

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CRPShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIR_LIGHT_COUNT];
CBUFFER_END

struct DirectionalShadowData{
    float strength;
    int tileIndex;
};

float SampleDirectionalShadowAtlas(float3 posShadowSpace){
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,posShadowSpace).x;
}
float GetDirShadowAttenuation(DirectionalShadowData data,Surface surface){
    if(data.strength<=0)
        return 1;

    float3 posShadowSpace = mul(_DirectionalShadowMatrices[data.tileIndex],float4(surface.worldPos,1)).xyz;
    float atten = SampleDirectionalShadowAtlas(posShadowSpace);
    return lerp(1,atten,data.strength);
}
#endif  //CRP_SHADOWS_HLSL