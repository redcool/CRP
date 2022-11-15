#if !defined(CRP_LIGHT_HLSL)
#define CRP_LIGHT_HLSL

#define MAX_LIGHT_COUNT 8

CBUFFER_START(_CustomLights)
int _DirectionalLightCount;
half4 _DirectionalLightColors[MAX_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_LIGHT_COUNT];
CBUFFER_END

struct Light{
    half3 color;
    float3 direction;
    float attenuation;
};

int GetLightCount(){
    return _DirectionalLightCount;
}

DirectionalShadowData GetDirLightShadowData(int lightId,ShadowData shadowData){
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightId].x * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[lightId].y + shadowData.cascadeIndex;
    return data;
}

Light GetLight(int id,Surface surface,ShadowData shadowData){
    Light l = (Light)0;
    l.color = _DirectionalLightColors[id].xyz;
    l.direction = _DirectionalLightDirections[id].xyz;
    DirectionalShadowData dirShadowData = GetDirLightShadowData(id,shadowData);
    l.attenuation = GetDirShadowAttenuation(dirShadowData,surface);
    // l.attenuation = shadowData.cascadeIndex * 0.25;
    return l;
}

#endif //CRP_LIGHT_HLSL