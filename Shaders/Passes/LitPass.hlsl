#if !defined(CRP_LIT_PASS_HLSL)
    #include "Libs/UnityInput.hlsl"
    #include "Libs/Surface.hlsl"
    #include "Libs/Shadows.hlsl"
    #include "Libs/Light.hlsl"
    #include "Libs/BRDF.hlsl"
    #include "Libs/Lighting.hlsl"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float3 normal:NORMAL;
        float4 tangent:TANGENT;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
        float4 tSpace0:TEXCOORD2;
        float4 tSpace1:TEXCOORD3;
        float4 tSpace2:TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    sampler2D _MainTex;
    sampler2D _PBRMask;

    // CBUFFER_START(UnityPerMaterial)
    UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
        UNITY_DEFINE_INSTANCED_PROP(half4,_Color)
        UNITY_DEFINE_INSTANCED_PROP(float,_Metallic)
        UNITY_DEFINE_INSTANCED_PROP(float,_Smoothness)
        UNITY_DEFINE_INSTANCED_PROP(float,_Occlusion)
        UNITY_DEFINE_INSTANCED_PROP(float,_CullOff)
    UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
    #define _Color UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Color)
    #define _MainTex_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_MainTex_ST)
    #define _Metallic UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Metallic)
    #define _Smoothness UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Smoothness)
    #define _Occlusion UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Occlusion)
    #define _CullOff UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_CullOff)
    // CBUFFER_END

    v2f vert (appdata v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        
        float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
        float3 wn = TransformObjectToWorldNormal(v.normal);
        float3 wt = TransformObjectToWorldDir(v.tangent.xyz);
        float3 wb = cross(wn,wt) * v.tangent.w;
        o.tSpace0 = float4(wt.x,wb.x,wn.x,worldPos.x);
        o.tSpace1 = float4(wt.y,wb.y,wn.y,worldPos.y);
        o.tSpace2 = float4(wt.z,wb.z,wn.z,worldPos.z);

        return o;
    }

    half4 frag (v2f i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float3 worldPos = float3(i.tSpace0.w,i.tSpace1.w,i.tSpace2.w);
        float3 wn = float3(i.tSpace0.z,i.tSpace1.z,i.tSpace2.z);
        wn = normalize(wn);

        half4 mainTex = tex2D(_MainTex, i.uv) * _Color;
        #if defined(_CLIPPING)
            clip(mainTex.w - _CullOff);
        #endif
        
        half4 pbrMask = tex2D(_PBRMask,i.uv);

        Surface surface = (Surface)0;
        surface.normal = wn;
        surface.albedo = mainTex.xyz;
        surface.alpha = mainTex.w;

        surface.metallic = pbrMask.x * _Metallic;
        surface.oneMinusReflectivity = 0.96 - 0.96 * surface.metallic;
        surface.smoothness = pbrMask.y * _Smoothness;
        surface.occlusion = lerp(1,pbrMask.z , _Occlusion);
        surface.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
        PremultiplyAlpha(surface);
        surface.worldPos = worldPos;
        
        half3 col = GetLighting(surface);
        return half4(col,surface.alpha);
    }
#endif //CRP_LIT_PASS_HLSL