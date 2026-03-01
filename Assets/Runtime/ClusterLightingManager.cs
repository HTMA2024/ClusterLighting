using UnityEngine;
using ClusterLighting.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClusterLighting.Runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Cluster Lighting/Cluster Lighting Manager")]
    public class ClusterLightingManager : MonoBehaviour
    {
        [SerializeField] private ClusterBakeAsset m_BakeAsset;
        [SerializeField] private bool m_AutoInitialize = true;
        [SerializeField] private bool m_DebugLog = false;

        private ComputeBuffer m_CellBuffer;
        private ComputeBuffer m_InlinedLightsPosBuffer;
        private ComputeBuffer m_InlinedLightsColorBuffer;

        public bool IsInitialized { get; private set; }

        public ClusterBakeAsset bakeAsset
        {
            get => m_BakeAsset;
            set
            {
                if (IsInitialized) Release();
                m_BakeAsset = value;
                if (m_AutoInitialize && m_BakeAsset != null) Initialize();
            }
        }

        private void OnEnable()
        {
            if (m_AutoInitialize) Initialize();
        }

        private void OnDisable() => Release();

        public void Initialize()
        {
            if (IsInitialized) return;
            if (m_BakeAsset == null) return;
            if (!m_BakeAsset.IsValid()) { Debug.LogError("ClusterLightingManager: Bake asset is invalid."); return; }

            try
            {
                ClusterGPUUploader.UploadClusterData(m_BakeAsset,
                    out m_CellBuffer, out m_InlinedLightsPosBuffer, out m_InlinedLightsColorBuffer);
                ClusterGPUUploader.SetShaderGlobalBuffers(m_CellBuffer, m_InlinedLightsPosBuffer, m_InlinedLightsColorBuffer);
                ClusterGPUUploader.SetShaderGlobalParams(m_BakeAsset.gridConfig);
                IsInitialized = true;

                if (m_DebugLog)
                    Debug.Log($"ClusterLighting initialized: {m_BakeAsset.gridConfig.gridDimensions}, " +
                              $"{m_BakeAsset.totalCellCount} cells, {m_BakeAsset.totalLightCount} lights");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ClusterLighting init failed: {e.Message}");
                Release();
            }
        }

        public void Release()
        {
            if (!IsInitialized) return;
            ClusterGPUUploader.ReleaseBuffers(m_CellBuffer, m_InlinedLightsPosBuffer, m_InlinedLightsColorBuffer);
            m_CellBuffer = null;
            m_InlinedLightsPosBuffer = null;
            m_InlinedLightsColorBuffer = null;
            ClusterGPUUploader.ClearShaderGlobals();
            IsInitialized = false;
        }

        public void Reinitialize() { Release(); Initialize(); }

        public string GetStatistics()
        {
            return (!IsInitialized || m_BakeAsset == null) ? "Not initialized" : m_BakeAsset.GetStatisticsString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (IsInitialized)
            {
                if (m_BakeAsset == null) Release();
                else EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Reinitialize(); };
            }
            else if (m_AutoInitialize && m_BakeAsset != null && isActiveAndEnabled)
            {
                EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Initialize(); };
            }
        }

        [ContextMenu("Initialize Now")] private void InitializeFromMenu() => Initialize();
        [ContextMenu("Release Resources")] private void ReleaseFromMenu() => Release();
        [ContextMenu("Print Statistics")] private void PrintStatistics() => Debug.Log(GetStatistics());
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ClusterLightingManager))]
    public class ClusterLightingManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var t = (ClusterLightingManager)target;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                t.IsInitialized ? "Cluster Lighting is active." : "Cluster Lighting is not initialized.",
                t.IsInitialized ? MessageType.Info : MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Initialize")) { t.Initialize(); SceneView.RepaintAll(); }
            if (GUILayout.Button("Release")) { t.Release(); SceneView.RepaintAll(); }
            EditorGUILayout.EndHorizontal();

            if (t.bakeAsset != null && GUILayout.Button("Match Renderer"))
            {
                t.transform.position = t.bakeAsset.gridConfig.GetBounds().center;
                t.transform.localScale = t.bakeAsset.gridConfig.GetBounds().size;
            }
        }
    }
#endif
}
