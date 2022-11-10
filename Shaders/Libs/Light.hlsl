#if !defined(CRP_LIGHT_HLSL)
#define CRP_LIGHT_HLSL

#define MAX_LIGHT_COUNT 8

CBUFFER_START(_CustomLights)
int _DirectionalLightCount;
half4 _DirectionalLightColors[MAX_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_LIGHT_COUNT];
CBUFFER_END

struct Light{
    half3 color;
    float3 direction;
    half attenuation;
};

int GetLightCount(){
    return _DirectionalLightCount;
}

Light GetLight(int id){
    Light l = (Light)0;
    l.color = _DirectionalLightColors[id];
    l.direction = _DirectionalLightDirections[id];
    l.attenuation = 1;
    return l;
}

#endif //CRP_LIGHT_HLSL