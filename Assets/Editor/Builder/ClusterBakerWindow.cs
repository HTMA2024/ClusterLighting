using UnityEngine;
using UnityEditor;
using ClusterLighting.Core;

namespace ClusterLighting.Builder
{
    public class ClusterBakerWindow : EditorWindow
    {
        private ClusterBuilder builder;
        private ClusterBakeAsset lastBakedAsset;
        private Vector2 scrollPosition;
        private bool showAdvanced = false;
        private bool showBoundsGizmo = true;

        [MenuItem("Window/Cluster Lighting/Baker")]
        public static void ShowWindow()
        {
            var window = GetWindow<ClusterBakerWindow>("Cluster Baker");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            if (builder == null) builder = new ClusterBuilder();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!builder.settings.autoCalculateBounds && showBoundsGizmo)
                DrawBoundsGizmo(builder.settings.manualBounds);
        }

        private void DrawBoundsGizmo(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 ext = bounds.extents;

            Vector3[] c = new Vector3[8];
            c[0] = center + new Vector3(-ext.x, -ext.y, -ext.z);
            c[1] = center + new Vector3( ext.x, -ext.y, -ext.z);
            c[2] = center + new Vector3( ext.x, -ext.y,  ext.z);
            c[3] = center + new Vector3(-ext.x, -ext.y,  ext.z);
            c[4] = center + new Vector3(-ext.x,  ext.y, -ext.z);
            c[5] = center + new Vector3( ext.x,  ext.y, -ext.z);
            c[6] = center + new Vector3( ext.x,  ext.y,  ext.z);
            c[7] = center + new Vector3(-ext.x,  ext.y,  ext.z);

            Handles.color = new Color(0f, 1f, 0f, 0.3f);
            // bottom
            Handles.DrawLine(c[0], c[1]); Handles.DrawLine(c[1], c[2]);
            Handles.DrawLine(c[2], c[3]); Handles.DrawLine(c[3], c[0]);
            // top
            Handles.DrawLine(c[4], c[5]); Handles.DrawLine(c[5], c[6]);
            Handles.DrawLine(c[6], c[7]); Handles.DrawLine(c[7], c[4]);
            // vertical
            Handles.DrawLine(c[0], c[4]); Handles.DrawLine(c[1], c[5]);
            Handles.DrawLine(c[2], c[6]); Handles.DrawLine(c[3], c[7]);

            Handles.color = new Color(0f, 1f, 0f, 0.05f);
            Handles.DrawSolidRectangleWithOutline(
                new[] { c[0], c[1], c[2], c[3] }, new Color(0f, 1f, 0f, 0.05f), Color.clear);

            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, center, Quaternion.identity, bounds.size.magnitude * 0.02f, EventType.Repaint);

            Handles.Label(center + Vector3.up * ext.y,
                $"Bounds\nCenter: {center}\nSize: {bounds.size}",
                new GUIStyle { normal = new GUIStyleState { textColor = Color.white }, fontSize = 12, fontStyle = FontStyle.Bold });
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            GUILayout.Label("Cluster Lighting Baker", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawGridSettings();
            EditorGUILayout.Space(10);
            DrawLightSettings();
            EditorGUILayout.Space(10);
            DrawOptimizationSettings();
            EditorGUILayout.Space(10);
            DrawAdvancedSettings();
            EditorGUILayout.Space(10);
            DrawEstimateInfo();
            EditorGUILayout.Space(10);
            DrawBakeButtons();
            EditorGUILayout.Space(10);

            if (lastBakedAsset != null) DrawBakeResult();

            EditorGUILayout.EndScrollView();
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.LabelField("Grid Configuration", EditorStyles.boldLabel);

            builder.settings.autoCalculateDimensions = EditorGUILayout.Toggle("Auto Calculate Dimensions", builder.settings.autoCalculateDimensions);
            if (builder.settings.autoCalculateDimensions)
                builder.settings.targetCellSize = EditorGUILayout.FloatField("Target Cell Size", builder.settings.targetCellSize);
            else
                builder.settings.gridDimensions = EditorGUILayout.Vector3IntField("Grid Dimensions", builder.settings.gridDimensions);

            EditorGUILayout.Space(5);
            builder.settings.autoCalculateBounds = EditorGUILayout.Toggle("Auto Calculate Bounds", builder.settings.autoCalculateBounds);

            if (!builder.settings.autoCalculateBounds)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                builder.settings.manualBounds = EditorGUILayout.BoundsField("Manual Bounds", builder.settings.manualBounds);
                EditorGUILayout.Space(3);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("From Selection", GUILayout.Height(25))) SetBoundsFromSelection();
                if (GUILayout.Button("From Camera", GUILayout.Height(25))) SetBoundsFromCamera();
                if (GUILayout.Button("From Lights", GUILayout.Height(25))) SetBoundsFromLights();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
                showBoundsGizmo = EditorGUILayout.Toggle("Show Bounds Gizmo", showBoundsGizmo);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLightSettings()
        {
            EditorGUILayout.LabelField("Light Collection", EditorStyles.boldLabel);
            builder.settings.lightLayers = LayerMaskField(new GUIContent("Light Layers"), builder.settings.lightLayers);
            builder.settings.includeInactiveLights = EditorGUILayout.Toggle("Include Inactive", builder.settings.includeInactiveLights);
            EditorGUILayout.HelpBox("Only Point Lights are supported.", MessageType.Warning);
        }

