Shader "CRP/Lit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("_Color",color) = (1,1,1,1)

        _PBRMask("_PBRMask",2d)="white"{}
        _Metallic("_Metallic",range(0,1)) = 0.5
        _Smoothness("_Smoothness",range(0,1)) = 0.5
        _Occlusion("_Occlusion",range(0,1)) = 0
        
        [Header(Alpha)]
        [Toggle(_PREMULTIPLY_ALPHA)]_PremulAlpha("_PremulAlpha",int) = 0
        [Toggle(_CLIPPING)]_Cull("_Cull",int) = 0
        _CullOff("_CullOff",range(0,1)) = 0.5

        [Enum(UnityEngine.Rendering.BlendMode)]_SrcMode("_SrcMode",int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstMode("_DstMode",int) = 0

        [Header(ShadowCaster)]
        [GroupEnum(,SHADOW_NONE SHADOW_HARD SHADOW_DITHER,true)]_ShadowMode("_ShadowMode",int) = 1
        [GroupToggle(,_RECEIVE_SHADOW_OFF)]_ReceiveShadowOff("_ReceiveShadowOff",int) = 1

        [Header(Render States)]
        [GroupToggle()]_ZWrite("_ZWrite",int) = 1
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("_CullMode",int) = 2
    }

    HLSLINCLUDE

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            zwrite[_ZWrite]
            cull[_CullMode]
            blend [_SrcMode][_DstMode]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma shader_feature _RECEIVE_SHADOW_OFF

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
            #pragma shader_featuer SHADOW_HARD

            #include "Passes/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
    }
    CustomEditor "CRPShaderGUI"
}
