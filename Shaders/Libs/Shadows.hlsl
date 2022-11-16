#if !defined(CRP_SHADOWS_HLSL)
#define CRP_SHADOWS_HLSL
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIR_LIGHT_COUNT 8
#define MAX_CASCADE_COUNT 4


TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CRPShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIR_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float _ShadowDistance;
    float4 _ShadowDistanceFade;
    float4 _ShadowAtlasSize;
CBUFFER_END

struct DirectionalShadowData{
    float strength;
    int tileIndex;
    float normalBias;
};
struct ShadowData{
    int cascadeIndex;
    float strength;
};

float FadeShadowStrength(float distance,float scale,float fade){
    return saturate( (1-distance*scale) * fade);
}

ShadowData GetShadowData(Surface surface){
    int i=0;
    float4 sphere;
    float dist2;
    for(;i<_CascadeCount;i++)
    {
        sphere = _CascadeCullingSpheres[i];
        dist2 = DistanceSquared(surface.worldPos,sphere.xyz);
        if(dist2 < sphere.w)
            break;
    }

    ShadowData d;
    d.cascadeIndex = i;
    // d.strength = surface.depth < _ShadowDistance ? 1 : 0;
    d.strength = FadeShadowStrength(surface.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
    if(i == _CascadeCount-1){
        d.strength *= FadeShadowStrength(dist2,_CascadeData[i].x/*(1/sphere.w)*/,_ShadowDistanceFade.z);
    }
    if(i == _CascadeCount)
        d.strength = 0;
    return d;
}

float SampleDirectionalShadowAtlas(float3 posShadowSpace){
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,posShadowSpace).x;
}

float FilterDirShadow(float3 posShadowSpace){
    #if defined(DIRECTIONAL_FILTER_SETUP)
        float weights[DIRECTIONAL_FILTER_SAMPLES];
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
        float4 size = _ShadowAtlasSize.yyxx;
        DIRECTIONAL_FILTER_SETUP(size,posShadowSpace.xy,weights/**/,positions/**/);
        float shadow = 0;
        for(int i=0;i<DIRECTIONAL_FILTER_SAMPLES;i++){
            shadow += SampleDirectionalShadowAtlas(float3(positions[i].xy,posShadowSpace.z)) * weights[i];
        }
        return shadow;
    #else
        return SampleDirectionalShadowAtlas(posShadowSpace);
    #endif
}

float GetDirShadowAttenuation(DirectionalShadowData data,ShadowData shadowData,Surface surface){
    if(data.strength<=0)
        return 1;
    float3 normalBias = surface.normal * data.normalBias * _CascadeData[shadowData.cascadeIndex].y;

    float3 posShadowSpace = mul(_DirectionalShadowMatrices[data.tileIndex],float4(surface.worldPos+normalBias,1)).xyz;
    float atten = FilterDirShadow(posShadowSpace);
    return lerp(1,atten,data.strength);
}
#endif  //CRP_SHADOWS_HLSL