        private void DrawOptimizationSettings()
        {
            EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);
            builder.settings.optimizeLightOrder = EditorGUILayout.Toggle("Optimize Light Order", builder.settings.optimizeLightOrder);
            builder.settings.compressEmptyCells = EditorGUILayout.Toggle("Compress Empty Cells", builder.settings.compressEmptyCells);
        }

        private void DrawAdvancedSettings()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Settings", true);
        }

        private void DrawEstimateInfo()
        {
            EditorGUILayout.LabelField("Estimate", EditorStyles.boldLabel);
            if (GUILayout.Button("Calculate Estimate"))
                EditorUtility.DisplayDialog("Bake Estimate", builder.EstimateBake().ToString(), "OK");
        }

        private void DrawBakeButtons()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Validate Scene", GUILayout.Height(30)))
            {
                if (builder.ValidateScene(out var errors))
                    EditorUtility.DisplayDialog("OK", "Scene is valid.", "OK");
                else
                    EditorUtility.DisplayDialog("Failed", string.Join("\n", errors), "OK");
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Bake", GUILayout.Height(30))) BakeClusterLighting();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBakeResult()
        {
            EditorGUILayout.LabelField("Last Bake Result", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.ObjectField("Baked Asset", lastBakedAsset, typeof(ClusterBakeAsset), false);
            EditorGUILayout.LabelField($"Grid: {lastBakedAsset.gridConfig.gridDimensions}");
            EditorGUILayout.LabelField($"Cells: {lastBakedAsset.totalCellCount} (non-empty: {lastBakedAsset.nonEmptyCellCount})");
            EditorGUILayout.LabelField($"Lights: {lastBakedAsset.totalLightCount}");
            EditorGUILayout.LabelField($"Memory: {lastBakedAsset.memoryUsageBytes / 1024f:F2} KB");
            if (GUILayout.Button("Save Asset to Project")) SaveBakeAsset();
            EditorGUILayout.EndVertical();
        }

        private void BakeClusterLighting()
        {
            if (!builder.ValidateScene(out var errors))
            { EditorUtility.DisplayDialog("Failed", string.Join("\n", errors), "OK"); return; }

            EditorUtility.DisplayProgressBar("Cluster Lighting", "Baking...", 0.5f);
            try
            {
                lastBakedAsset = builder.BakeScene();
                if (lastBakedAsset != null)
                    EditorUtility.DisplayDialog("Done", lastBakedAsset.GetStatisticsString(), "OK");
                else
                    EditorUtility.DisplayDialog("Failed", "Check console.", "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void SaveBakeAsset()
        {
            if (lastBakedAsset == null) return;
            string path = EditorUtility.SaveFilePanelInProject("Save", "ClusterBakeAsset", "asset", "Save bake asset");
            if (string.IsNullOrEmpty(path)) return;
            AssetDatabase.CreateAsset(lastBakedAsset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(lastBakedAsset);
        }

        private static LayerMask LayerMaskField(GUIContent label, LayerMask layerMask)
        {
            var layers = new System.Collections.Generic.List<string>();
            var nums = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 32; i++)
            {
                string n = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(n)) { layers.Add(n); nums.Add(i); }
            }
            int mask = 0;
            for (int i = 0; i < nums.Count; i++)
                if (((1 << nums[i]) & layerMask.value) != 0) mask |= (1 << i);
            mask = EditorGUILayout.MaskField(label, mask, layers.ToArray());
            int result = 0;
            for (int i = 0; i < nums.Count; i++)
                if ((mask & (1 << i)) != 0) result |= (1 << nums[i]);
            layerMask.value = result;
            return layerMask;
        }

        private void SetBoundsFromSelection()
        {
            var sel = Selection.gameObjects;
            if (sel.Length == 0) { EditorUtility.DisplayDialog("Error", "No objects selected.", "OK"); return; }

            Bounds bounds = new Bounds(sel[0].transform.position, Vector3.zero);
            foreach (var obj in sel)
            {
                bounds.Encapsulate(obj.transform.position);
                foreach (var r in obj.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                foreach (var l in obj.GetComponentsInChildren<Light>())
                    bounds.Encapsulate(new Bounds(l.transform.position, Vector3.one * l.range * 2f));
            }
            bounds.Expand(1f);
            builder.settings.manualBounds = bounds;
            SceneView.RepaintAll();
        }

        private void SetBoundsFromCamera()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) return;
            var cam = sv.camera;
            float far = Mathf.Min(cam.farClipPlane, 500f);
            float near = cam.nearClipPlane;
            float dist = (near + far) * 0.5f;
            float size = far - near;
            builder.settings.manualBounds = new Bounds(
                cam.transform.position + cam.transform.forward * dist,
                new Vector3(size * 0.8f, size * 0.8f, size));
            SceneView.RepaintAll();
        }

        private void SetBoundsFromLights()
        {
            var pointLights = new System.Collections.Generic.List<Light>();
            foreach (var l in Object.FindObjectsOfType<Light>())
                if (l.type == LightType.Point) pointLights.Add(l);
            if (pointLights.Count == 0) { EditorUtility.DisplayDialog("Error", "No Point Lights found.", "OK"); return; }

            Bounds bounds = new Bounds(pointLights[0].transform.position, Vector3.one * pointLights[0].range * 2f);
            foreach (var l in pointLights)
                bounds.Encapsulate(new Bounds(l.transform.position, Vector3.one * l.range * 2f));
            bounds.Expand(10f);
            builder.settings.manualBounds = bounds;
            SceneView.RepaintAll();
        }
    }
}
