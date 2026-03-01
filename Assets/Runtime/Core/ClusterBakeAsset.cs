using UnityEngine;

namespace ClusterLighting.Core
{
    [CreateAssetMenu(fileName = "ClusterBakeAsset", menuName = "ClusterLighting/Bake Asset", order = 1)]
    public class ClusterBakeAsset : ScriptableObject
    {
        [Header("Grid Configuration")]
        public ClusterGridConfig gridConfig;

        [Header("Baked Data")]
        public ClusterCell[] cells;
        public int[] lightIndices;
        public BakedLightData[] lights;

        [Header("Inlined Light Data")]
        public Vector4[] inlinedPositionAndRange;  // xyz: pos, w: range
        public Vector4[] inlinedColorAndIntensity; // rgb: color, a: intensity

        [Header("Statistics")]
        public int totalLightCount;
        public int totalCellCount;
        public int nonEmptyCellCount;
        public int inlinedLightCount;
        public long memoryUsageBytes;

        public bool IsValid()
        {
            if (!gridConfig.IsValid()) return false;
            if (cells == null || cells.Length == 0) return false;
            if (lightIndices == null || lights == null) return false;
            if (cells.Length != gridConfig.GetTotalCellCount()) return false;
            if (inlinedPositionAndRange == null || inlinedColorAndIntensity == null) return false;
            if (inlinedPositionAndRange.Length != inlinedColorAndIntensity.Length) return false;
            return true;
        }

        public void UpdateStatistics()
        {
            totalCellCount = cells != null ? cells.Length : 0;
            totalLightCount = lights != null ? lights.Length : 0;
            inlinedLightCount = inlinedPositionAndRange != null ? inlinedPositionAndRange.Length : 0;

            nonEmptyCellCount = 0;
            if (cells != null)
                foreach (var cell in cells)
                    if (!cell.IsEmpty()) nonEmptyCellCount++;

            // Cell: 8B, Inlined entry: 32B (2 * Vector4)
            memoryUsageBytes = (cells != null ? cells.Length * 8 : 0)
                             + (inlinedPositionAndRange != null ? inlinedPositionAndRange.Length * 32 : 0);
        }

        public string GetStatisticsString()
        {
            return $"Grid: {gridConfig.gridDimensions}\n" +
                   $"Total Cells: {totalCellCount}\n" +
                   $"Non-Empty Cells: {nonEmptyCellCount}\n" +
                   $"Unique Lights: {totalLightCount}\n" +
                   $"Inlined Lights: {inlinedLightCount}\n" +
                   $"Memory: {memoryUsageBytes / 1024f:F2} KB";
        }
    }
}
