#ifndef CLUSTER_LIGHTING_1_INCLUDED
#define CLUSTER_LIGHTING_1_INCLUDED

#include "ClusterLookup.hlsl"

UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

float LightFalloff(float4 lightPosRadius, float3 worldPos)
{
    float3 lightCoord = worldPos - lightPosRadius.xyz;
    float distSq = dot(lightCoord, lightCoord);
    return saturate(1.0f - pow(sqrt(distSq * lightPosRadius.w), 2.0f)) / (distSq + 1.0f);
}

float LinearAttenuation(float4 lightPosRadius, float3 worldPos)
{
    return saturate(1.0 - distance(worldPos, lightPosRadius.xyz) / lightPosRadius.w);
}

float SimpleQuadraticAttenuation(float4 lightPosRadius, float3 worldPos)
{
    float n = saturate(distance(worldPos, lightPosRadius.xyz) / lightPosRadius.w);
    float a = 1.0 - n * n;
    return a * a;
}

float InverseSquareAttenuation(float4 lightPosRadius, float3 worldPos)
{
    float d = distance(worldPos, lightPosRadius.xyz) / lightPosRadius.w;
    float denom = d + 1.0;
    float smooth = saturate(1.0 - pow(d, 4.0));
    return (1.0 / (denom * denom)) * smooth * smooth;
}

float2 UnStereo(float2 UV)
{
    #if UNITY_SINGLE_PASS_STEREO
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
    #endif
    return UV;
}

float3 InvertDepthDir72_g6(float3 In)
{
    float3 result = In;
    #if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301
    result *= float3(1, 1, -1);
    #endif
    return result;
}

float3 WorldFromDepthTexture(float4 ScreenPosNorm)
{
    float2 uv = UnStereo(ScreenPosNorm.xy);
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, ScreenPosNorm.xy);
    #ifdef UNITY_REVERSED_Z
    depth = 1.0 - depth;
    #endif
    float4 ndc = float4(float3(uv, depth) * 2.0 - 1.0, 1.0);
    float4 viewPos = mul(unity_CameraInvProjection, ndc);
    viewPos.xyz /= viewPos.w;
    float3 inverted = InvertDepthDir72_g6(viewPos.xyz);
    return mul(unity_CameraToWorld, float4(inverted, 1.0));
}

float3 WorldFromDepthTexture(float2 uv)
{
    float2 stereoUV = UnStereo(uv);
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
    #ifdef UNITY_REVERSED_Z
    depth = 1.0 - depth;
    #endif
    float4 ndc = float4(float3(stereoUV, depth) * 2.0 - 1.0, 1.0);
    float4 viewPos = mul(unity_CameraInvProjection, ndc);
    viewPos.xyz /= viewPos.w;
    float3 inverted = InvertDepthDir72_g6(viewPos.xyz);
    return mul(unity_CameraToWorld, float4(inverted, 1.0));
}

//-------------------------------------------------------------------------------------
// Custom PBS BRDF based on Unity Standard
// GGX NDF + Smith Visibility + Disney Diffuse
//-------------------------------------------------------------------------------------

half4 BRDF_Unity_PBS_Custom(half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
                            float3 normal, float3 viewDir, UnityLight light, float lightMode)
{
    float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
    float3 halfDir = Unity_SafeNormalize(float3(light.dir) + viewDir);

    #define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0
    #if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
    half shiftAmount = dot(normal, viewDir);
    normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
    half nv = saturate(dot(normal, viewDir));
    #else
    half nv = abs(dot(normal, viewDir));
    #endif

    half nl = saturate(dot(normal, light.dir));
    float nh = saturate(dot(normal, halfDir));
    half lh = saturate(dot(light.dir, halfDir));

    half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;

    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    #if UNITY_BRDF_GGX
    half V = SmithJointGGXVisibilityTerm(nl, nv, roughness);
    float D = GGXTerm(nh, roughness);
    #else
    half V = SmithBeckmannVisibilityTerm(nl, nv, roughness);
    half D = NDFBlinnPhongNormalizedTerm(nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
    #endif

    half specularTerm = V * D * UNITY_PI;
    #ifdef UNITY_COLORSPACE_GAMMA
    specularTerm = sqrt(max(1e-4h, specularTerm));
    #endif
    specularTerm = max(0, specularTerm * nl);
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
    #endif

    specularTerm *= any(specColor) ? 1.0 : 0.0;

    half3 color = 0;
    if (lightMode == 0)
        color = diffColor * (light.color * diffuseTerm);
    else if (lightMode == 1)
        color = specularTerm * light.color * FresnelTerm(specColor, lh);
    else if (lightMode == 2)
        color = diffColor * (light.color * diffuseTerm) + specularTerm * light.color * FresnelTerm(specColor, lh);

    return half4(color, 1);
}

inline half4 PointLightingStandard(SurfaceOutputStandard s, float3 viewDir, UnityGI gi, float lightMode)
{
    s.Normal = normalize(s.Normal);
    half oneMinusReflectivity;
    half3 specColor;
    s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, specColor, oneMinusReflectivity);
    half outputAlpha;
    s.Albedo = PreMultiplyAlpha(s.Albedo, s.Alpha, oneMinusReflectivity, outputAlpha);
    half4 c = BRDF_Unity_PBS_Custom(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, lightMode);
    c.a = outputAlpha;
    return c;
}

// PBS cluster lighting with configurable attenuation
float3 ApplyClusterLighting1(
    UnityStandardData data, float3 positionWS, float3 viewDir,
    float fade, float attenMode, float lightMode)
{
    viewDir = normalize(viewDir);
    ClusterCell cell = GetClusterCell(positionWS);
    float3 totalLighting = float3(0,0,0);

    for (int i = 0; i < cell.lightCount; i++)
    {
        GPULightData light = GetCellLight(cell, i);
        float4 lightPos = light.positionAndRange;
        float3 lightColor = light.colorAndIntensity.xyz;
        float lightIntensity = light.colorAndIntensity.w;
        float lightRange = light.positionAndRange.w;

        half3 lightDir = normalize(lightPos.xyz - positionWS);

        float lightAtten = 1;
        if (attenMode == 1) lightAtten = LinearAttenuation(lightPos, positionWS);
        else if (attenMode == 2) { lightPos.w = 0; lightAtten = LightFalloff(lightPos, positionWS); }

        float dist = distance(positionWS, light.positionAndRange.xyz);
        float lightFade = 1 - smoothstep(fade, 1, Remap(dist, 0, lightRange, 0, 1));
        lightAtten *= lightFade;

        SurfaceOutputStandard o = (SurfaceOutputStandard)0;
        o.Albedo = data.diffuseColor;
        o.Normal = data.normalWorld;
        o.Metallic = data.specularColor;
        o.Occlusion = data.occlusion;
        o.Smoothness = data.smoothness;
        o.Alpha = 1;

        UnityGI gi;
        UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
        gi.indirect.diffuse = 0;
        gi.indirect.specular = 0;
        gi.light.color = lightColor * lightIntensity * lightAtten;
        gi.light.dir = lightDir;

        totalLighting += PointLightingStandard(o, viewDir, gi, lightMode);
    }
    return totalLighting;
}

#endif
