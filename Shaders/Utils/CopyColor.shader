Shader "CRP/Utils/CopyColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "CopyColorPass.hlsl"
    ENDHLSL

    SubShader
    {
        Cull off
        zwrite off
        ztest always

        Pass //0
        {
            Name "CopyColor"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "CopyColor Reinhard"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragReinhard
            ENDHLSL
        }
        Pass //2 
        {
            Name "CopyColor ACESFitted"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragACESFitted
            ENDHLSL
        }
        Pass
        {
            Name "CopyColor ACESFilm"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragACESFilm
            ENDHLSL
        }       
        Pass //4
        {
            Name "CopyColor GTTone"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragGTTone
            ENDHLSL
        }           
    }
}
