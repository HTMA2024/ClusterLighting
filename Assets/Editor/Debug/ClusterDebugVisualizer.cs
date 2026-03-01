using UnityEngine;
using ClusterLighting.Core;
using ClusterLighting.Builder;

namespace ClusterLighting
{
    public class ClusterDebugVisualizer : MonoBehaviour
    {
        [SerializeField] public ClusterBakeAsset bakeAsset;

        [Header("Visualization")]
        [SerializeField] private bool showGridWireframe = true;
        [SerializeField] private bool showGridBounds = true;
        [SerializeField] private bool showCellHeatmap = false;
        [SerializeField] private bool showEmptyCells = false;
        [SerializeField] private bool showLights = true;
        [SerializeField] private bool showStatistics = false;
        [SerializeField] private Color gridColor = new Color(0, 1, 0, 0.3f);

        private void OnDrawGizmos()
        {
            if (bakeAsset == null || !bakeAsset.IsValid()) return;

            if (showGridBounds) ClusterDebugRenderer.DrawGridBounds(bakeAsset.gridConfig);
            if (showGridWireframe) ClusterDebugRenderer.DrawGridWireframe(bakeAsset.gridConfig, gridColor);
            if (showCellHeatmap) ClusterDebugRenderer.DrawCellHeatmap(bakeAsset, showEmptyCells);
            if (showLights) ClusterDebugRenderer.DrawAllLights(bakeAsset);

            if (showStatistics)
            {
                var stats = ClusterDataEncoder.GenerateStatistics(bakeAsset.cells, bakeAsset.lightIndices);
                stats.memoryUsageBytes = bakeAsset.memoryUsageBytes;
                ClusterDebugRenderer.DrawStatisticsGUI(stats, new Vector2(10, 10));
            }
        }
    }
}
