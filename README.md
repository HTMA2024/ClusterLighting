[中文版本 (Chinese Version)](README_CN.md)

# Clustered Forward Lighting

A Clustered Forward Lighting implementation for Unity Built-in Render Pipeline. The system partitions 3D scene space into a uniform grid, pre-computes which lights affect each cell, and at runtime only evaluates the relevant light contributions — enabling efficient rendering of large numbers of Point Lights.

## Showcase

![ClusterLighting](assets/github-repo-images/ClusterLighting/ClusterLighting.jpg)

![Heatmap](assets/github-repo-images/ClusterLighting/Heatmap.jpg)

## System Architecture

### Bake Pipeline (Editor)

Collects scene Point Lights in the editor, generates a 3D grid configuration, assigns lights to cells via a two-stage intersection test (coarse AABB culling + precise Sphere-AABB), and encodes the result into a `ClusterBakeAsset` (ScriptableObject).

### Inlined Data Layout (GPU Optimization)

Traditional Clustered Lighting uses three-level indirect addressing: Cell → LightIndices → LightData, requiring two non-contiguous buffer jumps per light access. This project flattens light data linearly by cell order into two parallel StructuredBuffers — lights belonging to the same cell are physically contiguous in VRAM, so adjacent pixels within the same warp are likely to hit the same cache line. The trade-off is duplicate storage when a light affects multiple cells — trading space for bandwidth.

```
_ClusterCells:                  [Cell0, Cell1, Cell2, ...]
_ClusterInlinedLightsPosRange:  [C0.L0, C0.L1, | C1.L0, | C2.L0, C2.L1, C2.L2, | ...]
_ClusterInlinedLightsColorInt:  [C0.L0, C0.L1, | C1.L0, | C2.L0, C2.L1, C2.L2, | ...]
```

### Runtime

`ClusterLightingManager` (marked `[ExecuteInEditMode]`) uploads baked data to GPU ComputeBuffers and sets Shader global variables on `OnEnable`, allowing lighting preview in editor non-play mode.

### Shaders

Two lighting models provided:

- **ClusterLighting.hlsl** — Simplified Blinn-Phong (Lambert diffuse + Blinn specular + smooth distance attenuation)
- **ClusterLighting1.hlsl** — Unity Standard PBS (GGX NDF + Smith Visibility + Disney Diffuse), supporting Linear / Exponential / Inverse Square attenuation modes, reading material properties from G-Buffer

## Rendering Pipeline

```
Scene Point Lights
  → ClusterLightCollector (collect & filter)
  → ClusterGridGenerator (generate grid config)
  → LightClusterAssigner (two-stage intersection test, assign lights to cells)
  → ClusterDataEncoder (encode + inline light data flattening)
  → ClusterBakeAsset (serialize & save)
  → ClusterGPUUploader (upload ComputeBuffer + set global variables)
  → Shader (single-lookup inlined light data read, compute lighting)
```

## Directory Structure

```
Assets/
├── Runtime/
│   ├── Core/                        Core data structures
│   │   ├── ClusterStructs.cs        ClusterGridConfig, ClusterCell, BakedLightData
│   │   ├── ClusterBakeAsset.cs      ScriptableObject bake asset
│   │   └── ClusterMath.cs           Coordinate conversion, Sphere-AABB intersection
│   ├── Builder/                     Build pipeline
│   │   ├── ClusterGridGenerator.cs  Grid generation & validation
│   │   ├── LightClusterAssigner.cs  Light-Cell assignment
│   │   └── ClusterDataEncoder.cs    Data encoding, inline layout generation
│   ├── ClusterLightingManager.cs    Runtime manager (ExecuteInEditMode)
│   └── ClusterGPUUploader.cs        ComputeBuffer creation & upload
├── Editor/
│   ├── Builder/
│   │   ├── ClusterLightCollector.cs Scene light collection
│   │   ├── ClusterBuilder.cs        Bake pipeline orchestration
│   │   └── ClusterBakerWindow.cs    Editor window UI
│   └── Debug/
│       ├── ClusterDebugRenderer.cs  Gizmos drawing
│       └── ClusterDebugVisualizer.cs Scene View visualization component
└── Shaders/
    ├── ClusterLookup.hlsl           GPU-side grid lookup
    ├── ClusterLighting.hlsl         Blinn-Phong lighting
    ├── ClusterLighting.shader       Basic Cluster Lighting Shader
    ├── ClusterLighting1.hlsl        GGX PBS lighting
    ├── ClusterLighting1.shader      Deferred G-Buffer sampling version
    └── ClusterLightingCube.shader   Cube volume proxy version (Cull Front)
```

## Requirements

- Unity 2021.3+
- Built-in Render Pipeline
- Platform with StructuredBuffer support

## Usage

1. Place Point Lights in the scene
2. Open Window → Cluster Lighting → Baker, configure grid parameters and click Bake, save the Asset
3. Add `ClusterLightingManager` component to the scene, drag in the Bake Asset
4. Assign materials using `Recreate/ClusterLighting1` or `Unlit/ClusterLighting` Shader

## Known Limitations

- Only supports Point Lights (Spot / Directional require different intersection algorithms)
- Light data is a static snapshot from bake time; runtime light add/remove/move is not supported
- Inlined layout memory usage grows significantly with high light density and large cell counts

## License

MIT
