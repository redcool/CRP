#if !defined(CRP_LIT_PASS_HLSL)

    #include "Libs/Surface.hlsl"
    #include "Libs/Shadows.hlsl"
    #include "Libs/Light.hlsl"
    #include "Libs/BRDF.hlsl"
    #include "Libs/GI.hlsl"
    #include "Libs/Lighting.hlsl"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float2 uv1:TEXCOORD1;
        float2 uv2:TEXCOORD2;
        float3 normal:NORMAL;
        float4 tangent:TANGENT;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 tSpace0:TEXCOORD1;
        float4 tSpace1:TEXCOORD2;
        float4 tSpace2:TEXCOORD3;
        float2 lightmapUV : TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    v2f vert (appdata v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        
        float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
        float3 worldNormal = TransformObjectToWorldNormal(v.normal);
        float3 wt = TransformObjectToWorldDir(v.tangent.xyz);
        float3 wb = cross(worldNormal,wt) * v.tangent.w;
        o.tSpace0 = float4(wt.x,wb.x,worldNormal.x,worldPos.x);
        o.tSpace1 = float4(wt.y,wb.y,worldNormal.y,worldPos.y);
        o.tSpace2 = float4(wt.z,wb.z,worldNormal.z,worldPos.z);

        o.lightmapUV = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

        return o;
    }

    half4 frag (v2f i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        ClipLOD(i.vertex.xy);

        float3 worldPos = float3(i.tSpace0.w,i.tSpace1.w,i.tSpace2.w);
        float3 worldNormal = float3(i.tSpace0.z,i.tSpace1.z,i.tSpace2.z);
        worldNormal = normalize(worldNormal);

        half4 mainTex = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv) * _Color;
        #if defined(_CLIPPING)
            clip(mainTex.w - _CullOff);
        #endif
        
        half4 pbrMask = SAMPLE_TEXTURE2D(_PBRMask,sampler_PBRMask,i.uv);

        Surface surface = (Surface)0;
        surface.normal = worldNormal;
        surface.albedo = mainTex.xyz;
        surface.alpha = mainTex.w;

        surface.metallic = pbrMask.x * _Metallic;
        surface.oneMinusReflectivity = 0.96 - 0.96 * surface.metallic;
        surface.smoothness = pbrMask.y * _Smoothness;
        surface.occlusion = lerp(1,pbrMask.z , _Occlusion);
        surface.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
        PremultiplyAlpha(surface);
        surface.worldPos = worldPos;
        surface.depth = -TransformWorldToView(worldPos).z;
        surface.dither = InterleavedGradientNoise(i.vertex.xy,0);
        surface.fresnelIntensity = _FresnelIntensity;

        BRDF brdf = GetBRDF(surface);
// return SampleIBL(unity_SpecCube0,samplerunity_SpecCube0,1,surface.viewDir,surface.normal,brdf.roughness).xyzx;
// return SampleUnityIBL(surface.viewDir,surface.normal,brdf.roughness).xyzx;
        GI gi = GetGI(i.lightmapUV,surface,brdf);
// return gi.specular.xyzx;
        ShadowData shadowData = GetShadowData(surface);
        shadowData.shadowMask = gi.shadowMask;

// return _DirectionalLightShadowData[0].w==1;
        half3 col = CalcLighting(surface,gi,brdf,shadowData);

        col.xyz += GetEmission(i.uv);
        return half4(col,surface.alpha);
    }
#endif //CRP_LIT_PASS_HLSL