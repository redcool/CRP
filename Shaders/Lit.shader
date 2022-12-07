Shader "CRP/Lit"
{
    Properties
    {
        // [Group(_)]
        [GroupItem(_)] _MainTex ("Texture", 2D) = "white" {}
        [GroupItem(_)] _Color("_Color",color) = (1,1,1,1)

        [GroupItem(_)] _PBRMask("_PBRMask(M,S,O)",2d)="white"{}
        [GroupItem(_)] _Metallic("_Metallic",range(0,1)) = 0.5
        [GroupItem(_)] _Smoothness("_Smoothness",range(0,1)) = 0.5
        [GroupItem(_)] _Occlusion("_Occlusion",range(0,1)) = 0
        
        [Header(Env)]
        _FresnelIntensity("_FresnelIntensity",range(0,1))=1

        [Header(NormalMap)]
        [GroupToggle(,_NORMAL_MAP_ON)]_NormalMapOn("_NormalMapOn",int) = 0
        _NormalMap("_NormalMap",2d)="bump"{}
        _NormalMapScale("_NormalMapScale",range(0,5)) = 1

        [Header(Detail)]
        [GroupToggle(,_DETAIL_MAP_ON)]_DetailMapOn("_DetailMapOn",int) = 0
        _DetailMap("_DetailMap(w : mask)",2d) = "white"{}
        _DetailMapScale("_DetailMapScale",range(0,2)) = 1
        _DetailNormalMap("_DetailNormalMap",2d)="bump"{}
        _DetailNormalMapScale("_DetailNormalMapScale",range(0,5)) = 1

        [Header(Emission)]
        [GroupToggle(,_EMISSION_MAP_ON)]_EmissionMapOn("_EmissionMapOn",int) = 0
        _EmissionMap("_EmissionMap",2d)="white"{}
        [hdr]_EmissionColor("_EmissionColor",color) = (0,0,0,0)
        
        [Header(Alpha)]
        [Toggle(_PREMULTIPLY_ALPHA)]_PremulAlpha("_PremulAlpha",int) = 0
        [Toggle(_CLIPPING)]_Cull("_Cull",int) = 0
        _CullOff("_CullOff",range(0,1)) = 0.5

        [Enum(UnityEngine.Rendering.BlendMode)]_SrcMode("_SrcMode",int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstMode("_DstMode",int) = 0

        [Header(ShadowCaster)]
        [GroupEnum(,SHADOW_HARD SHADOW_DITHER,true)]_ShadowMode("_ShadowMode",int) = 1
        [GroupToggle(,_RECEIVE_SHADOW_OFF)]_ReceiveShadowOff("_ReceiveShadowOff",int) = 1

        [Header(Render States)]
        [GroupToggle()]_ZWrite("_ZWrite",int) = 1
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("_CullMode",int) = 2
    }

    HLSLINCLUDE
        #include "Passes/LitInput.hlsl"
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque"}
        LOD 100

        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            zwrite[_ZWrite]
            cull[_CullMode]
            blend [_SrcMode][_DstMode]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _RECEIVE_SHADOW_OFF
            #pragma shader_feature _NORMAL_MAP_ON
            #pragma shader_feature _DETAIL_MAP_ON
            #pragma shader_feature _EMISSION_MAP_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _SHADOW_MASK_DISTANCE _SHADOW_MASK
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ _LIGHTS_PER_OBJECT

            #pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7

            #include "Passes/LitPass.hlsl"
            
            ENDHLSL
        }

        Pass{
            Tags{"LightMode"="ShadowCaster"}
            colorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING
            #pragma shader_feature SHADOW_DITHER
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "Passes/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
        pass{
            Tags{"LightMode"="Meta"}
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Passes/MetaPass.hlsl"
            
            ENDHLSL
        }
    }
    CustomEditor "CRPShaderGUI"
}
