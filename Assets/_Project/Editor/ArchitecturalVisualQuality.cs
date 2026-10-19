using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using ProyectoVR;

public static class ArchitecturalVisualQuality
{
    const string Folder = "Assets/_Project/Settings/Visualizacion";
    const string RootName = "Calidad visual arquitectonica";

    [MenuItem("Vivienda/Graficos/Aplicar perfil de visualizacion")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de configurar los graficos.");
        JourneyGeometry.EnsureFolder(Folder + "/Reflections");
        var rig = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
        var camera = rig.Camera;
        Undo.RecordObject(camera, "Configurar camara arquitectonica");
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 80;
        camera.allowHDR = true;
        camera.allowMSAA = true;
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.None;
        EditorUtility.SetDirty(cameraData);
        EditorUtility.SetDirty(camera);

        var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var pipelineData = new SerializedObject(pipeline);
        pipelineData.FindProperty("m_ReflectionProbeBoxProjection").boolValue = true;
        pipelineData.FindProperty("m_ColorGradingMode").intValue = (int)ColorGradingMode.HighDynamicRange;
        pipelineData.ApplyModifiedPropertiesWithoutUndo();
        pipeline.msaaSampleCount = 4;
        pipeline.renderScale = 1;
        EditorUtility.SetDirty(pipeline);

        var root = GameObject.Find(RootName) ?? new GameObject(RootName);
        var volume = root.GetComponent<Volume>();
        if (volume == null) volume = Undo.AddComponent<Volume>(root);
        volume.isGlobal = true;
        volume.priority = 10;
        volume.weight = 1;
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder + "/Arquitectura_Neutral.asset");
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Folder + "/Arquitectura_Neutral.asset");
        }
        if (!profile.TryGet<Tonemapping>(out var tone))
        {
            tone = profile.Add<Tonemapping>(true);
            AssetDatabase.AddObjectToAsset(tone, profile);
        }
        tone.mode.Override(TonemappingMode.Neutral);
        if (!profile.TryGet<ColorAdjustments>(out var color))
        {
            color = profile.Add<ColorAdjustments>(true);
            AssetDatabase.AddObjectToAsset(color, profile);
        }
        color.postExposure.Override(.65f);
        color.contrast.Override(4);
        color.saturation.Override(-3);
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(tone);
        EditorUtility.SetDirty(color);
        EditorUtility.SetDirty(volume);

        var fixtures = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Journey/Plafones.mat");
        fixtures.EnableKeyword("_EMISSION");
        fixtures.SetColor("_BaseColor", new Color(.78f, .78f, .74f));
        fixtures.SetColor("_EmissionColor", new Color(1, .82f, .6f) * .8f);
        fixtures.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        // The shared material keeps the emission variant; each room supplies its on/off color.
        foreach (var room in UnityEngine.Object.FindObjectsByType<RoomLightGroup>())
            room.SetLightsOn(room.LightsOn);
        EditorUtility.SetDirty(fixtures);

        var source = GameObject.Find("Vivienda");
        var texturePaths = new HashSet<string>();
        foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.GetComponentInParent<DoorHinge>() == null && renderer.GetComponentInParent<WindowHinge>() == null)
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) | StaticEditorFlags.ReflectionProbeStatic);
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;
                foreach (var property in new[] { "_BaseMap", "_BumpMap", "_SpecGlossMap", "_MetallicGlossMap" })
                    if (material.HasProperty(property) && material.GetTexture(property) != null)
                        texturePaths.Add(AssetDatabase.GetAssetPath(material.GetTexture(property)));
            }
        }
        foreach (var path in texturePaths.Where(path => path.StartsWith("Assets/")))
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            if (importer.filterMode == FilterMode.Trilinear && importer.anisoLevel == 4 && importer.mipmapEnabled) continue;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Perfil nativo aplicado: camara 0.05-80m; MSAA4; Neutral HDR; dos ajustes de color; difusores emisivos; texturas con mipmaps/trilineal/aniso4. Texturas revisadas=" + texturePaths.Count);
    }

    [MenuItem("Vivienda/Graficos/Hornear reflejos de dia y noche")]
    public static void BakeReflections()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de hornear reflejos.");
        var controller = UnityEngine.Object.FindAnyObjectByType<DayNightController>();
        var data = new SerializedObject(controller);
        var sun = (Light)data.FindProperty("directionalLight").objectReferenceValue;
        var originalSky = RenderSettings.skybox;
        var captureSky = UnityEngine.Object.Instantiate((Material)data.FindProperty("skyboxMaterial").objectReferenceValue);
        var lights = UnityEngine.Object.FindObjectsByType<Light>().ToDictionary(light => light, light => light.enabled);
        var root = GameObject.Find(RootName) ?? new GameObject(RootName);
        var probes = new List<ReflectionProbe>();
        var day = new List<Cubemap>();
        var night = new List<Cubemap>();
        var report = new StringBuilder();
        RenderSettings.skybox = captureSky;
        try
        {
            for (int floor = 0; floor < 2; floor++)
            {
                var name = "Reflejos Piso " + (floor + 1);
                var child = root.transform.Find(name);
                var target = child != null ? child.gameObject : new GameObject(name);
                target.transform.SetParent(root.transform, false);
                target.transform.position = new Vector3(2.6f, floor == 0 ? 1.4f : 4.45f, .7f);
                var probe = target.GetComponent<ReflectionProbe>();
                if (probe == null) probe = Undo.AddComponent<ReflectionProbe>(target);
                var center = new Vector3(.3f, floor == 0 ? 1.4f : 4.45f, -1.7f);
                probe.center = center - target.transform.position;
                probe.size = new Vector3(8.8f, floor == 0 ? 2.8f : 2.9f, 11);
                probe.resolution = 128;
                probe.hdr = true;
                probe.boxProjection = true;
                probe.blendDistance = .3f;
                probe.importance = 10;
                probe.nearClipPlane = .05f;
                probe.farClipPlane = 40;
                probe.cullingMask = ~(1 << 5);
                probe.clearFlags = ReflectionProbeClearFlags.Skybox;
                probes.Add(probe);
                for (int mode = 0; mode < 2; mode++)
                {
                    bool isNight = mode == 1;
                    foreach (var light in lights.Keys) light.enabled = light == sun || (isNight && light.name.StartsWith("Acceso "));
                    SetCaptureEnvironment(data.FindProperty(isNight ? "night" : "day"), sun, captureSky);
                    probe.mode = ReflectionProbeMode.Baked;
                    var path = Folder + "/Reflections/Piso" + (floor + 1) + (isNight ? "_Noche" : "_Dia") + ".exr";
                    if (!Lightmapping.BakeReflectionProbe(probe, path)) throw new InvalidOperationException("No se pudo hornear " + path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                    if (cube == null) throw new InvalidOperationException("Cubemap no importado: " + path);
                    (isNight ? night : day).Add(cube);
                    report.AppendLine("PASS " + path + ": cubemap HDR 128; bake nativo.");
                }
            }
            var scenarios = data.FindProperty("reflectionScenarios");
            scenarios.arraySize = probes.Count;
            for (int index = 0; index < probes.Count; index++)
            {
                var scenario = scenarios.GetArrayElementAtIndex(index);
                scenario.FindPropertyRelative("probe").objectReferenceValue = probes[index];
                scenario.FindPropertyRelative("day").objectReferenceValue = day[index];
                scenario.FindPropertyRelative("night").objectReferenceValue = night[index];
                probes[index].mode = ReflectionProbeMode.Custom;
                probes[index].customBakedTexture = day[index];
                EditorUtility.SetDirty(probes[index]);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
        finally
        {
            SetCaptureEnvironment(data.FindProperty("day"), sun, captureSky);
            RenderSettings.skybox = originalSky;
            foreach (var item in lights) item.Key.enabled = item.Value;
            UnityEngine.Object.DestroyImmediate(captureSky);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/Architectural-Reflections.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    static void SetCaptureEnvironment(SerializedProperty snapshot, Light sun, Material sky)
    {
        sun.intensity = snapshot.FindPropertyRelative("sunIntensity").floatValue;
        sun.color = snapshot.FindPropertyRelative("sunColor").colorValue;
        sun.transform.rotation = Quaternion.Euler(snapshot.FindPropertyRelative("sunEuler").vector3Value);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = snapshot.FindPropertyRelative("ambientSky").colorValue;
        RenderSettings.ambientEquatorColor = snapshot.FindPropertyRelative("ambientEquator").colorValue;
        RenderSettings.ambientGroundColor = snapshot.FindPropertyRelative("ambientGround").colorValue;
        RenderSettings.ambientIntensity = snapshot.FindPropertyRelative("ambientIntensity").floatValue;
        RenderSettings.reflectionIntensity = snapshot.FindPropertyRelative("reflectionIntensity").floatValue;
        sky.SetFloat("_Exposure", snapshot.FindPropertyRelative("skyboxExposure").floatValue);
        sky.SetColor("_SkyTint", snapshot.FindPropertyRelative("skyboxTint").colorValue);
    }
}
