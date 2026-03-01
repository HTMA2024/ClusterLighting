using UnityEngine;

namespace ClusterLighting.Core
{
    [System.Serializable]
    public struct ClusterGridConfig
    {
        public Vector3 boundsMin;
        public Vector3 boundsMax;
        public Vector3Int gridDimensions;
        public Vector3 cellSize;

        public bool IsValid()
        {
            if (gridDimensions.x <= 0 || gridDimensions.y <= 0 || gridDimensions.z <= 0)
                return false;
            if (boundsMax.x <= boundsMin.x || boundsMax.y <= boundsMin.y || boundsMax.z <= boundsMin.z)
                return false;
            if (cellSize.x <= 0 || cellSize.y <= 0 || cellSize.z <= 0)
                return false;
            return true;
        }

        public Bounds GetBounds()
        {
            return new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin);
        }

        public int GetTotalCellCount()
        {
            return gridDimensions.x * gridDimensions.y * gridDimensions.z;
        }
    }

    [System.Serializable]
    public struct ClusterCell
    {
        public int lightStartIndex;
        public int lightCount;

        public bool IsEmpty() => lightCount == 0;
    }

    [System.Serializable]
    public struct BakedLightData
    {
        public Vector3 position;
        public float range;
        public Color color;
        public float intensity;
        public int lightIndex;

        public bool IsValid() => range >= 0 && intensity >= 0;

        public Bounds GetBounds()
        {
            return new Bounds(position, Vector3.one * (range * 2f));
        }
    }
}
