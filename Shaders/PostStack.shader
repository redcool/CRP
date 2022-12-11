Shader "Hidden/CRP/PostStack"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Passes/PostPass.hlsl"
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
            zwrite off
            ztest always
            cull off

        Pass 
        {
            name "Prefilter"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragPrefilter
            
            ENDHLSL
        }

        Pass 
        {
            Name "Copy"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragCopy
            
            ENDHLSL
        }
        Pass 
        {
            Name "Combine"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragCombine
            
            ENDHLSL
        }
    }
}
