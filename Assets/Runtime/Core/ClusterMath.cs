using UnityEngine;

namespace ClusterLighting.Core
{
    public static class ClusterMath
    {
        public static Vector3Int WorldToGridCell(Vector3 worldPos, ClusterGridConfig config)
        {
            Vector3 localPos = worldPos - config.boundsMin;
            Vector3Int cellCoord = new Vector3Int(
                Mathf.FloorToInt(localPos.x / config.cellSize.x),
                Mathf.FloorToInt(localPos.y / config.cellSize.y),
                Mathf.FloorToInt(localPos.z / config.cellSize.z));

            cellCoord.x = Mathf.Clamp(cellCoord.x, 0, config.gridDimensions.x - 1);
            cellCoord.y = Mathf.Clamp(cellCoord.y, 0, config.gridDimensions.y - 1);
            cellCoord.z = Mathf.Clamp(cellCoord.z, 0, config.gridDimensions.z - 1);
            return cellCoord;
        }

        // z * (dimX * dimY) + y * dimX + x
        public static int GridCellToLinearIndex(Vector3Int cellCoord, Vector3Int gridDimensions)
        {
            return cellCoord.z * (gridDimensions.x * gridDimensions.y)
                 + cellCoord.y * gridDimensions.x
                 + cellCoord.x;
        }

        public static Vector3Int LinearIndexToGridCell(int linearIndex, Vector3Int gridDimensions)
        {
            int layerSize = gridDimensions.x * gridDimensions.y;
            int z = linearIndex / layerSize;
            int remainder = linearIndex % layerSize;
            int y = remainder / gridDimensions.x;
            int x = remainder % gridDimensions.x;
            return new Vector3Int(x, y, z);
        }

        public static Bounds GetCellBounds(Vector3Int cellCoord, ClusterGridConfig config)
        {
            Vector3 cellMin = config.boundsMin + new Vector3(
                cellCoord.x * config.cellSize.x,
                cellCoord.y * config.cellSize.y,
                cellCoord.z * config.cellSize.z);
            return new Bounds(cellMin + config.cellSize * 0.5f, config.cellSize);
        }

        public static bool SphereIntersectsAABB(Vector3 sphereCenter, float radius, Bounds aabb)
        {
            Vector3 closestPoint = new Vector3(
                Mathf.Clamp(sphereCenter.x, aabb.min.x, aabb.max.x),
                Mathf.Clamp(sphereCenter.y, aabb.min.y, aabb.max.y),
                Mathf.Clamp(sphereCenter.z, aabb.min.z, aabb.max.z));
            return (closestPoint - sphereCenter).sqrMagnitude <= radius * radius;
        }

        public static (Vector3Int min, Vector3Int max) GetLightInfluenceCellRange(
            Vector3 lightPos, float range, ClusterGridConfig config)
        {
            Vector3Int minCell = WorldToGridCell(lightPos - Vector3.one * range, config);
            Vector3Int maxCell = WorldToGridCell(lightPos + Vector3.one * range, config);
            maxCell.x = Mathf.Max(maxCell.x, minCell.x);
            maxCell.y = Mathf.Max(maxCell.y, minCell.y);
            maxCell.z = Mathf.Max(maxCell.z, minCell.z);
            return (minCell, maxCell);
        }
    }
}
