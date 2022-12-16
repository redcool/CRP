#if !defined(COLOR_GRADING_PASS_HLSL)
#define COLOR_GRADING_PASS_HLSL
    #include "../Libs/Common.hlsl"
    #include "../../../PowerShaderLib/Lib/ToneMappers.hlsl"
    #include "../../../PowerShaderLib/Lib/Colors.hlsl"

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

    v2f vert (uint vid:SV_VertexID)
    {
        v2f o;
        FullScreenTriangleVert(vid,o.vertex/**/,o.uv/**/);
        // o.vertex = TransformObjectToHClip(v.vertex.xyz);
        // o.uv = v.uv;
        // #if defined(UNITY_UV_STARTS_AT_TOP)
        // o.uv.y = 1-o.uv.y;
        // #endif
        return o;
    }

    float4 frag (v2f i) : SV_Target{
        float3 c = ColorGradingLUT(i.uv);
        return float4(c,1);
    }
    float4 fragReinhard(v2f i):SV_Target{
        float3 c = ColorGradingLUT(i.uv);
        return float4(Reinhard(c),1);
    }

    float4 fragACESFitted(v2f i):SV_Target{
        float3 c = ColorGradingLUT(i.uv);
        return float4(ACESFitted(c),1);
    }
    float4 fragACESFilm(v2f i):SV_Target{
        float3 c = ColorGradingLUT(i.uv);
        return float4(ACESFilm(c),1);
    }
    float4 fragACES(v2f i):SV_TARGET{
        float3 c = ColorGradingLUT(i.uv);
        return float4(AcesTonemap(c),1); 
    }

    float4 fragNeutralTone(v2f i):SV_TARGET{
        float3 c = ColorGradingLUT(i.uv);
        return float4(NeutralTonemap(c),1); 
    }

    float4 fragGTTone(v2f i):SV_Target{
        float3 c = ColorGradingLUT(i.uv);
        return float4(GTTone(c),1);
    }        
#endif //COLOR_GRADING_PASS_HLSL