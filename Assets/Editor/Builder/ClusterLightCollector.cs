using UnityEngine;
using System.Collections.Generic;
using ClusterLighting.Core;

namespace ClusterLighting.Builder
{
    public class ClusterLightCollector
    {
        public static List<BakedLightData> CollectLights(LayerMask lightLayers, bool includeInactive = false)
        {
            Light[] allLights = Object.FindObjectsOfType<Light>(includeInactive);
            var result = new List<BakedLightData>();

            for (int i = 0; i < allLights.Length; i++)
            {
                Light light = allLights[i];
                if (((1 << light.gameObject.layer) & lightLayers) == 0) continue;

                if (light.type != LightType.Point)
                {
                    Debug.LogWarning($"Skipping non-Point light '{light.gameObject.name}'.");
                    continue;
                }

                BakedLightData data = ConvertLight(light, i);
                if (ValidateLightData(data, out string err))
                    result.Add(data);
                else
                    Debug.LogWarning($"Light '{light.gameObject.name}' validation failed: {err}");
            }

            Debug.Log($"Collected {result.Count} lights.");
            return result;
        }

        public static BakedLightData ConvertLight(Light light, int index)
        {
            return new BakedLightData
            {
                position = light.transform.position,
                range = light.range,
                color = light.color,
                intensity = light.intensity,
                lightIndex = index
            };
        }

        public static List<BakedLightData> FilterSupportedLights(List<Light> lights)
        {
            var result = new List<BakedLightData>();
            for (int i = 0; i < lights.Count; i++)
                if (lights[i].type == LightType.Point)
                    result.Add(ConvertLight(lights[i], i));
            return result;
        }

        public static bool ValidateLightData(BakedLightData light, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!light.IsValid()) { errorMessage = "Invalid range or intensity."; return false; }
            if (light.range < 0.01f) { errorMessage = "Range too small (< 0.01)."; return false; }
            if (light.intensity < 0.001f) { errorMessage = "Intensity too small (< 0.001)."; return false; }
            return true;
        }

        public static List<BakedLightData> CollectAndConvertLights(bool includeInactive = false, LayerMask? layerMask = null)
        {
            return CollectLights(layerMask ?? ~0, includeInactive);
        }
    }
}
