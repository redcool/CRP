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

        Pass  // 1
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
        Pass //3
        {
            Name "Horizontal"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragHorizontal
            
            ENDHLSL
        }
        Pass 
        {
            Name "Vertical"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragVertical
            
            ENDHLSL
        }
        Pass //5
        {
            Name "CombineScatter"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragCombineScatter
            
            ENDHLSL
        }
        Pass 
        {
            Name "CombineScatterFinaly"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragCombineScatterFinal
            
            ENDHLSL
        }        
    }
}
