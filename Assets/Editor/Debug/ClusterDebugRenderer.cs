using ClusterLighting.Builder;
using UnityEngine;
using UnityEditor;
using ClusterLighting.Core;

namespace ClusterLighting
{
    public static class ClusterDebugRenderer
    {
        private static readonly Color GridColor = new Color(0, 1, 0, 0.3f);
        private static readonly Color LightRangeColor = new Color(1, 0.5f, 0, 0.2f);

        public static void DrawGridWireframe(ClusterGridConfig config, Color color)
        {
            Gizmos.color = color;
            Handles.color = color;
            Vector3Int dims = config.gridDimensions;
            Bounds bounds = config.GetBounds();
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            for (int x = 0; x <= dims.x; x++)
            for (int y = 0; y <= dims.y; y++)
            {
                Vector3 start = config.boundsMin + new Vector3(x * config.cellSize.x, y * config.cellSize.y, 0);
                Gizmos.DrawLine(start, start + new Vector3(0, 0, bounds.size.z));
            }
            for (int y = 0; y <= dims.y; y++)
            for (int z = 0; z <= dims.z; z++)
            {
                Vector3 start = config.boundsMin + new Vector3(0, y * config.cellSize.y, z * config.cellSize.z);
                Gizmos.DrawLine(start, start + new Vector3(bounds.size.x, 0, 0));
            }
            for (int x = 0; x <= dims.x; x++)
            for (int z = 0; z <= dims.z; z++)
            {
                Vector3 start = config.boundsMin + new Vector3(x * config.cellSize.x, 0, z * config.cellSize.z);
                Gizmos.DrawLine(start, start + new Vector3(0, bounds.size.y, 0));
            }
        }

        public static void DrawCellHeatmap(ClusterBakeAsset asset, bool showEmpty = false)
        {
            if (asset == null || !asset.IsValid()) return;
            var config = asset.gridConfig;
            var dims = config.gridDimensions;

            int maxLights = 1;
            foreach (var cell in asset.cells)
                if (cell.lightCount > maxLights) maxLights = cell.lightCount;

            for (int z = 0; z < dims.z; z++)
            for (int y = 0; y < dims.y; y++)
            for (int x = 0; x < dims.x; x++)
            {
                var coord = new Vector3Int(x, y, z);
                var cell = asset.cells[ClusterMath.GridCellToLinearIndex(coord, dims)];
                if (cell.IsEmpty() && !showEmpty) continue;

                Bounds cb = ClusterMath.GetCellBounds(coord, config);
                Color c = GetHeatmapColor((float)cell.lightCount / maxLights);
                c.a = 0.3f;
                Gizmos.color = c;
                Gizmos.DrawCube(cb.center, cb.size * 0.9f);
                if (cell.lightCount > 0) Handles.Label(cb.center, cell.lightCount.ToString());
            }
        }

        public static void HighlightCell(Vector3Int cellCoord, ClusterGridConfig config, Color color)
        {
            Bounds cb = ClusterMath.GetCellBounds(cellCoord, config);
            Gizmos.color = color;
            Gizmos.DrawWireCube(cb.center, cb.size);
            Handles.color = color;
            Handles.Label(cb.center, $"[{cellCoord.x},{cellCoord.y},{cellCoord.z}]");
        }

        public static void DrawLightInfluence(BakedLightData light, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(light.position, light.range);
            Handles.color = color;
            Handles.Label(light.position + Vector3.up * light.range,
                $"Light {light.lightIndex} R:{light.range:F1} I:{light.intensity:F1}");
        }

        public static void DrawAllLights(ClusterBakeAsset asset)
        {
            if (asset?.lights == null) return;
            foreach (var light in asset.lights)
                DrawLightInfluence(light, LightRangeColor);
        }

        public static void DrawGridBounds(ClusterGridConfig config)
        {
            Bounds bounds = config.GetBounds();
            Gizmos.color = GridColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Handles.Label(bounds.center + Vector3.up * (bounds.size.y * 0.5f + 1f),
                $"Grid: {config.gridDimensions} ({config.GetTotalCellCount()} cells)");
        }

        public static void DrawStatisticsGUI(ClusterStatistics stats, Vector2 position)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(position.x, position.y, 250, 200));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Cluster Statistics", EditorStyles.boldLabel);
            GUILayout.Label($"Cells: {stats.totalCells} (non-empty: {stats.nonEmptyCells})");
            GUILayout.Label($"Light Refs: {stats.totalLightReferences}");
            GUILayout.Label($"Max/Cell: {stats.maxLightsPerCell}, Avg: {stats.averageLightsPerCell:F2}");
            GUILayout.Label($"Memory: {stats.memoryUsageBytes / 1024f:F2} KB");
            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        public static void DrawWorldPositionInfo(Vector3 worldPos, ClusterGridConfig config)
        {
            var coord = ClusterMath.WorldToGridCell(worldPos, config);
            int idx = ClusterMath.GridCellToLinearIndex(coord, config.gridDimensions);
            Handles.color = Color.white;
            Handles.Label(worldPos, $"Cell [{coord.x},{coord.y},{coord.z}] idx:{idx}");
            HighlightCell(coord, config, Color.yellow);
        }

        private static Color GetHeatmapColor(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.25f) return Color.Lerp(Color.blue, Color.cyan, t / 0.25f);
            if (t < 0.5f) return Color.Lerp(Color.cyan, Color.green, (t - 0.25f) / 0.25f);
            if (t < 0.75f) return Color.Lerp(Color.green, Color.yellow, (t - 0.5f) / 0.25f);
            return Color.Lerp(Color.yellow, Color.red, (t - 0.75f) / 0.25f);
        }
    }
}
