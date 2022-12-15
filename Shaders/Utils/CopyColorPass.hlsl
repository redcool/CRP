#if !defined(COPY_COLOR_PASS_HLSL)
#define COPY_COLOR_PASS_HLSL
    #include "../Libs/UnityInput.hlsl"
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

    sampler2D _MainTex;

    CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    CBUFFER_END

    bool _ApplyColorGrading;

    float3 ApplyColorGrading(float3 c){
        if(_ApplyColorGrading)
            c = ColorGrading(c);
        return c;
    }

    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        #if defined(UNITY_UV_STARTS_AT_TOP)
        o.uv.y = 1-o.uv.y;
        #endif
        return o;
    }

    float4 frag (v2f i) : SV_Target{
        float3 c = tex2D(_MainTex, i.uv);
        c = ApplyColorGrading(c);
        return float4(c,1);
    }
    float4 fragReinhard(v2f i):SV_Target{
        float3 c = tex2D(_MainTex, i.uv);
        c = ApplyColorGrading(c);
        return float4(Reinhard(c),1);
    }
    float4 fragACESFitted(v2f i):SV_Target{
        float3 c = tex2D(_MainTex, i.uv);
        c = ApplyColorGrading(c);
        return float4(ACESFitted(c),1);
    }
    float4 fragACESFilm(v2f i):SV_Target{
        float3 c = tex2D(_MainTex, i.uv);
        c = ApplyColorGrading(c);
        return float4(ACESFilm(c),1);
    }
    float4 fragGTTone(v2f i):SV_Target{
        float3 c = tex2D(_MainTex, i.uv);
        c = ColorGrading(c);
        return float4(GTTone(c),1);
    }        
#endif //COPY_COLOR_PASS_HLSL