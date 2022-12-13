#if !defined(COPY_COLOR_PASS_HLSL)
#define COPY_COLOR_PASS_HLSL
    #include "../Libs/UnityInput.hlsl"
    #include "../../../PowerShaderLib/Lib/ToneMappers.hlsl"

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

    half4 frag (v2f i) : SV_Target{
        return tex2D(_MainTex, i.uv);
    }
    half4 fragReinhard(v2f i):SV_Target{
        return half4(Reinhard(tex2D(_MainTex,i.uv).xyz),1);
    }
    half4 fragACESFitted(v2f i):SV_Target{
        return half4(ACESFitted(tex2D(_MainTex,i.uv).xyz),1);
    }
    half4 fragACESFilm(v2f i):SV_Target{
        return half4(ACESFilm(tex2D(_MainTex,i.uv).xyz),1);
    }
    half4 fragGTTone(v2f i):SV_Target{
        return half4(GTTone(tex2D(_MainTex,i.uv).xyz),1);
    }        
#endif //COPY_COLOR_PASS_HLSL