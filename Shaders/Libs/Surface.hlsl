#if !defined(CRP_SURFACE_HLSL)
#define CRP_SURFACE_HLSL
struct Surface{
    float3 normal;
    float3 albedo;
    float alpha;
    float metallic,smoothness,occlusion;
    float3 viewDir;
};
#endif //CRP_SURFACE_HLSL