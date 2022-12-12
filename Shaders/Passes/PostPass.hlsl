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
    TEXTURE2D(_CameraTexture);SAMPLER(sampler_CameraTexture);
    float4 _SourceTex_Texel;

    v2f vert(uint vid:SV_VERTEXID){
        v2f o = (v2f)0;
        o.vertex = float4(
            vid <= 1 ? -1 : 3,
            vid == 1 ? 3 : -1,
            0,1
        );
        o.uv = float2(
            vid <= 1 ? 0 : 2,
            vid == 1 ? 2 : 0
        );
        #if defined(UNITY_UV_STARTS_AT_TOP)
        // if(_ProjectionParams.x < 0)
            o.uv.y = 1 - o.uv.y;
        #endif
        return o;
    }

    half4 SampleSourceTex(float2 uv){
        return SAMPLE_TEXTURE2D(_SourceTex,sampler_SourceTex,uv);
    }
    half4 SampleCameraTex(float2 uv){
        return SAMPLE_TEXTURE2D(_CameraTexture,sampler_CameraTexture,uv);
    }

    float4 _BloomThreshold;
    half3 ApplyBloomThreshold(half3 col){
        float luma = dot(half3(0.2,0.7,0.02),col);
        // luma = smoothstep(0,1,luma);
        
        luma -= _BloomThreshold.x + _BloomThreshold.z;
        return luma * col;

        float maxValue = Max3(col.x,col.y,col.z);
        float soft = maxValue + _BloomThreshold.y;
        soft = clamp(soft,0,_BloomThreshold.z);
        float weight = max(soft,maxValue - _BloomThreshold.x);
        weight /= max(maxValue,1e-5);
        return col * weight;
    }

    half4 fragPrefilter (v2f i) : SV_Target{
        half4 col = SampleSourceTex(i.uv);
        return half4(ApplyBloomThreshold(col.xyz),1);
    }

    half4 fragCopy(v2f i):SV_Target{
        return SampleSourceTex(i.uv);
    }

    float _BloomIntensity;
    bool _BloomCombineBicubicFilter;
    half4 fragCombine(v2f i):SV_Target{
        half4 bloomCol = 0;
        if(_BloomCombineBicubicFilter){
            bloomCol = SampleTexture2DBicubic(_SourceTex,sampler_SourceTex,i.uv,_SourceTex_Texel.zwxy,1,0);
        }else{
            bloomCol = SampleSourceTex(i.uv);
        }

        half4 screenCol = SampleCameraTex(i.uv);
        return half4(bloomCol.xyz*_BloomIntensity+screenCol.xyz,1);
    }

    half4 fragHorizontal(v2f i):SV_TARGET{
        static float weights[5] = {.1,.2,.4,.2,.1};
        static float2 uvOffsets[5] = {
            float2(-2,0)* _SourceTex_Texel.xy,
            float2(-1,0)* _SourceTex_Texel.xy,
            float2(0,0)* _SourceTex_Texel.xy,
            float2(1,0)* _SourceTex_Texel.xy,
            float2(2,0)* _SourceTex_Texel.xy
        };
        half4 c = 0;
        for(uint x=0;x<5;x++){
            c += SampleSourceTex(i.uv+uvOffsets[x]) * weights[x];
        }
        return c;
    }

    half4 fragVertical(v2f i):SV_TARGET{
        static float weights[5] = {.1,.2,.4,.2,.1};
        static float2 uvOffsets[5] = {
            float2(0,-2)* _SourceTex_Texel.xy,
            float2(0,-1)* _SourceTex_Texel.xy,
            float2(0,0)* _SourceTex_Texel.xy,
            float2(0,1)* _SourceTex_Texel.xy,
            float2(0,2)* _SourceTex_Texel.xy
        };
        half4 c = 0;
        for(uint x=0;x<5;x++){
            c += SampleSourceTex(i.uv+uvOffsets[x]) * weights[x];
        }
        return c;
    }
#endif //POST_PASS_HLSL