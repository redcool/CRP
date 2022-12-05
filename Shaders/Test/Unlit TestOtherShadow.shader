Shader "CRP/Unlit TestOtherShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("_Color",color) = (1,1,1,1)
        [IntRange]_TileId("_TileId",range(0,3)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcMode("_SrcMode",int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstMode("_DstMode",int) = 0
        [GroupToggle()]_ZWrite("_ZWrite",int) = 1
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("_CullMode",int) = 2
    }

    HLSLINCLUDE
    #include "../Libs/UnityInput.hlsl"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
        float3 worldPos:TEXCOORD1;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };
#define MAX_SHADOWED_COUNT 16
#define MAX_OTHER_LIGHT_COUNT 64

    sampler2D _MainTex;
    sampler2D _OtherShadowMap;
    float4x4 _OtherShadowMatrices[MAX_SHADOWED_COUNT];
    float4 _OtherShadowData[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    int _OtherLightCount;

    // CBUFFER_START(UnityPerMaterial)
    UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
        UNITY_DEFINE_INSTANCED_PROP(half4,_Color)
    UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
    #define _Color UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Color)
    #define _MainTex_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_MainTex_ST)
    // CBUFFER_END

    v2f vert (appdata v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.worldPos = TransformObjectToWorld(v.vertex);

        return o;
    }

    float GetShadow(float3 worldPos){
        float shadow = 0;
        for(int i=0;i<_OtherLightCount;i++){
            float tileIndex = _OtherShadowData[i].x;
            bool isPoint = _OtherShadowData[i].y;

            float3 lightPos = _OtherLightPositions[i];
            float3 dir = lightPos - worldPos;
            
            float3 lightDir = _OtherLightDirections[i];

            if(isPoint){
                float faceOffset = CubeMapFaceID(-dir);
                tileIndex += faceOffset;
            }
            float4 posShadow = mul(_OtherShadowMatrices[tileIndex],float4(worldPos,1));
            posShadow.xyz /= posShadow.w;

            shadow += tex2D(_OtherShadowMap,posShadow.xy).x < posShadow.z;
            // return shadow;
        }
        return shadow;
    }

    half4 frag (v2f i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float shadow = GetShadow(i.worldPos);
        return shadow;

        half4 col = tex2D(_MainTex, i.uv) * _Color;
        return col;
    }

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
            
            ENDHLSL
        }
    }
}
