Shader "CRP/Test/ShowScreenTarget"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcMode("_SrcMode",int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstMode("_DstMode",int) = 0
        [GroupToggle]_ZWrite("_ZWrite",int) = 1
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("_CullMode",int) = 2

        [GroupToggle]_ShowColorTarget("_ShowColorTarget",int) = 0
    }

    HLSLINCLUDE
    #include "Libs/UnityInput.hlsl"

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
    sampler2D _CameraDepthTexture;
    sampler2D _CameraTexture;

    CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    int _ShowColorTarget;
    CBUFFER_END

    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);

        return o;
    }

    half4 frag (v2f i) : SV_Target
    {
        half4 col = tex2D(_CameraDepthTexture, i.uv);
        if(_ShowColorTarget)
            col = tex2D(_CameraTexture,i.uv);
        return col;
    }
    ENDHLSL

    SubShader
    {
        LOD 100

        Pass
        {
            zwrite[_ZWrite]
            cull[_CullMode]
            blend [_SrcMode][_DstMode]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            
            ENDHLSL
        }
    }
}
