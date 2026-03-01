#ifndef CLUSTER_LOOKUP_INCLUDED
#define CLUSTER_LOOKUP_INCLUDED

// Global params (set by ClusterGPUUploader)
float3 _ClusterGridBoundsMin;
float3 _ClusterGridBoundsMax;
float4 _ClusterGridDimensions; // xyz: dims
float3 _ClusterGridCellSize;

struct ClusterCell
{
    int lightStartIndex;
    int lightCount;
};

struct GPULightData
{
    float4 positionAndRange;  // xyz: pos, w: range
    float4 colorAndIntensity; // rgb: color, a: intensity
};

StructuredBuffer<ClusterCell> _ClusterCells;
StructuredBuffer<float4> _ClusterInlinedLightsPosRange;
StructuredBuffer<float4> _ClusterInlinedLightsColorInt;

int3 WorldToGridCell(float3 worldPos)
{
    float3 norm = (worldPos - _ClusterGridBoundsMin) / (_ClusterGridBoundsMax - _ClusterGridBoundsMin);
    int3 coord = (int3)(norm * _ClusterGridDimensions.xyz);
    return clamp(coord, int3(0,0,0), (int3)_ClusterGridDimensions.xyz - int3(1,1,1));
}

int GridCellToLinearIndex(int3 coord)
{
    int3 d = (int3)_ClusterGridDimensions.xyz;
    return coord.z * (d.x * d.y) + coord.y * d.x + coord.x;
}

int WorldToLinearIndex(float3 worldPos)
{
    return GridCellToLinearIndex(WorldToGridCell(worldPos));
}

ClusterCell GetClusterCell(float3 worldPos)
{
    return _ClusterCells[WorldToLinearIndex(worldPos)];
}

GPULightData GetInlinedLight(int idx)
{
    GPULightData l;
    l.positionAndRange  = _ClusterInlinedLightsPosRange[idx];
    l.colorAndIntensity = _ClusterInlinedLightsColorInt[idx];
    return l;
}

GPULightData GetCellLight(ClusterCell cell, int localIndex)
{
    return GetInlinedLight(cell.lightStartIndex + localIndex);
}

bool IsInsideClusterGrid(float3 worldPos)
{
    return all(worldPos >= _ClusterGridBoundsMin) && all(worldPos <= _ClusterGridBoundsMax);
}

float3 GetGridBoundsMin() { return _ClusterGridBoundsMin; }
float3 GetGridBoundsMax() { return _ClusterGridBoundsMax; }
int3   GetGridDimensions() { return (int3)_ClusterGridDimensions.xyz; }
float3 GetGridCellSize()  { return _ClusterGridCellSize; }

#endif
