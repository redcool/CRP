#if !defined(POST_PASS_HLSL)
#define POST_PASS_HLSL
    #include "Libs/Common.hlsl"

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    sampler2D _SourceTex;
    sampler2D _CameraTexture;
    // float4 _SourceTex_Texel;

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

    float4 _BloomThreshold;
    half3 ApplyBloomThreshold(half3 col){
        float maxValue = Max3(col.x,col.y,col.z);
        float soft = maxValue + _BloomThreshold.y;
        soft = clamp(soft,0,_BloomThreshold.z);
        float weight = max(soft,maxValue - _BloomThreshold.x);
        weight /= max(maxValue,1e-5);
        return col * weight;
    }

    half4 fragPrefilter (v2f i) : SV_Target{
        half4 col = tex2D(_SourceTex,i.uv);
        return half4(ApplyBloomThreshold(col.xyz),1);
    }

    half4 fragCopy(v2f i):SV_Target{
        return tex2D(_SourceTex,i.uv);
    }

    float _BloomIntensity;
    half4 fragCombine(v2f i):SV_Target{
        half4 col = tex2D(_SourceTex,i.uv);
        half4 col2 = tex2D(_CameraTexture,i.uv);
        return half4(col.xyz*_BloomIntensity+col2.xyz,1);
    }
#endif //POST_PASS_HLSL