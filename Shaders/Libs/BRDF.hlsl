#if !defined(CRP_BRDF_HLSL)
#define CRP_BRDF_HLSL

struct BRDF{
    float3 diffuse;
    float3 specular;
    float a,a2;
};


BRDF GetBRDF(Surface s){
    BRDF b = (BRDF)0;
    b.diffuse = s.albedo * (0.96 - s.metallic * 0.96);
    b.specular = lerp(0.04,s.albedo,s.metallic);
    float r = 1 - s.smoothness;
    b.a = r * r;
    b.a2 = b.a*b.a;

    return b;
}

float CalcSpecTerm(Light light,Surface s,BRDF b){
    float3 l = light.direction;
    float3 h = normalize(l + s.viewDir);
    float3 n = s.normal;

    float nh = saturate(dot(n,h));
    float lh = saturate(dot(l,h));
    float nl = saturate(dot(n,l));

    float d = nh*nh * (b.a2-1)+1;
    float specTerm = b.a2/(d*d*max(0.00001,lh*lh) * (4*b.a+2));
    return specTerm;
}

#endif //CRP_BRDF_HLSL