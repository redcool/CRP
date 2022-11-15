#if !defined(CRP_SHADOWS_HLSL)
#define CRP_SHADOWS_HLSL
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

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
CBUFFER_END

struct DirectionalShadowData{
    float strength;
    int tileIndex;
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

float GetDirShadowAttenuation(DirectionalShadowData data,Surface surface){
    if(data.strength<=0)
        return 1;

    float3 posShadowSpace = mul(_DirectionalShadowMatrices[data.tileIndex],float4(surface.worldPos,1)).xyz;
    float atten = SampleDirectionalShadowAtlas(posShadowSpace);
    return lerp(1,atten,data.strength);
}
#endif  //CRP_SHADOWS_HLSL