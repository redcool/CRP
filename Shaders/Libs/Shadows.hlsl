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

#if defined(_OTHER_PCF3)
    #define OTHER_FILTER_SAMPLES 4
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_OTHER_PCF5)
    #define OTHER_FILTER_SAMPLES 9
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_OTHER_PCF7)
    #define OTHER_FILTER_SAMPLES 16
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_OTHER_LIGHT_COUNT 16


TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_OtherShadowAtlas);

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

    float4x4 _OtherShadowMatrices[MAX_SHADOWED_OTHER_LIGHT_COUNT];
    float4 _OtherShadowTiles[MAX_SHADOWED_OTHER_LIGHT_COUNT];
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

    ShadowData d = (ShadowData)0;
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

float GetDirShadow(DirectionalShadowData dirShadowData,ShadowData shadowData,Surface surface){
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
    float shadow = 1;
    #if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK)
        if(occlusionMaskChannel>=0)
            shadow = lerp(1,shadowData.shadowMask[occlusionMaskChannel],shadowData.strength);
    #endif
    return shadow;
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

    float realtimeShadow = GetDirShadow(dirShadowData,shadowData,surface);
    return MixRealtimeAndBakedShadows(realtimeShadow,bakedShadow,dirShadowData.strength,shadowData.strength);
}

struct OtherShadowData{
    float strength;
    int occlusionMaskChannel;
    int tileIndex;
    bool isPoint;
    float3 lightPos;
    float3 lightDir;
    float3 spotDir;
};

float SampleOtherShadowAtlas(float3 posShadowSpace,float3 bounds){
    posShadowSpace.xy = clamp(posShadowSpace.xy,bounds.xy,bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas,SHADOW_SAMPLER,posShadowSpace).x;
}

static const float3 pointShadowPlanes[6]={
    float3(-1,0,0),float3(1,0,0),
    float3(0,-1,0),float3(0,1,0),
    float3(0,0,-1),float3(0,0,1)
};

float FilterOtherShadow(float3 posShadowSpace,float3 bounds){
    #if defined(OTHER_FILTER_SETUP)
    real weights[OTHER_FILTER_SAMPLES];
    real2 positions[OTHER_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.wwzz;
    OTHER_FILTER_SETUP(size,posShadowSpace,weights,positions);
    float shadow = 0;
    for(int i=0;i<OTHER_FILTER_SAMPLES;i++){
        shadow += SampleOtherShadowAtlas(float3(positions[i].xy,posShadowSpace.z),bounds) * weights[i];
    }
    return shadow;
    #else
    return SampleOtherShadowAtlas(posShadowSpace,bounds);
    #endif
}

float GetOtherShadow(OtherShadowData otherShadowData,ShadowData shadowData,Surface surface){
    float tileIndex = otherShadowData.tileIndex;
    if(tileIndex<0) // no shadowed light
        return 1;
    
    float3 lightPlane = otherShadowData.spotDir;
    if(otherShadowData.isPoint){
        float faceOffset = CubeMapFaceID(-otherShadowData.lightDir);
        tileIndex += faceOffset;
        lightPlane = pointShadowPlanes[faceOffset];
    }

    float4 tileData = _OtherShadowTiles[tileIndex];
    float3 dir = otherShadowData.lightPos - surface.worldPos;
    float dist = dot(dir,lightPlane);
    float3 normalBias = surface.vertexNormal * dist * tileData.w;
    float4 posShadowSpace = mul(_OtherShadowMatrices[tileIndex],float4(surface.worldPos + normalBias,1));
    return FilterOtherShadow(posShadowSpace.xyz/posShadowSpace.w,tileData.xyz);
}

float GetOtherShadowAttenuation(OtherShadowData otherShadowData,ShadowData shadowData,Surface surface){
    #if defined(_RECEIVE_SHADOW_OFF)
        return 1;
    #endif

    float bakedShadow = GetBakedShadow(otherShadowData.occlusionMaskChannel,shadowData);
    if(otherShadowData.strength * shadowData.strength <=0)
        return bakedShadow;
    float realtimeShadow = GetOtherShadow(otherShadowData,shadowData,surface);
    return MixRealtimeAndBakedShadows(realtimeShadow,bakedShadow,otherShadowData.strength,shadowData.strength);
}
#endif  //CRP_SHADOWS_HLSL