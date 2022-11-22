#if !defined(CRP_META_PASS_HLSL)
#define CRP_META_PASS_HLSL

    #include "Libs/Surface.hlsl"
    #include "Libs/Shadows.hlsl"
    #include "Libs/Light.hlsl"
    #include "Libs/BRDF.hlsl"
    #include "Libs/GI.hlsl"
    #include "Libs/Lighting.hlsl"

    struct appdata{
        float3 position:POSITION;
        float2 uv:TEXCOORD;
        float2 uv1:TEXCOORD1;
    };

    struct v2f{
        float4 position:SV_POSITION;
        float2 uv:TEXCOORD;
    };

    bool4 unity_MetaFragmentControl;
    float unity_OneOverOutputBoost;
    float unity_MaxOutputValue;

    v2f vert(appdata i){
        i.position.xy = i.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
        i.position.z = i.position.z > 0 ? FLT_MIN : 0;
        v2f o = (v2f)o;
        o.position = TransformWorldToHClip(i.position);
        o.uv = TRANSFORM_TEX(i.uv,_MainTex);
        return o;
    }

    float4 frag(v2f v):SV_Target{
        // return float4(1,0,0,0);
        float4 mainTex = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);
        Surface surface = (Surface)0;
        surface.albedo = mainTex.xyz;
        surface.metallic = _Metallic;
        surface.smoothness = _Smoothness;
        BRDF brdf = GetBRDF(surface);

        float4 meta = 0;
        if(unity_MetaFragmentControl.x){
            meta = float4(brdf.diffuse,1);
            meta.xyz += brdf.specular * brdf.a * 0.5;
            meta.xyz = min(PositivePow(meta.xyz,unity_OneOverOutputBoost),unity_MaxOutputValue);
        }
        else if(unity_MetaFragmentControl.y){
            meta.xyz = GetEmission(v.uv);
        }
        return meta;
    }


#endif //CRP_META_PASS_HLSL