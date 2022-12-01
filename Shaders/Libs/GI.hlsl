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
    float3 diffuse; // sh,lightmap
    float3 specular; // envColor
    float4 shadowMask;
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

half4 SampleBakedShadow(float2 uv,float3 position){
    #if defined(LIGHTMAP_ON)
        half4 shadowMask = SAMPLE_TEXTURE2D(unity_ShadowMask,samplerunity_ShadowMask,uv);
        return shadowMask;
    #else
        if(unity_ProbeVolumeParams.x){ // lppv
            return SampleProbeOcclusion(
                TEXTURE3D_ARGS(unity_ProbeVolumeSH,samplerunity_ProbeVolumeSH),
                position,
                unity_ProbeVolumeWorldToObject,
                unity_ProbeVolumeParams.y,unity_ProbeVolumeParams.z,
                unity_ProbeVolumeMin.xyz,unity_ProbeVolumeSizeInv.xyz
            );
        }
        return unity_ProbesOcclusion;
    #endif
}

half3 SampleIBL(TEXTURECUBE_PARAM(iblMap,sampler_iblMap),float4 hdrParams,float3 viewDir,float3 normal,float rough){
    float3 reflectDir = reflect(-viewDir,normal);
    float mip = rough * (1.7 - 0.7 * rough) * 6;
    float4 envColor = SAMPLE_TEXTURECUBE_LOD(iblMap,sampler_iblMap,reflectDir,mip);
    envColor.xyz = DecodeHDREnvironment(envColor,hdrParams);
    return envColor.xyz;
}

float3 SampleUnityIBL(float3 viewDir,float3 normal,float rough){
    // float4 hdrParams = max(float4(1,1,0,0),unity_SpecCube0_HDR);
    return SampleIBL(unity_SpecCube0,samplerunity_SpecCube0,unity_SpecCube0_HDR,viewDir,normal,rough);
}

GI GetGI(float2 lightmapUV,Surface surface,BRDF brdf){
    GI gi = (GI)0;
    #if defined(LIGHTMAP_ON)
    gi.diffuse += SampleLightmap(lightmapUV);
    #else
    gi.diffuse += SampleProbes(surface.worldPos,surface.normal); 
    #endif

    gi.specular = SampleUnityIBL(surface.viewDir,surface.normal,brdf.a);

    #if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK)
    gi.shadowMask = SampleBakedShadow(lightmapUV,surface.worldPos);
    #endif

    return gi;
}

#endif //CRP_GI_HLSL