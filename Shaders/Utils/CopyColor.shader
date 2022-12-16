Shader "CRP/Utils/CopyColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "../Libs/UnityInput.hlsl"
    #include "../../../PowerShaderLib/Lib/Colors.hlsl"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    sampler2D _MainTex;

    CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    bool _ApplyColorGrading;
    CBUFFER_END

    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.uv = v.uv;
        #if defined(UNITY_UV_STARTS_AT_TOP)
        o.uv.y = 1-o.uv.y;
        #endif
        return o;
    }

    half4 frag (v2f i) : SV_Target
    {
        half4 col = tex2D(_MainTex, i.uv);
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
