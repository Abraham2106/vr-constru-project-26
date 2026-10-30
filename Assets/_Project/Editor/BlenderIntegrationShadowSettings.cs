using System;
using ProyectoVR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Native URP settings for the reviewed house. No gameplay layer or collider changes.</summary>
public static class BlenderIntegrationShadowSettings
{
    const string ScenePath = "Assets/Scenes/Vivienda_Integracion_Blender.unity";
    const uint HouseCasterMask = 2;

    [MenuItem("Vivienda/Integracion Blender/Configurar sombras nativas")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || Lightmapping.isRunning || scene.path != ScenePath)
            throw new InvalidOperationException("Abrir la escena de integracion fuera de Play y de horneado.");

        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
            "Assets/_Project/Settings/URP_Vivienda.asset");
        if (!pipeline) throw new InvalidOperationException("Falta el asset URP de la vivienda.");
        var settings = new SerializedObject(pipeline);
        foreach (var name in new[] { "m_AdditionalLightShadowsSupported", "m_SoftShadowsSupported", "m_SupportsLightLayers" })
            settings.FindProperty(name).boolValue = true;
        settings.FindProperty("m_SoftShadowQuality").intValue = 1; // Low.
        settings.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = 1024;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);

        var tagAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var tags = new SerializedObject(tagAsset);
        var layers = tags.FindProperty("m_RenderingLayers");
        if (layers.arraySize > 1 && !string.IsNullOrEmpty(layers.GetArrayElementAtIndex(1).stringValue) &&
            layers.GetArrayElementAtIndex(1).stringValue != "HouseInteriorCasters")
            throw new InvalidOperationException("La Rendering Layer 1 ya tiene otro uso.");
        layers.arraySize = Math.Max(2, layers.arraySize);
        layers.GetArrayElementAtIndex(1).stringValue = "HouseInteriorCasters";
        tags.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tagAsset);

        var house = GameObject.Find("Vivienda");
        if (!house) throw new InvalidOperationException("No se encontro la casa principal.");
        foreach (var renderer in house.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            renderer.renderingLayerMask |= HouseCasterMask; // Retain Default for sun and all existing lights.
            EditorUtility.SetDirty(renderer);
            if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        foreach (var room in UnityEngine.Object.FindObjectsByType<RoomLightGroup>())
        {
            var serialized = new SerializedObject(room).FindProperty("controlledLights");
            for (int i = 0; i < serialized.arraySize; i++)
            {
                var light = serialized.GetArrayElementAtIndex(i).objectReferenceValue as Light;
                if (!light) continue;
                var data = light.GetComponent<UniversalAdditionalLightData>();
                if (!data) data = light.gameObject.AddComponent<UniversalAdditionalLightData>();
                data.customShadowLayers = true;
                data.shadowRenderingLayers = HouseCasterMask;
                ConfigureBias(light, data, .03f, .03f);
                var detail = new SerializedObject(data);
                detail.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = 0; // Low: 256px.
                detail.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        var sun = RenderSettings.sun;
        if (!sun) throw new InvalidOperationException("Falta el sol de la escena.");
        var sunData = sun.GetComponent<UniversalAdditionalLightData>();
        if (!sunData) sunData = sun.gameObject.AddComponent<UniversalAdditionalLightData>();
        ConfigureBias(sun, sunData, .01f, .03f);

        // Rendering Layers are unsupported on OpenGL ES. Quest uses the native Vulkan path.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Sombras URP Low, atlas 1024, focos 256 y casters de casa configurados; Android Vulkan.");
    }

    static void ConfigureBias(Light light, UniversalAdditionalLightData data, float depth, float normal)
    {
        light.shadowBias = depth;
        light.shadowNormalBias = normal;
        var serialized = new SerializedObject(data);
        serialized.FindProperty("m_UsePipelineSettings").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(light);
        EditorUtility.SetDirty(data);
    }
}
