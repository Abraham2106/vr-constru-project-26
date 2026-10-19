using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProyectoVR
{
    /// <summary>Restores native lightmaps, renderer atlas bindings and light probes for each environment.</summary>
    [DisallowMultipleComponent]
    public sealed class BakedDayNightLighting : MonoBehaviour
    {
        [Serializable]
        public sealed class Scenario
        {
            public Texture2D[] color = Array.Empty<Texture2D>();
            public Texture2D[] direction = Array.Empty<Texture2D>();
            public int[] indices = Array.Empty<int>();
            public Vector4[] scaleOffsets = Array.Empty<Vector4>();
            public float[] probeCoefficients = Array.Empty<float>();
        }

        public DayNightController controller;
        public Renderer[] receivers = Array.Empty<Renderer>();
        public Scenario day = new();
        public Scenario night = new();
        public bool IsNight { get; private set; }

        void Start()
        {
            if (controller == null) return;
            controller.OnDayStarted.AddListener(ApplyDay);
            controller.OnNightStarted.AddListener(ApplyNight);
            Apply(controller.IsNight);
        }

        void OnDestroy()
        {
            if (controller == null) return;
            controller.OnDayStarted.RemoveListener(ApplyDay);
            controller.OnNightStarted.RemoveListener(ApplyNight);
        }

        public void ApplyDay() => Apply(false);
        public void ApplyNight() => Apply(true);

        public void Apply(bool isNight)
        {
            var scenario = isNight ? night : day;
            if (scenario.color.Length == 0) return;
            var maps = new LightmapData[scenario.color.Length];
            for (int i = 0; i < maps.Length; i++)
                maps[i] = new LightmapData { lightmapColor = scenario.color[i],
                    lightmapDir = i < scenario.direction.Length ? scenario.direction[i] : null };
            LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
            LightmapSettings.lightmaps = maps;
            for (int i = 0; i < receivers.Length && i < scenario.indices.Length; i++)
            {
                if (receivers[i] == null) continue;
                receivers[i].lightmapIndex = scenario.indices[i];
                receivers[i].lightmapScaleOffset = scenario.scaleOffsets[i];
            }
            var probes = LightmapSettings.lightProbes;
            if (probes != null && scenario.probeCoefficients.Length == probes.count * 27)
            {
                var coefficients = new SphericalHarmonicsL2[probes.count];
                for (int i = 0; i < probes.count; i++)
                    for (int channel = 0; channel < 3; channel++)
                        for (int coefficient = 0; coefficient < 9; coefficient++)
                            coefficients[i][channel, coefficient] = scenario.probeCoefficients[i * 27 + channel * 9 + coefficient];
                probes.bakedProbes = coefficients;
            }
            IsNight = isNight;
        }
    }
}
