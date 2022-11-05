Shader "CRP/Utils/CopyDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "../Libs/UnityInput.hlsl"

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

    TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

    CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
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

    float4 frag (v2f i) : SV_Target
    {
        float depth = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).x;
        return depth;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            
            ENDHLSL
        }
    }
}
