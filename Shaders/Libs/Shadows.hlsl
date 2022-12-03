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
    float4 _CascadeData[MAX_CASCADE_COUNT]; //{1 / cascadeCullingSphere.W(radius2),cascade Filter Size}
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIR_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float _ShadowDistance;
    float4 _ShadowDistanceFade; //{1/distance,distanceFade factor,cascadeFade factor}
    float4 _ShadowAtlasSize; //{size ,1/size}
CBUFFER_END

struct DirectionalShadowData{
    float strength;
    int tileIndex;
    float normalBias;
    int occlusionMaskChannel;
};
struct ShadowData{
    int cascadeIndex;
    float strength;
    float cascadeBlend;
    float4 shadowMask;
};

float FadeShadowStrength(float distance,float scale,float fade){
    return saturate( (1-distance*scale) * fade);
}

ShadowData GetShadowData(Surface surface){
    int i=0;
    float fade=1;
    for(;i<_CascadeCount;i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float dist2 = DistanceSquared(surface.worldPos,sphere.xyz);
        if(dist2 < sphere.w){
            fade = FadeShadowStrength(dist2,_CascadeData[i].x/*(1/sphere.w)*/,_ShadowDistanceFade.z);
            break;
        }
    }

    ShadowData d;
    d.cascadeIndex = i;
    d.cascadeBlend = fade;
    
    bool isLast = i == _CascadeCount-1;

    // d.strength = surface.depth < _ShadowDistance ? 1 : 0;
    d.strength = FadeShadowStrength(surface.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
    d.strength *= lerp(1,fade,isLast); // last cascade
    // d.strength *= lerp(1,0, i == _CascadeCount);
    d.strength *= step(i,_CascadeCount);

    d.cascadeBlend = lerp(d.cascadeBlend,1,isLast);

    // if(i == _CascadeCount-1){
    //     d.strength *= fade;
    //     d.cascadeBlend = 1;
    // } 
    #if defined(_CASCADE_BLEND_DITHER)
    // else if(d.cascadeBlend < surface.dither)
    {
        d.cascadeIndex += d.cascadeBlend < surface.dither;
    }
    #endif

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

float GetDirShadowAttenuationRealtime(DirectionalShadowData dirShadowData,ShadowData shadowData,Surface surface){
    float3 normalBias = surface.vertexNormal * dirShadowData.normalBias * _CascadeData[shadowData.cascadeIndex].y;
    float3 posShadowSpace = mul(_DirectionalShadowMatrices[dirShadowData.tileIndex],float4(surface.worldPos+normalBias,1)).xyz;
    float atten = FilterDirShadow(posShadowSpace);

    #if defined(_CASCADE_BLEND_SOFT)
        if(shadowData.cascadeBlend < 1)
        {
            normalBias = surface.vertexNormal * dirShadowData.normalBias * _CascadeData[shadowData.cascadeIndex+1].y;
            posShadowSpace = mul(_DirectionalShadowMatrices[dirShadowData.tileIndex+1],float4(surface.worldPos + normalBias,1)).xyz;
            float atten2 = FilterDirShadow(posShadowSpace);
            atten = lerp(atten2,atten,shadowData.cascadeBlend);
        }
    #endif
    return lerp(1,atten,dirShadowData.strength);
}

float GetBakedShadow(int occlusionMaskChannel,ShadowData shadowData){
    #if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK)
        if(occlusionMaskChannel>=0)
            return lerp(1,shadowData.shadowMask[occlusionMaskChannel],shadowData.strength);
    #endif
    return 1;
}

float MixRealtimeAndBakedShadows(float realtimeShadow,float bakedShadow,float dirShadowStrength,float shadowStrength){
    float shadow = realtimeShadow;
    #if defined(_SHADOW_MASK_DISTANCE)
        shadow = lerp(bakedShadow,realtimeShadow,shadowStrength);
    #elif defined(_SHADOW_MASK)
        realtimeShadow = lerp(1,realtimeShadow,shadowStrength);
        shadow = min(bakedShadow,realtimeShadow);
    #endif
    return lerp(1,shadow,dirShadowStrength);
    // return lerp(bakedShadow,realtimeShadow,dirShadowStrength);
}

float GetDirShadowAttenuation(DirectionalShadowData dirShadowData,ShadowData shadowData,Surface surface){
    #if defined(_RECEIVE_SHADOW_OFF)
        return 1;
    #endif

    float bakedShadow = GetBakedShadow(dirShadowData.occlusionMaskChannel,shadowData);
    if(dirShadowData.strength * shadowData.strength <= 0)
        return bakedShadow;

    float realtimeShadow = GetDirShadowAttenuationRealtime(dirShadowData,shadowData,surface);
    return MixRealtimeAndBakedShadows(realtimeShadow,bakedShadow,dirShadowData.strength,shadowData.strength);
}

struct OtherShadowData{
    float strength;
    int occlusionMaskChannel;
};

float GetOtherShadowAttenuation(OtherShadowData otherShadowData,ShadowData shadowData,Surface surface){
    #if defined(_RECEIVE_SHADOW_OFF)
        return 1;
    #endif
    float bakedShadow = GetBakedShadow(otherShadowData.occlusionMaskChannel,shadowData);
    return bakedShadow;
}
#endif  //CRP_SHADOWS_HLSL