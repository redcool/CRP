#if !defined(CRP_LIGHTING_HLSL)
#define CRP_LIGHTING_HLSL
float3 GetLighting(Surface surface){
    return surface.normal.y * surface.color;
}
#endif //CRP_LIGHTING_HLSL