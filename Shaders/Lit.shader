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

        [Header(Render States)]
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcMode("_SrcMode",int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstMode("_DstMode",int) = 0
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Passes/LitPass.hlsl"
            
            ENDHLSL
        }
    }
}
