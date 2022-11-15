#if !defined(CRP_SHADOW_CASTER_PASS_HLSL)
#define CRP_SHADOW_CASTER_PASS_HLSL
#include "../Libs/UnityInput.hlsl"

TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(half4,_Color)
    UNITY_DEFINE_INSTANCED_PROP(float,_CullOff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
#define _Color UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Color)
#define _MainTex_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_MainTex_ST)
#define _CullOff UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_CullOff)

struct appdata{
    float4 vertex:POSITION;
    float2 uv:TEXCOORD;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f{
    float4 vertex:SV_POSITION;
    float2 uv:TEXCOORD;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2f vert(appdata v){
    v2f o = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v,o);

    float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
    o.vertex = TransformWorldToHClip(worldPos);
    o.uv = TRANSFORM_TEX(v.uv,_MainTex);
    return o;
}

void frag(v2f i){
    UNITY_SETUP_INSTANCE_ID(i);
    #if defined(_CLIPPING)
        half4 mainTex = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex) * _Color;
        clip(mainTex.w - _CullOff);
    #endif
}

#endif //CRP_SHADOW_CASTER_PASS_HLSL