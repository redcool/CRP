#if !defined(CRP_COMMON_HLSL)
#define CRP_COMMON_HLSL
#include "UnityInput.hlsl"


float DistanceSquared(float3 p1,float3 p2){
    return dot(p1-p2,p1-p2);
}

void ClipLOD(float2 screenPos){
    #if defined(LOD_FADE_CROSSFADE)
    float fade = unity_LODFade.x;

    // float dither = screenPos.y % 16/16;
    float dither = InterleavedGradientNoise(screenPos.xy,0);
    dither *= lerp(-1,1,fade < 0);
    clip(fade + dither);
    #endif
}

float3 TransformToTangent(float3 tSpace0,float3 tSpace1,float3 tSpace2,float3 tn){
    return float3(
        dot(tSpace0,tn),
        dot(tSpace1,tn),
        dot(tSpace2,tn)
    );
}

#endif //CRP_COMMON_HLSL