#if !defined(POST_PASS_HLSL)
#define POST_PASS_HLSL
    #include "Libs/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    TEXTURE2D(_SourceTex);SAMPLER(sampler_SourceTex);
    TEXTURE2D(_SourceTex2);SAMPLER(sampler_SourceTex2);
    float4 _SourceTex_Texel;

    // TEXTURE2D(_CameraTexture);SAMPLER(sampler_CameraTexture);
    
    float4 _BloomThreshold;
    float _BloomIntensity;
    bool _BloomCombineBicubicFilter;

    v2f vert(uint vid:SV_VERTEXID){
        v2f o = (v2f)0;
        FullScreenTriangleVert(vid,o.vertex/**/,o.uv/**/);
        return o;
    }

    float4 SampleSourceTex(float2 uv){
        return SAMPLE_TEXTURE2D(_SourceTex,sampler_SourceTex,uv);
    }
    float4 SampleSourceTex2(float2 uv){
        return SAMPLE_TEXTURE2D(_SourceTex2,sampler_SourceTex2,uv);
    }
    float4 SampleBloomTex(float2 uv){
        if(_BloomCombineBicubicFilter){
            return SampleTexture2DBicubic(_SourceTex,sampler_SourceTex,uv,_SourceTex_Texel.zwxy,1,0);
        }else{
            return SampleSourceTex(uv);
        }
    }

    float3 ApplyBloomThreshold(float3 col){
        float luma = dot(float3(0.2,0.7,0.02),col);
        // luma = smoothstep(0,1,luma);
        
        luma -= _BloomThreshold.x + _BloomThreshold.z;
        return clamp(luma * col,0,_BloomThreshold.w);

        // float maxValue = Max3(col.x,col.y,col.z);
        // float soft = maxValue + _BloomThreshold.y;
        // soft = clamp(soft,0,_BloomThreshold.z);
        // float weight = max(soft,maxValue - _BloomThreshold.x);
        // weight /= max(maxValue,1e-5);
        // return col * weight;
    }

    float4 fragPrefilter (v2f i) : SV_Target{
        float4 col = SampleSourceTex(i.uv);
        return float4(ApplyBloomThreshold(col.xyz),1);
    }

    float4 fragCopy(v2f i):SV_Target{
        return SampleSourceTex(i.uv);
    }

    float4 fragHorizontal(v2f i):SV_TARGET{
        static float weights[5] = {.1,.2,.4,.2,.1};
        static float2 uvOffsets[5] = {
            float2(-2,0)* _SourceTex_Texel.xy,
            float2(-1,0)* _SourceTex_Texel.xy,
            float2(0,0)* _SourceTex_Texel.xy,
            float2(1,0)* _SourceTex_Texel.xy,
            float2(2,0)* _SourceTex_Texel.xy
        };
        float4 c = 0;
        for(uint x=0;x<5;x++){
            c += SampleSourceTex(i.uv+uvOffsets[x]) * weights[x];
        }
        return c;
    }

    float4 fragVertical(v2f i):SV_TARGET{
        static float weights[5] = {.1,.2,.4,.2,.1};
        static float2 uvOffsets[5] = {
            float2(0,-2)* _SourceTex_Texel.xy,
            float2(0,-1)* _SourceTex_Texel.xy,
            float2(0,0)* _SourceTex_Texel.xy,
            float2(0,1)* _SourceTex_Texel.xy,
            float2(0,2)* _SourceTex_Texel.xy
        };
        float4 c = 0;
        for(uint x=0;x<5;x++){
            c += SampleSourceTex(i.uv+uvOffsets[x]) * weights[x];
        }
        return c;
    }

    float4 fragCombineScatter(v2f i):SV_Target{
        float4 bloomCol = SampleBloomTex(i.uv);

        float4 screenCol = SampleSourceTex2(i.uv);
        return float4(lerp(screenCol.xyz,bloomCol.xyz,_BloomIntensity),screenCol.w);
    }

    float4 fragCombine(v2f i):SV_Target{
        float4 bloomCol = SampleBloomTex(i.uv);
        float4 screenCol = SampleSourceTex2(i.uv);
        return float4(bloomCol.xyz * _BloomIntensity + screenCol.xyz,screenCol.w);
    }
    float4 fragCombineScatterFinal(v2f i):SV_Target{
        float4 bloomCol = SampleBloomTex(i.uv);

        float4 screenCol = SampleSourceTex2(i.uv);
        bloomCol.xyz += screenCol.xyz ;//- ApplyBloomThreshold(screenCol.xyz);

        return float4(lerp(screenCol.xyz,bloomCol.xyz,_BloomIntensity),screenCol.w);
    }
#endif //POST_PASS_HLSL