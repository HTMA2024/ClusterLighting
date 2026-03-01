using UnityEngine;
using ClusterLighting.Core;

namespace ClusterLighting.Runtime
{
    public static class ClusterGPUUploader
    {
        private const string SHADER_CELLS_BUFFER = "_ClusterCells";
        private const string SHADER_INLINED_LIGHTS_POS = "_ClusterInlinedLightsPosRange";
        private const string SHADER_INLINED_LIGHTS_COLOR = "_ClusterInlinedLightsColorInt";
        private const string SHADER_GRID_BOUNDS_MIN = "_ClusterGridBoundsMin";
        private const string SHADER_GRID_BOUNDS_MAX = "_ClusterGridBoundsMax";
        private const string SHADER_GRID_DIMENSIONS = "_ClusterGridDimensions";
        private const string SHADER_GRID_CELL_SIZE = "_ClusterGridCellSize";

        public static void UploadClusterData(
            ClusterBakeAsset asset,
            out ComputeBuffer cellBuffer,
            out ComputeBuffer inlinedLightsPosBuffer,
            out ComputeBuffer inlinedLightsColorBuffer)
        {
            if (asset == null || !asset.IsValid())
            {
                Debug.LogError("Cannot upload invalid ClusterBakeAsset to GPU.");
                cellBuffer = null;
                inlinedLightsPosBuffer = null;
                inlinedLightsColorBuffer = null;
                return;
            }

            cellBuffer = new ComputeBuffer(asset.cells.Length, sizeof(int) * 2);
            cellBuffer.SetData(asset.cells);

            int inlinedCount = asset.inlinedPositionAndRange.Length;
            if (inlinedCount > 0)
            {
                inlinedLightsPosBuffer = new ComputeBuffer(inlinedCount, 16);
                inlinedLightsPosBuffer.SetData(asset.inlinedPositionAndRange);
                inlinedLightsColorBuffer = new ComputeBuffer(inlinedCount, 16);
                inlinedLightsColorBuffer.SetData(asset.inlinedColorAndIntensity);
            }
            else
            {
                // ComputeBuffer不允许size=0，放一个dummy
                inlinedLightsPosBuffer = new ComputeBuffer(1, 16);
                inlinedLightsPosBuffer.SetData(new Vector4[] { Vector4.zero });
                inlinedLightsColorBuffer = new ComputeBuffer(1, 16);
                inlinedLightsColorBuffer.SetData(new Vector4[] { Vector4.zero });
            }
        }

        public static void SetShaderGlobalBuffers(
            ComputeBuffer cellBuffer,
            ComputeBuffer inlinedLightsPosBuffer,
            ComputeBuffer inlinedLightsColorBuffer)
        {
            if (cellBuffer != null) Shader.SetGlobalBuffer(SHADER_CELLS_BUFFER, cellBuffer);
            if (inlinedLightsPosBuffer != null) Shader.SetGlobalBuffer(SHADER_INLINED_LIGHTS_POS, inlinedLightsPosBuffer);
            if (inlinedLightsColorBuffer != null) Shader.SetGlobalBuffer(SHADER_INLINED_LIGHTS_COLOR, inlinedLightsColorBuffer);
        }

        public static void SetShaderGlobalParams(ClusterGridConfig config)
        {
            Shader.SetGlobalVector(SHADER_GRID_BOUNDS_MIN, config.boundsMin);
            Shader.SetGlobalVector(SHADER_GRID_BOUNDS_MAX, config.boundsMax);
            Shader.SetGlobalVector(SHADER_GRID_DIMENSIONS, new Vector4(
                config.gridDimensions.x, config.gridDimensions.y, config.gridDimensions.z, 0));
            Shader.SetGlobalVector(SHADER_GRID_CELL_SIZE, config.cellSize);
        }

        public static void ReleaseBuffers(params ComputeBuffer[] buffers)
        {
            foreach (var buffer in buffers)
            {
                if (buffer != null) { buffer.Release(); buffer.Dispose(); }
            }
        }

        public static void ClearShaderGlobals()
        {
            Shader.SetGlobalVector(SHADER_GRID_BOUNDS_MIN, Vector4.zero);
            Shader.SetGlobalVector(SHADER_GRID_BOUNDS_MAX, Vector4.zero);
            Shader.SetGlobalVector(SHADER_GRID_DIMENSIONS, Vector4.zero);
            Shader.SetGlobalVector(SHADER_GRID_CELL_SIZE, Vector4.zero);
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct GPULightData
    {
        public Vector4 positionAndRange;  // xyz: pos, w: range
        public Vector4 colorAndIntensity; // rgb: color, a: intensity

        public GPULightData(BakedLightData light)
        {
            positionAndRange = new Vector4(light.position.x, light.position.y, light.position.z, light.range);
            colorAndIntensity = new Vector4(light.color.r, light.color.g, light.color.b, light.intensity);
        }
    }
}
