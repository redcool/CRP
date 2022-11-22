#if !defined(CRP_GI_HLSL)
#define CRP_GI_HLSL
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

// Unity specific
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);
TEXTURECUBE(unity_SpecCube1);
SAMPLER(samplerunity_SpecCube1);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
TEXTURE2D_ARRAY(unity_Lightmaps);
SAMPLER(samplerunity_Lightmaps);

// Dynamic lightmap
TEXTURE2D(unity_DynamicLightmap);
SAMPLER(samplerunity_DynamicLightmap);
// TODO ENLIGHTEN: Instanced GI

// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);
TEXTURE2D_ARRAY(unity_LightmapsInd);
TEXTURE2D(unity_DynamicDirectionality);
// TODO ENLIGHTEN: Instanced GI
// TEXTURE2D_ARRAY(unity_DynamicDirectionality);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);
TEXTURE2D_ARRAY(unity_ShadowMasks);
SAMPLER(samplerunity_ShadowMasks);

TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

struct GI{
    float3 diffuse;
};

half3 SampleLightmap(float2 lmapUV){
    half3 lmap = SampleSingleLightmap(unity_Lightmap,samplerunity_Lightmap,lmapUV,float4(1,1,0,0),
        #if defined(UNITY_LIGHTMAP_FULL_HDR)
            false,
        #else
            true,
        #endif
        float4(LIGHTMAP_HDR_MULTIPLIER,LIGHTMAP_HDR_EXPONENT,0,0)
        );
    return lmap;
}

half3 SampleSH9(float3 normal){
    float4 coefficients[7];
    coefficients[0] = unity_SHAr;
    coefficients[1] = unity_SHAg;
    coefficients[2] = unity_SHAb;
    coefficients[3] = unity_SHBr;
    coefficients[4] = unity_SHBg;
    coefficients[5] = unity_SHBb;
    coefficients[6] = unity_SHC;
    return SampleSH9(coefficients,normal);
}

half3 SampleProbes(float3 position,float3 normal){
    if(unity_ProbeVolumeParams.x){
        return SampleProbeVolumeSH4(
            TEXTURE3D_ARGS(unity_ProbeVolumeSH,samplerunity_ProbeVolumeSH),
            position,normal,
            unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y,unity_ProbeVolumeParams.z,
            unity_ProbeVolumeMin.xyz,unity_ProbeVolumeSizeInv.xyz
        );
    }
    return SampleSH9(normal);
}

GI GetGI(float2 lightmapUV,Surface surface){
    GI gi = (GI)0;
    #if defined(LIGHTMAP_ON)
    gi.diffuse += SampleLightmap(lightmapUV);
    #else
    gi.diffuse += SampleProbes(surface.worldPos,surface.normal); 
    #endif

    return gi;
}

#endif //CRP_GI_HLSL