Shader "CRP/Utils/CopyColor"
{
    Properties
    {
        // _SourceTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "../Libs/Common.hlsl"
    #include "../../../PowerShaderLib/Lib/Colors.hlsl"

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    sampler2D _SourceTex;

    bool _ApplyColorGrading;

    v2f vert (uint vid:SV_VERTEXID)
    {
        v2f o;
        FullScreenTriangleVert(vid,o.vertex/**/,o.uv/**/);

        return o;
    }

    half4 frag (v2f i) : SV_Target
    {
        half4 col = tex2D(_SourceTex, i.uv);
        if(_ApplyColorGrading)
            col.xyz = ApplyColorGradingLUT(col.xyz);
        return col;
    }
    ENDHLSL

    SubShader
    {
        Cull off
        zwrite off
        ztest always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
