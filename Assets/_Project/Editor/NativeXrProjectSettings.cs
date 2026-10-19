using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.PackageManager.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using ProyectoVR;

public static class NativeXrProjectSettings
{
    [MenuItem("Vivienda/XR/Modo escritorio")]
    public static void Desktop() => SetPreview(DesktopPreview.PreviewMode.Desktop);

    [MenuItem("Vivienda/XR/Modo simulador XR")]
    public static void Simulator() => SetPreview(DesktopPreview.PreviewMode.XRSimulator);

    [MenuItem("Vivienda/XR/Modo visor conectado")]
    public static void Headset() => SetPreview(DesktopPreview.PreviewMode.Headset);

    static void SetPreview(DesktopPreview.PreviewMode mode)
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Selecciona el modo fuera de Play."); return; }
        var preview = Object.FindAnyObjectByType<DesktopPreview>();
        var so = new SerializedObject(preview);
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach (var device in Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Include))
        {
            device.gameObject.SetActive(mode == DesktopPreview.PreviewMode.XRSimulator);
            // Walk through XRI locomotion instead of translating the tracked head into colliders.
            device.translateXInput.inputSourceMode = XRInputValueReader.InputSourceMode.Unused;
            device.translateYInput.inputSourceMode = XRInputValueReader.InputSourceMode.Unused;
            device.translateZInput.inputSourceMode = XRInputValueReader.InputSourceMode.Unused;
            EditorUtility.SetDirty(device);
        }
        var desktopSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (desktopSettings != null)
        {
            desktopSettings.InitManagerOnStart = mode == DesktopPreview.PreviewMode.Headset;
            EditorUtility.SetDirty(desktopSettings);
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Vivienda/XR/Importar simulador oficial")]
    public static void ImportSimulator()
    {
        var sample = Sample.FindByPackage("com.unity.xr.interaction.toolkit", "3.6.1").First(s => s.displayName == "XR Interaction Simulator");
        if (!sample.isImported) sample.Import(Sample.ImportOptions.OverridePreviousImports);
    }

    [MenuItem("Vivienda/XR/Instalar simulador oficial en escena")]
    public static void InstallSimulator()
    {
        const string path = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        if (Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Include).Length == 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new System.InvalidOperationException("Importar primero el sample XR Interaction Simulator.");
            var simulator = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            simulator.tag = "EditorOnly";
            simulator.SetActive(false);
        }
#pragma warning disable CS0618
        foreach (var legacy in Object.FindObjectsByType<XRDeviceSimulator>(FindObjectsInactive.Include)) Undo.DestroyObjectImmediate(legacy.gameObject);
#pragma warning restore CS0618
        Desktop();
    }

    [MenuItem("Vivienda/XR/Guardar configuracion del proyecto")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Salir de Play antes de configurar el proyecto."); return; }
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings != null)
        {
            settings.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(settings);
        }
        var androidSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (androidSettings != null)
        {
            androidSettings.InitManagerOnStart = true;
            EditorUtility.SetDirty(androidSettings);
        }
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] {GraphicsDeviceType.Direct3D11});
        var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Select(scene => new EditorBuildSettingsScene(scene.path, scene.path == scenePath)).ToArray();
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline != null)
        {
            pipeline.msaaSampleCount = 4;
            EditorUtility.SetDirty(pipeline);
        }
        var rig = Object.FindAnyObjectByType<XROrigin>();
        JourneyGeometry.EnsureFolder("Assets/_Project/Prefabs/XR");
        PrefabUtility.SaveAsPrefabAssetAndConnect(rig.gameObject, "Assets/_Project/Prefabs/XR/ViviendaXR.prefab", InteractionMode.AutomatedAction);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Rig guardado como prefab; OpenXR Single Pass Instanced, latencia de entrada y MSAA 4x configurados. Windows usa D3D11.");
    }

    [MenuItem("Vivienda/XR/Configurar sombras URP")]
    public static void Shadows()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) throw new System.InvalidOperationException("Se requiere URP.");
        var so = new SerializedObject(pipeline);
        so.FindProperty("m_MainLightShadowsSupported").boolValue = true;
        so.FindProperty("m_MainLightShadowmapResolution").intValue = 1024;
        so.FindProperty("m_ShadowCascadeCount").intValue = 1;
        so.FindProperty("m_ShadowDistance").floatValue = 25;
        so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (RenderSettings.sun != null)
        {
            RenderSettings.sun.shadows = LightShadows.Hard;
            RenderSettings.sun.shadowStrength = .8f;
            RenderSettings.sun.shadowBias = .03f;
            RenderSettings.sun.shadowNormalBias = .35f;
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Sombras nativas URP habilitadas; backend=" + SystemInfo.graphicsDeviceType);
    }
}
