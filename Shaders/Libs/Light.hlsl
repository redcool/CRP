#if !defined(CRP_LIGHT_HLSL)
#define CRP_LIGHT_HLSL

#define MAX_LIGHT_COUNT 8
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLights)
int _DirectionalLightCount;
half4 _DirectionalLightColors[MAX_LIGHT_COUNT];
float4 _DirectionalLightDirectionsAndMask[MAX_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_LIGHT_COUNT]; // {x:shadow strength,y: light tileId,z: NormalBiasFactor,w: shadowMask Channel}
float4 _DirectionalLightShadowMaskChannel;

int _OtherLightCount;
half4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightDirectionsAndMask[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
float4 _OtherShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

struct Light{
    half3 color;
    float3 direction;
    float attenuation;
    uint renderingLayerMask;
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
    l.direction = _DirectionalLightDirectionsAndMask[id].xyz;
    l.renderingLayerMask = asuint(_DirectionalLightDirectionsAndMask[id].w);

    DirectionalShadowData dirShadowData = GetDirLightShadowData(id,shadowData);
    l.attenuation = GetDirShadowAttenuation(dirShadowData,shadowData,surface);
    // l.attenuation = shadowData.cascadeIndex * 0.25;
    return l;
}

OtherShadowData GetOtherShadowData(int id){
    OtherShadowData data = (OtherShadowData)0;
    data.strength = _OtherShadowData[id].x;
    data.occlusionMaskChannel = _OtherShadowData[id].w;
    data.tileIndex = _OtherShadowData[id].y;
    data.isPoint = _OtherShadowData[id].z > 0;
    return data;
}

Light GetOtherLight(int id,Surface surface,ShadowData shadowData){
    Light l = (Light)0;
    l.color = _OtherLightColors[id].xyz;

    float3 lightPos = _OtherLightPositions[id].xyz;
    float3 dir = lightPos - surface.worldPos;
    l.direction = normalize(dir);

    float dist2 = dot(dir,dir) + 0.00001;
    //range atten
    float range = _OtherLightPositions[id].w;
    float rangeAtten = Pow2(saturate(1 - Pow2(dist2*range)));
    // spot angle atten
    float4 spotAngles = _OtherLightSpotAngles[id];

    l.renderingLayerMask = asuint(_OtherLightDirectionsAndMask[id].w);
    // pow(da + b,2)
    float3 spotDirection = _OtherLightDirectionsAndMask[id].xyz;
    float spotAngleAtten = saturate(dot(spotDirection,l.direction) * spotAngles.x + spotAngles.y);
    spotAngleAtten *= spotAngleAtten;
// spotAngleAtten = saturate(dot(_OtherLightDirectionsAndMask[id].xyz,l.direction)-0.5);

// shadowMask atten
    OtherShadowData otherShadowData = GetOtherShadowData(id);
    otherShadowData.lightPos = lightPos;
    otherShadowData.lightDir = l.direction;
    otherShadowData.spotDir = spotDirection;
    float shadowAtten = GetOtherShadowAttenuation(otherShadowData,shadowData,surface);

    l.attenuation = shadowAtten * spotAngleAtten * rangeAtten/dist2;
    // l.attenuation = smoothstep(1/range,0,dist2);
    return l;
}

#endif //CRP_LIGHT_HLSL