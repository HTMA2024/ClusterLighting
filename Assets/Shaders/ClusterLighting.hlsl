#ifndef CLUSTER_LIGHTING_INCLUDED
#define CLUSTER_LIGHTING_INCLUDED

#include "ClusterLookup.hlsl"

// Blinn-Phong point light
float3 CalculatePointLight(
    GPULightData light, float3 worldPos, float3 normal, float3 viewDir,
    float3 albedo, float smoothness)
{
    float3 lightPos = light.positionAndRange.xyz;
    float lightRange = light.positionAndRange.w;
    float3 lightColor = light.colorAndIntensity.rgb;
    float lightIntensity = light.colorAndIntensity.a;

    float3 lightDir = lightPos - worldPos;
    float dist = length(lightDir);
    if (dist > lightRange) return float3(0,0,0);

    lightDir = normalize(lightDir);
    float atten = 1.0 - saturate(dist / lightRange);
    atten *= atten;

    float NdotL = max(0.0, dot(normal, lightDir));
    float3 halfDir = normalize(lightDir + viewDir);
    float spec = pow(max(0.0, dot(normal, halfDir)), smoothness * 128.0);

    return (albedo * lightColor * lightIntensity * NdotL
          + lightColor * lightIntensity * spec * smoothness) * atten;
}

float3 ApplyClusterLighting(
    float3 worldPos, float3 normal, float3 viewDir, float3 albedo, float smoothness)
{
    normal = normalize(normal);
    viewDir = normalize(viewDir);
    ClusterCell cell = GetClusterCell(worldPos);

    if (cell.lightCount == 0) return albedo * 0.1;

    float3 total = float3(0,0,0);
    for (int i = 0; i < cell.lightCount; i++)
        total += CalculatePointLight(GetCellLight(cell, i), worldPos, normal, viewDir, albedo, smoothness);

    return total + albedo * 0.1;
}

float3 ApplyClusterLightingSimple(float3 worldPos, float3 normal, float3 albedo)
{
    normal = normalize(normal);
    ClusterCell cell = GetClusterCell(worldPos);
    if (cell.lightCount == 0) return albedo * 0.1;

    float3 total = float3(0,0,0);
    for (int i = 0; i < cell.lightCount; i++)
    {
        GPULightData light = GetCellLight(cell, i);
        float3 toLight = light.positionAndRange.xyz - worldPos;
        float dist = length(toLight);
        if (dist > light.positionAndRange.w) continue;

        float atten = 1.0 - saturate(dist / light.positionAndRange.w);
        atten *= atten;
        float NdotL = max(0.0, dot(normal, normalize(toLight)));
        total += albedo * light.colorAndIntensity.rgb * light.colorAndIntensity.a * NdotL * atten;
    }
    return total + albedo * 0.1;
}

float3 VisualizeClusterLightCount(float3 worldPos)
{
    float t = saturate(GetClusterCell(worldPos).lightCount / 8.0);
    return float3(t, 1.0 - t, 0);
}

float3 VisualizeClusterGrid(float3 worldPos, float3 baseColor)
{
    int3 coord = WorldToGridCell(worldPos);
    float3 localPos = (worldPos - (_ClusterGridBoundsMin + coord * _ClusterGridCellSize)) / _ClusterGridCellSize;
    float3 edge = step(0.95, localPos) + step(localPos, 0.05);
    return lerp(baseColor, float3(1,1,0), saturate(edge.x + edge.y + edge.z) * 0.5);
}

#endif
