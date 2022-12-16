Shader "CRP/Utils/ColorGrading"
{
    Properties
    {

    }

    HLSLINCLUDE
    #include "ColorGradingPass.hlsl"
    ENDHLSL

    SubShader
    {
        Cull off
        zwrite off
        ztest always

        Pass //0
        {
            Name "ColorGrading no Tone"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "ColorGrading Reinhard"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragReinhard
            ENDHLSL
        }
        Pass //2 
        {
            Name "ColorGrading ACESFitted"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragACESFitted
            ENDHLSL
        }
        Pass
        {
            Name "ColorGrading ACESFilm"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragACESFilm
            ENDHLSL
        }       
        Pass //4
        {
            Name "ColorGrading ACES"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragACES
            ENDHLSL
        }
        Pass 
        {
            Name "ColorGrading Neutral"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragNeutralTone
            ENDHLSL
        }     
        Pass //6
        {
            Name "ColorGrading GTTone"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragGTTone
            ENDHLSL
        }
    }
}
