#if !defined(CRP_COMMON_HLSL)
#define CRP_COMMON_HLSL
#include "UnityInput.hlsl"

float DistanceSquared(float3 p1,float3 p2){
    return dot(p1-p2,p1-p2);
}

#endif //CRP_COMMON_HLSL