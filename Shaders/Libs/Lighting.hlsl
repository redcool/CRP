#if !defined(CRP_LIGHTING_HLSL)
#define CRP_LIGHTING_HLSL

float3 CalcLight(Light light,Surface surface){
    float3 diffColor = surface.albedo * (0.96 - surface.metallic * 0.96);
    float3 specColor = lerp(0.04,surface.albedo,surface.metallic);
    float pr = 1 - surface.smoothness;
    float r = pr*pr;
    float r2 = r * r;

    float3 h = normalize(light.direction + surface.viewDir);
    float3 l = light.direction;
    float3 n = surface.normal;
    float nh = saturate(dot(n,h));
    float lh = saturate(dot(l,h));
    float nl = saturate(dot(n,l));

    float d = nh*nh * (r2-1)+1;
    float specTerm = r2/(d*d * max(0.0001,lh*lh) * (4*r + 2));
    float3 radiance = nl * light.attenuation * light.color;
    return (diffColor + specColor * specTerm)* radiance;
}

float3 CalcLight(Light light,Surface surface,BRDF brdf){
    float nl = saturate(dot(light.direction,surface.normal));
    float3 radiance = nl * light.attenuation * light.color;
    float specTerm = CalcSpecTerm(light,surface,brdf);
    return (brdf.diffuse + brdf.specular * specTerm) * radiance;
}

float3 GetLighting(Surface surface){
    float3 col=0;
    int lightCount = GetLightCount();
    BRDF brdf = GetBRDF(surface);

    for(int i=0;i<lightCount;i++){
        Light l = GetLight(i,surface);
        col += CalcLight(l,surface);
    }
    return col;
}
#endif //CRP_LIGHTING_HLSL