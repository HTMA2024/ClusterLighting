using UnityEngine;
using System.Collections.Generic;
using ClusterLighting.Core;

namespace ClusterLighting.Builder
{
    public class LightClusterAssigner
    {
        public static void AssignLightsToGrid(
            List<BakedLightData> lights, ClusterGridConfig config,
            ref ClusterCell[] cells, out List<int> lightIndices)
        {
            lightIndices = new List<int>();
            var cellLightMap = new Dictionary<int, List<int>>();

            for (int lightIdx = 0; lightIdx < lights.Count; lightIdx++)
            {
                var influenced = GetInfluencedCells(lights[lightIdx], config);
                foreach (var cellCoord in influenced)
                {
                    int idx = ClusterMath.GridCellToLinearIndex(cellCoord, config.gridDimensions);
                    if (!cellLightMap.ContainsKey(idx))
                        cellLightMap[idx] = new List<int>();
                    cellLightMap[idx].Add(lightIdx);
                }
            }

            foreach (var kvp in cellLightMap)
            {
                cells[kvp.Key].lightStartIndex = lightIndices.Count;
                cells[kvp.Key].lightCount = kvp.Value.Count;
                lightIndices.AddRange(kvp.Value);
            }
        }

        // 粗略AABB裁剪 + 精确Sphere-AABB相交
        public static List<Vector3Int> GetInfluencedCells(BakedLightData light, ClusterGridConfig config)
        {
            var result = new List<Vector3Int>();
            var (minCell, maxCell) = ClusterMath.GetLightInfluenceCellRange(light.position, light.range, config);

            for (int z = minCell.z; z <= maxCell.z; z++)
            for (int y = minCell.y; y <= maxCell.y; y++)
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                var coord = new Vector3Int(x, y, z);
                if (ClusterMath.SphereIntersectsAABB(light.position, light.range,
                        ClusterMath.GetCellBounds(coord, config)))
                    result.Add(coord);
            }
            return result;
        }

        public static void BatchAssignLights(
            BakedLightData[] lights, ClusterGridConfig config,
            ref ClusterCell[] cells, out List<int> lightIndices)
        {
            AssignLightsToGrid(new List<BakedLightData>(lights), config, ref cells, out lightIndices);
        }

        public static AssignmentStatistics GetStatistics(ClusterCell[] cells, List<int> lightIndices)
        {
            var stats = new AssignmentStatistics
            {
                totalCells = cells.Length,
                totalLightReferences = lightIndices.Count
            };
            int maxLights = 0;
            foreach (var cell in cells)
            {
                if (!cell.IsEmpty())
                {
                    stats.nonEmptyCells++;
                    maxLights = Mathf.Max(maxLights, cell.lightCount);
                }
            }
            stats.maxLightsPerCell = maxLights;
            stats.averageLightsPerCell = stats.totalCells > 0
                ? (float)stats.totalLightReferences / stats.totalCells : 0f;
            return stats;
        }
    }

    [System.Serializable]
    public struct AssignmentStatistics
    {
        public int totalCells;
        public int nonEmptyCells;
        public int totalLightReferences;
        public int maxLightsPerCell;
        public float averageLightsPerCell;

        public override string ToString()
        {
            return $"Cells: {totalCells}, Non-Empty: {nonEmptyCells}, " +
                   $"Refs: {totalLightReferences}, Max/Cell: {maxLightsPerCell}, " +
                   $"Avg/Cell: {averageLightsPerCell:F2}";
        }
    }
}
