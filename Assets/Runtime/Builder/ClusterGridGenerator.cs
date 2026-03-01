using UnityEngine;
using ClusterLighting.Core;

namespace ClusterLighting.Builder
{
    public class ClusterGridGenerator
    {
        private const int MAX_DIMENSION = 256;
        private const int MAX_TOTAL_CELLS = 256 * 256 * 256;

        public static ClusterGridConfig GenerateGrid(Bounds sceneBounds, Vector3Int targetDimensions)
        {
            const float BOUNDS_PADDING = 0.01f;
            Vector3 padding = Vector3.one * BOUNDS_PADDING;

            ClusterGridConfig config = new ClusterGridConfig
            {
                boundsMin = sceneBounds.min - padding,
                boundsMax = sceneBounds.max + padding,
                gridDimensions = targetDimensions
            };

            Vector3 totalSize = config.boundsMax - config.boundsMin;
            config.cellSize = new Vector3(
                totalSize.x / targetDimensions.x,
                totalSize.y / targetDimensions.y,
                totalSize.z / targetDimensions.z);
            return config;
        }

        public static Vector3Int CalculateOptimalDimensions(Bounds sceneBounds, float targetCellSize)
        {
            Vector3 size = sceneBounds.size;
            Vector3Int dims = new Vector3Int(
                Mathf.Max(1, Mathf.RoundToInt(size.x / targetCellSize)),
                Mathf.Max(1, Mathf.RoundToInt(size.y / targetCellSize)),
                Mathf.Max(1, Mathf.RoundToInt(size.z / targetCellSize)));

            dims.x = Mathf.Min(dims.x, MAX_DIMENSION);
            dims.y = Mathf.Min(dims.y, MAX_DIMENSION);
            dims.z = Mathf.Min(dims.z, MAX_DIMENSION);

            long total = (long)dims.x * dims.y * dims.z;
            if (total > MAX_TOTAL_CELLS)
            {
                float scale = Mathf.Pow(MAX_TOTAL_CELLS / (float)total, 1f / 3f);
                dims.x = Mathf.Max(1, Mathf.FloorToInt(dims.x * scale));
                dims.y = Mathf.Max(1, Mathf.FloorToInt(dims.y * scale));
                dims.z = Mathf.Max(1, Mathf.FloorToInt(dims.z * scale));
            }
            return dims;
        }

        public static ClusterCell[] InitializeCells(ClusterGridConfig config)
        {
            int total = config.GetTotalCellCount();
            ClusterCell[] cells = new ClusterCell[total];
            for (int i = 0; i < total; i++)
                cells[i] = new ClusterCell { lightStartIndex = 0, lightCount = 0 };
            return cells;
        }

        public static bool ValidateGridConfig(ClusterGridConfig config, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!config.IsValid())
            { errorMessage = "Invalid dimensions, bounds, or cell size."; return false; }

            if (config.gridDimensions.x > MAX_DIMENSION || config.gridDimensions.y > MAX_DIMENSION || config.gridDimensions.z > MAX_DIMENSION)
            { errorMessage = $"Dimension exceeds max {MAX_DIMENSION}."; return false; }

            long total = (long)config.gridDimensions.x * config.gridDimensions.y * config.gridDimensions.z;
            if (total > MAX_TOTAL_CELLS)
            { errorMessage = $"Cell count ({total}) exceeds max ({MAX_TOTAL_CELLS})."; return false; }

            const float MIN_CELL_SIZE = 0.1f;
            if (config.cellSize.x < MIN_CELL_SIZE || config.cellSize.y < MIN_CELL_SIZE || config.cellSize.z < MIN_CELL_SIZE)
            { errorMessage = $"Cell size below minimum {MIN_CELL_SIZE}."; return false; }

            return true;
        }
    }
}
