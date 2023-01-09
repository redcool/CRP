#if !defined(CRP_SURFACE_HLSL)
#define CRP_SURFACE_HLSL
struct Surface{
    float3 normal,vertexNormal;
    float3 albedo;
    float alpha;
    float metallic,smoothness,occlusion;
    float oneMinusReflectivity;
    float3 viewDir;
    float3 worldPos;
    float depth;
    float dither;
    float fresnelIntensity;
    uint renderingLayerMask;
};

void PremultiplyAlpha(inout Surface s){
    #if defined(_PREMULTIPLY_ALPHA)
        s.albedo *= s.alpha;
        s.alpha = s.alpha * s.oneMinusReflectivity + (1 - s.oneMinusReflectivity);
    #endif
}

#endif //CRP_SURFACE_HLSL