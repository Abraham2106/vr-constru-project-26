using System;
using System.Collections;
using ProyectoVR.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace ProyectoVR
{
    /// <summary>
    /// Ciclo día/noche: sol, ambiente, skybox y luces exteriores (3–6 s).
    /// </summary>
    [DisallowMultipleComponent]
    public class DayNightController : MonoBehaviour
    {
        public static DayNightController Instance { get; private set; }

        [Serializable]
        public class LightingSnapshot
        {
            public Color sunColor = new(1f, 0.96f, 0.84f);
            public float sunIntensity = 1.1f;
            public Vector3 sunEuler = new(50f, 170f, 0f);
            public Color ambientSky = new(0.45f, 0.55f, 0.65f);
            public Color ambientEquator = new(0.4f, 0.43f, 0.48f);
            public Color ambientGround = new(0.22f, 0.23f, 0.21f);
            public float ambientIntensity = 1f;
            public float skyboxExposure = 1f;
            public Color skyboxTint = Color.white;
            public float reflectionIntensity = 0.8f;
        }

        [SerializeField] Light directionalLight;
        [SerializeField] Material skyboxMaterial;
        [SerializeField] Material sunsetSkybox;
        [SerializeField] LightingSnapshot day = new();
        [SerializeField] LightingSnapshot night = new()
        {
            sunColor = new Color(0.35f, 0.4f, 0.55f),
            sunIntensity = 0.08f,
            sunEuler = new Vector3(-10f, 200f, 0f),
            ambientSky = new Color(0.05f, 0.07f, 0.12f),
            ambientEquator = new Color(0.04f, 0.05f, 0.08f),
            ambientGround = new Color(0.02f, 0.02f, 0.03f),
            ambientIntensity = 0.65f,
            skyboxExposure = 0.35f,
            skyboxTint = new Color(0.25f, 0.3f, 0.45f)
        };

        [Serializable]
        public sealed class ReflectionScenario
        {
            public ReflectionProbe probe;
            public Cubemap day;
            public Cubemap night;
        }

        [SerializeField] ReflectionScenario[] reflectionScenarios = Array.Empty<ReflectionScenario>();

        [SerializeField] Light[] exteriorNightLights = Array.Empty<Light>();
        [SerializeField] float blendDuration = 4.5f;

        public UnityEvent OnDayStarted;
        public UnityEvent OnNightStarted;

        public bool IsNight { get; private set; }

        Coroutine _blend;
        Material _sourceSkybox;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            if (directionalLight == null) directionalLight = RenderSettings.sun;
            RenderSettings.sun = directionalLight;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (skyboxMaterial != null)
            {
                _sourceSkybox = skyboxMaterial;
                skyboxMaterial = new Material(_sourceSkybox);
                RenderSettings.skybox = skyboxMaterial;
            }
            ApplySnapshot(day, 1f);
            SetExteriorNightLights(false);
            ApplyReflections(false);
        }

        void OnEnable() => EnvironmentCycleEvents.OnSetNightMode += HandleEnvironmentRequest;

        void OnDisable() => EnvironmentCycleEvents.OnSetNightMode -= HandleEnvironmentRequest;

        void OnDestroy()
        {
            if (_sourceSkybox != null)
            {
                if (RenderSettings.skybox == skyboxMaterial || RenderSettings.skybox == sunsetSkybox)
                    RenderSettings.skybox = _sourceSkybox;
                Destroy(skyboxMaterial);
            }
            if (Instance == this)
                Instance = null;
        }

        void HandleEnvironmentRequest(bool isNight) => BlendTo(isNight);

        /// <summary>Invocable desde el menu mediante EnvironmentCycleEvents o directamente.</summary>
        public void SetDayNight(bool isNight) => BlendTo(isNight);

        public void StartDay() => BlendTo(false);

        public void StartNight() => BlendTo(true);

        public void ToggleDayNight() => BlendTo(!IsNight);

        public void BlendTo(bool night)
        {
            if (_blend != null)
                StopCoroutine(_blend);
            _blend = StartCoroutine(BlendRoutine(night));
        }

        IEnumerator BlendRoutine(bool night)
        {
            IsNight = night;
            var from = CaptureCurrent();
            var to = night ? this.night : day;
            var duration = Mathf.Clamp(blendDuration, 3f, 6f);
            var t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                var k = Mathf.SmoothStep(0f, 1f, t / duration);
                LerpSnapshots(from, to, k);
                yield return null;
            }

            ApplySnapshot(to, 1f);
            if (skyboxMaterial != null)
                RenderSettings.skybox = night && sunsetSkybox != null ? sunsetSkybox : skyboxMaterial;
            SetExteriorNightLights(night);
            ApplyReflections(night);

            if (night)
                OnNightStarted?.Invoke();
            else
                OnDayStarted?.Invoke();

            _blend = null;
        }

        LightingSnapshot CaptureCurrent()
        {
            var s = new LightingSnapshot();
            if (directionalLight != null)
            {
                s.sunColor = directionalLight.color;
                s.sunIntensity = directionalLight.intensity;
                s.sunEuler = directionalLight.transform.eulerAngles;
            }

            s.ambientSky = RenderSettings.ambientSkyColor;
            s.ambientEquator = RenderSettings.ambientEquatorColor;
            s.ambientGround = RenderSettings.ambientGroundColor;
            s.ambientIntensity = RenderSettings.ambientIntensity;
            s.reflectionIntensity = RenderSettings.reflectionIntensity;

            if (skyboxMaterial != null)
            {
                if (skyboxMaterial.HasProperty("_Exposure"))
                    s.skyboxExposure = skyboxMaterial.GetFloat("_Exposure");
                if (skyboxMaterial.HasProperty("_SkyTint"))
                    s.skyboxTint = skyboxMaterial.GetColor("_SkyTint");
            }

            return s;
        }

        void LerpSnapshots(LightingSnapshot a, LightingSnapshot b, float k)
        {
            var s = new LightingSnapshot
            {
                sunColor = Color.Lerp(a.sunColor, b.sunColor, k),
                sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, k),
                sunEuler = Quaternion.Slerp(Quaternion.Euler(a.sunEuler), Quaternion.Euler(b.sunEuler), k).eulerAngles,
                ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, k),
                ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, k),
                ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, k),
                ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, k),
                skyboxExposure = Mathf.Lerp(a.skyboxExposure, b.skyboxExposure, k),
                skyboxTint = Color.Lerp(a.skyboxTint, b.skyboxTint, k),
                reflectionIntensity = Mathf.Lerp(a.reflectionIntensity, b.reflectionIntensity, k)
            };
            ApplySnapshot(s, k);
        }

        void ApplySnapshot(LightingSnapshot s, float _)
        {
            if (directionalLight != null)
            {
                directionalLight.color = s.sunColor;
                directionalLight.intensity = s.sunIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(s.sunEuler);
            }

            RenderSettings.ambientSkyColor = s.ambientSky;
            RenderSettings.ambientEquatorColor = s.ambientEquator;
            RenderSettings.ambientGroundColor = s.ambientGround;
            RenderSettings.ambientIntensity = s.ambientIntensity;
            RenderSettings.reflectionIntensity = s.reflectionIntensity;

            if (skyboxMaterial != null)
            {
                if (skyboxMaterial.HasProperty("_Exposure"))
                    skyboxMaterial.SetFloat("_Exposure", s.skyboxExposure);
                if (skyboxMaterial.HasProperty("_SkyTint"))
                    skyboxMaterial.SetColor("_SkyTint", s.skyboxTint);
            }
        }

        void ApplyReflections(bool isNight)
        {
            foreach (var scenario in reflectionScenarios)
            {
                if (scenario == null || scenario.probe == null) continue;
                var texture = isNight ? scenario.night : scenario.day;
                if (texture == null) continue;
                scenario.probe.mode = ReflectionProbeMode.Custom;
                scenario.probe.customBakedTexture = texture;
            }
        }

        void SetExteriorNightLights(bool enabled)
        {
            foreach (var light in exteriorNightLights)
            {
                if (light != null)
                    light.enabled = enabled;
            }
        }
    }
}

