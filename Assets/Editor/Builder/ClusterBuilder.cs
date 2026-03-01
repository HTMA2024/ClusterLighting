using UnityEngine;
using System.Collections.Generic;
using ClusterLighting.Core;

namespace ClusterLighting.Builder
{
    [System.Serializable]
    public class ClusterBakeSettings
    {
        [Header("Grid Configuration")]
        public Vector3Int gridDimensions = new Vector3Int(16, 16, 16);
        public bool autoCalculateBounds = true;
        public Bounds manualBounds = new Bounds(Vector3.zero, Vector3.one * 100f);

        [Header("Light Collection")]
        public LayerMask lightLayers = ~0;
        public bool includeInactiveLights = false;

        [Header("Optimization")]
        public bool compressEmptyCells = false;
        public bool optimizeLightOrder = true;

        [Header("Advanced")]
        public float targetCellSize = 5f;
        public bool autoCalculateDimensions = false;
    }

    public class ClusterBuilder
    {
        public ClusterBakeSettings settings;

        public ClusterBuilder() { settings = new ClusterBakeSettings(); }

        public ClusterBakeAsset BakeScene()
        {
            if (!ValidateScene(out List<string> errors))
            {
                Debug.LogError($"Scene validation failed:\n{string.Join("\n", errors)}");
                return null;
            }

            Debug.Log("Starting Cluster Lighting bake...");

            var lights = ClusterLightCollector.CollectLights(settings.lightLayers, settings.includeInactiveLights);
            if (lights.Count == 0)
                Debug.LogWarning("No valid lights found.");

            Bounds sceneBounds = CalculateSceneBounds(lights);

            Vector3Int dims = settings.autoCalculateDimensions
                ? ClusterGridGenerator.CalculateOptimalDimensions(sceneBounds, settings.targetCellSize)
                : settings.gridDimensions;

            ClusterGridConfig config = ClusterGridGenerator.GenerateGrid(sceneBounds, dims);
            if (!ClusterGridGenerator.ValidateGridConfig(config, out string configError))
            {
                Debug.LogError($"Grid config invalid: {configError}");
                return null;
            }

            ClusterCell[] cells = ClusterGridGenerator.InitializeCells(config);
            LightClusterAssigner.AssignLightsToGrid(lights, config, ref cells, out List<int> lightIndices);

            ClusterDataEncoder.EncodeClusterData(cells, lightIndices, lights,
                out ClusterCell[] encodedCells, out int[] encodedIndices, out BakedLightData[] encodedLights);

            if (settings.optimizeLightOrder)
                ClusterDataEncoder.OptimizeLightIndices(ref encodedCells, ref encodedIndices, encodedLights, config);

            var stats = ClusterDataEncoder.GenerateStatistics(encodedCells, encodedIndices);
            stats.memoryUsageBytes = ClusterDataEncoder.CalculateMemoryUsage(encodedCells, encodedIndices, encodedLights);

            ClusterDataEncoder.BuildInlinedLightData(encodedCells, encodedIndices, encodedLights,
                out ClusterCell[] inlinedCells, out Vector4[] inlinedPosRange, out Vector4[] inlinedColorInt);

            var asset = ScriptableObject.CreateInstance<ClusterBakeAsset>();
            asset.gridConfig = config;
            asset.cells = inlinedCells;
            asset.lightIndices = encodedIndices;
            asset.lights = encodedLights;
            asset.inlinedPositionAndRange = inlinedPosRange;
            asset.inlinedColorAndIntensity = inlinedColorInt;
            asset.totalLightCount = encodedLights.Length;
            asset.totalCellCount = inlinedCells.Length;
            asset.nonEmptyCellCount = stats.nonEmptyCells;
            asset.inlinedLightCount = inlinedPosRange.Length;
            asset.memoryUsageBytes = (long)inlinedCells.Length * 8 + (long)inlinedPosRange.Length * 32;

            Debug.Log($"Bake completed.\n{stats}");
            return asset;
        }

        public bool ValidateScene(out List<string> errors)
        {
            errors = new List<string>();
            if (settings.gridDimensions.x <= 0 || settings.gridDimensions.y <= 0 || settings.gridDimensions.z <= 0)
                errors.Add("Grid dimensions must be positive.");
            if (!settings.autoCalculateBounds &&
                (settings.manualBounds.size.x <= 0 || settings.manualBounds.size.y <= 0 || settings.manualBounds.size.z <= 0))
                errors.Add("Manual bounds size must be positive.");
            if (settings.autoCalculateDimensions && settings.targetCellSize <= 0)
                errors.Add("Target cell size must be positive.");
            return errors.Count == 0;
        }

        public BakeEstimate EstimateBake()
        {
            Light[] allLights = Object.FindObjectsOfType<Light>();
            int pointLightCount = 0;
            foreach (var light in allLights)
                if (light.type == LightType.Point && ((1 << light.gameObject.layer) & settings.lightLayers) != 0)
                    pointLightCount++;

            Vector3Int dims = settings.autoCalculateDimensions ? new Vector3Int(32, 32, 32) : settings.gridDimensions;
            int cellCount = dims.x * dims.y * dims.z;
            int avgRefs = cellCount * 2;

            return new BakeEstimate
            {
                lightCount = pointLightCount,
                cellCount = cellCount,
                estimatedMemoryBytes = cellCount * 8L + avgRefs * 32L,
                estimatedTimeSeconds = (pointLightCount * cellCount) / 1000000f
            };
        }

        private Bounds CalculateSceneBounds(List<BakedLightData> lights)
        {
            if (!settings.autoCalculateBounds) return settings.manualBounds;
            if (lights.Count == 0) return new Bounds(Vector3.zero, Vector3.one * 100f);

            Bounds bounds = lights[0].GetBounds();
            for (int i = 1; i < lights.Count; i++)
                bounds.Encapsulate(lights[i].GetBounds());
            bounds.Expand(10f);
            return bounds;
        }
    }

    [System.Serializable]
    public struct BakeEstimate
    {
        public int lightCount;
        public int cellCount;
        public long estimatedMemoryBytes;
        public float estimatedTimeSeconds;

        public override string ToString()
        {
            return $"Lights: {lightCount}, Cells: {cellCount}, " +
                   $"Memory: {estimatedMemoryBytes / 1024f:F2} KB, Time: {estimatedTimeSeconds:F2}s";
        }
    }
}
