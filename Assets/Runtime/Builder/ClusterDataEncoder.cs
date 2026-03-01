using UnityEngine;
using System.Collections.Generic;
using ClusterLighting.Core;

namespace ClusterLighting.Builder
{
    public class ClusterDataEncoder
    {
        public static void EncodeClusterData(
            ClusterCell[] cells,
            List<int> lightIndices,
            List<BakedLightData> lights,
            out ClusterCell[] encodedCells,
            out int[] encodedIndices,
            out BakedLightData[] encodedLights)
        {
            encodedCells = new ClusterCell[cells.Length];
            System.Array.Copy(cells, encodedCells, cells.Length);
            encodedIndices = lightIndices.ToArray();
            encodedLights = lights.ToArray();
            ValidateEncodedData(encodedCells, encodedIndices, encodedLights);
        }

        public static ClusterCell[] CompressEmptyCells(ClusterCell[] cells)
        {
            List<ClusterCell> nonEmpty = new List<ClusterCell>();
            foreach (var cell in cells)
                if (!cell.IsEmpty()) nonEmpty.Add(cell);
            return nonEmpty.ToArray();
        }

        public static long CalculateMemoryUsage(ClusterCell[] cells, int[] lightIndices, BakedLightData[] lights)
        {
            return cells.Length * 8L + lightIndices.Length * 4L + lights.Length * 40L;
        }

        public static ClusterStatistics GenerateStatistics(ClusterCell[] cells, int[] lightIndices)
        {
            ClusterStatistics stats = new ClusterStatistics();
            stats.totalCells = cells.Length;
            stats.totalLightReferences = lightIndices.Length;

            int maxLights = 0;
            long totalLights = 0;
            foreach (var cell in cells)
            {
                if (!cell.IsEmpty())
                {
                    stats.nonEmptyCells++;
                    maxLights = Mathf.Max(maxLights, cell.lightCount);
                }
                totalLights += cell.lightCount;
            }
            stats.maxLightsPerCell = maxLights;
            stats.averageLightsPerCell = stats.totalCells > 0 ? (float)totalLights / stats.totalCells : 0f;
            return stats;
        }

        private static void ValidateEncodedData(ClusterCell[] cells, int[] lightIndices, BakedLightData[] lights)
        {
            foreach (var cell in cells)
            {
                if (cell.IsEmpty()) continue;
                if (cell.lightStartIndex < 0 || cell.lightStartIndex >= lightIndices.Length)
                    Debug.LogError($"Invalid lightStartIndex: {cell.lightStartIndex}");
                if (cell.lightStartIndex + cell.lightCount > lightIndices.Length)
                    Debug.LogError("Cell light count exceeds indices array bounds");
            }
            foreach (int idx in lightIndices)
                if (idx < 0 || idx >= lights.Length)
                    Debug.LogError($"Invalid light index: {idx}");
        }

        public static void OptimizeLightIndices(
            ref ClusterCell[] cells, ref int[] lightIndices,
            BakedLightData[] lights, ClusterGridConfig config)
        {
            List<int> optimized = new List<int>();

            for (int cellIdx = 0; cellIdx < cells.Length; cellIdx++)
            {
                ClusterCell cell = cells[cellIdx];
                if (cell.IsEmpty()) continue;

                Vector3Int coord = ClusterMath.LinearIndexToGridCell(cellIdx, config.gridDimensions);
                Vector3 center = ClusterMath.GetCellBounds(coord, config).center;

                var cellLights = new List<(int index, float dist)>();
                for (int i = 0; i < cell.lightCount; i++)
                {
                    int lightIdx = lightIndices[cell.lightStartIndex + i];
                    cellLights.Add((lightIdx, Vector3.Distance(center, lights[lightIdx].position)));
                }
                cellLights.Sort((a, b) => a.dist.CompareTo(b.dist));

                cells[cellIdx].lightStartIndex = optimized.Count;
                foreach (var (index, _) in cellLights)
                    optimized.Add(index);
            }
            lightIndices = optimized.ToArray();
        }

        /// <summary>
        /// 将每个Cell引用的光源数据按线性顺序平铺，消除GPU端间接寻址。
        /// </summary>
        public static void BuildInlinedLightData(
            ClusterCell[] cells, int[] lightIndices, BakedLightData[] lights,
            out ClusterCell[] inlinedCells, out Vector4[] inlinedPosRange, out Vector4[] inlinedColorInt)
        {
            int totalEntries = 0;
            foreach (var cell in cells) totalEntries += cell.lightCount;

            inlinedPosRange = new Vector4[totalEntries];
            inlinedColorInt = new Vector4[totalEntries];
            inlinedCells = new ClusterCell[cells.Length];
            int offset = 0;

            for (int i = 0; i < cells.Length; i++)
            {
                inlinedCells[i].lightCount = cells[i].lightCount;
                if (cells[i].IsEmpty()) { inlinedCells[i].lightStartIndex = 0; continue; }

                inlinedCells[i].lightStartIndex = offset;
                for (int j = 0; j < cells[i].lightCount; j++)
                {
                    var light = lights[lightIndices[cells[i].lightStartIndex + j]];
                    inlinedPosRange[offset] = new Vector4(light.position.x, light.position.y, light.position.z, light.range);
                    inlinedColorInt[offset] = new Vector4(light.color.r, light.color.g, light.color.b, light.intensity);
                    offset++;
                }
            }
        }
    }

    [System.Serializable]
    public struct ClusterStatistics
    {
        public int totalCells;
        public int nonEmptyCells;
        public int totalLightReferences;
        public int maxLightsPerCell;
        public float averageLightsPerCell;
        public long memoryUsageBytes;

        public override string ToString()
        {
            return $"Total Cells: {totalCells}\n" +
                   $"Non-Empty: {nonEmptyCells} ({(float)nonEmptyCells / totalCells * 100:F1}%)\n" +
                   $"Light Refs: {totalLightReferences}\n" +
                   $"Max/Cell: {maxLightsPerCell}\n" +
                   $"Avg/Cell: {averageLightsPerCell:F2}\n" +
                   $"Memory: {memoryUsageBytes / 1024f:F2} KB";
        }
    }
}
