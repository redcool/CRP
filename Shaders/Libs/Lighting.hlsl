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

float3 CalcGI(Surface surface,GI gi,BRDF brdf){
    float surfaceReduction = 1/(brdf.a2+1);
    float grazingTerm = saturate(surface.metallic+surface.smoothness);
    float nv = saturate(dot(surface.normal,surface.viewDir));
    float fresnelTerm = Pow4(1-nv) * surface.fresnelIntensity;
    float3 specularGI = gi.specular * lerp(brdf.specular,grazingTerm,fresnelTerm) * surfaceReduction;
    float3 diffuseGI = gi.diffuse * brdf.diffuse;
    return (diffuseGI + specularGI) * surface.occlusion;
}

bool IsRenderingLayersOverlap(Surface surface,Light light){
    return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

float3 CalcLighting(Surface surface,GI gi,BRDF brdf,ShadowData shadowData){
    float3 col = CalcGI(surface,gi,brdf);

    int lightCount = GetLightCount();
    for(int j=0;j<lightCount;j++){
        Light l = GetLight(j,surface,shadowData);
        if(IsRenderingLayersOverlap(surface,l))
            col += CalcLight(l,surface,brdf);
    }

    #if defined(_LIGHTS_PER_OBJECT)
        for(int i=0;i<min(unity_LightData.y,8);i++){
            int lightIndex = unity_LightIndices[(uint)i/4][(uint)i%4];
            Light l = GetOtherLight(lightIndex,surface,shadowData);

            if(IsRenderingLayersOverlap(surface,l))
                col += CalcLight(l,surface,brdf);
        }
    #else
        int otherLightCount = GetOtherLightCount();
        for(int i=0;i<otherLightCount;i++){
            Light l = GetOtherLight(i,surface,shadowData);
            // return l.attenuation;
            if(IsRenderingLayersOverlap(surface,l))
                col += CalcLight(l,surface,brdf);
        }
    #endif
    return col;
}
#endif //CRP_LIGHTING_HLSL