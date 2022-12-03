#if !defined(CRP_LIGHT_HLSL)
#define CRP_LIGHT_HLSL

#define MAX_LIGHT_COUNT 8
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLights)
int _DirectionalLightCount;
half4 _DirectionalLightColors[MAX_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_LIGHT_COUNT]; // {x:shadow strength,y: light tileId,z: NormalBiasFactor,w: shadowMask Channel}
float4 _DirectionalLightShadowMaskChannel;

int _OtherLightCount;
half4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

struct Light{
    half3 color;
    float3 direction;
    float attenuation;
};

int GetLightCount(){
    return _DirectionalLightCount;
}
int GetOtherLightCount(){
    return _OtherLightCount;
}

DirectionalShadowData GetDirLightShadowData(int lightId,ShadowData shadowData){
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightId].x;
    data.tileIndex = _DirectionalLightShadowData[lightId].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightId].z;
    data.occlusionMaskChannel = _DirectionalLightShadowData[lightId].w;
    return data;
}

Light GetLight(int id,Surface surface,ShadowData shadowData){
    Light l = (Light)0;
    l.color = _DirectionalLightColors[id].xyz;
    l.direction = _DirectionalLightDirections[id].xyz;

    DirectionalShadowData dirShadowData = GetDirLightShadowData(id,shadowData);
    l.attenuation = GetDirShadowAttenuation(dirShadowData,shadowData,surface);
    // l.attenuation = shadowData.cascadeIndex * 0.25;
    return l;
}

OtherShadowData GetOtherShadowData(int id){
    OtherShadowData data = (OtherShadowData)0;
    data.strength = _OtherLightShadowData[id].x;
    data.occlusionMaskChannel = _OtherLightShadowData[id].w;
    return data;
}

Light GetOtherLight(int id,Surface surface,ShadowData shadowData){
    Light l = (Light)0;
    l.color = _OtherLightColors[id];
    float3 dir = _OtherLightPositions[id].xyz - surface.worldPos;
    l.direction = normalize(dir);

    float dist2 = dot(dir,dir) + 0.00001;
    //range atten
    float range = _OtherLightPositions[id].w;
    float rangeAtten = Pow2(saturate(1 - Pow2(dist2*range)));
    // spot angle atten
    float4 spotAngles = _OtherLightSpotAngles[id];

    // pow(da + b,2)
    float spotAngleAtten = saturate(dot(_OtherLightDirections[id].xyz,l.direction) * spotAngles.x + spotAngles.y);
    spotAngleAtten *= spotAngleAtten;
// spotAngleAtten = saturate(dot(_OtherLightDirections[id].xyz,l.direction)-0.5);

// shadowMask atten
    OtherShadowData otherShadowData  = GetOtherShadowData(id);
    float shadowAtten = GetOtherShadowAttenuation(otherShadowData,shadowData,surface);

    l.attenuation = shadowAtten * spotAngleAtten * rangeAtten/dist2;
    // l.attenuation = smoothstep(1/range,0,dist2);
    return l;
}

#endif //CRP_LIGHT_HLSL