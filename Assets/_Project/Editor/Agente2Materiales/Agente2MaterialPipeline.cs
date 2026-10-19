#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Agente2MaterialPipeline
{
    // Agente2Materiales assembly — auto-run via agente2_autorun.flag
    const string PendingFlag = "Assets/_Project/.agente2_pending";
    const string AutoRunFlag = "Assets/_Project/Docs/Logs/agente2_autorun.flag";
    const string LogPath = "Assets/_Project/Docs/Logs/agente2_materiales.md";
    const string BakeReport = "Assets/_Project/Docs/Reports/materials_needing_bake.md";
    const string FbxPath = "Assets/Vivienda/Vivienda.fbx";
    const string SourceMaterials = "Assets/Vivienda/materials";
    const string SourceTextures = "Assets/Vivienda/textures";
    const string FidelityMaterials = "Assets/Vivienda/Fidelity/Materials";
    const string FidelityTextures = "Assets/Vivienda/Fidelity/Textures";
    const string DestMaterials = "Assets/_Project/Art/Materials/Vivienda";
    const string DestTextures = "Assets/_Project/Art/Textures/Vivienda";
    const string LibraryPath = "Assets/_Project/Art/Materials/Library";
    const string DeprecatedPath = "Assets/_Project/_Deprecated";
    const string SettingsPath = "Assets/_Project/Settings";
    const string ScenePath = "Assets/Scenes/Vivienda_Prueba.unity";
    const string CapturesDir = "Assets/_Project/Docs/Reports/captures";
    const string EditorLockPath = "Assets/_Project/Docs/.locks/editor.lock";
    const string AgentName = "Agente2-Materiales";
    const bool SkipAssetCopy = true;
    const double LockMaxAgeMinutes = 30;

    static readonly string[] ProceduralNames =
    {
        "Procedural Grass", "Procedural Brown Leather", "Procedural pearl blue painted plastic"
    };

    static readonly Dictionary<string, string> TextureDonorByMaterial = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Procedural Grass", "Grass 01" },
        { "Procedural Brown Leather", "Brown leather" },
        { "Procedural pearl blue painted plastic", "Plastic" },
        { "Green grass or lawn", "Grass 01" },
        { "Meadow with grass and Rocks", "Grass 01" },
        { "Stylized Grass", "Grass 01" },
        { "Synthetic turf", "Grass 01" },
        { "Nature wall with gras", "Grass 01" },
    };

    [InitializeOnLoadMethod]
    static void AutoRunIfPending()
    {
        EditorApplication.delayCall += TryAutoRunFromFlags;
        EditorApplication.update += UpdateAutoRunFromFlags;
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded()
    {
        if (!AutoRunRequested())
            return;
        EditorApplication.delayCall += TryAutoRunFromFlags;
        EditorApplication.update += UpdateAutoRunFromFlags;
    }

    static bool AutoRunRequested() =>
        File.Exists(Abs(PendingFlag)) || File.Exists(Abs(AutoRunFlag));

    static void ClearAutoRunFlags()
    {
        if (File.Exists(Abs(PendingFlag)))
            File.Delete(Abs(PendingFlag));
        if (File.Exists(Abs(AutoRunFlag)))
            File.Delete(Abs(AutoRunFlag));
    }

    static void UpdateAutoRunFromFlags()
    {
        if (!AutoRunRequested())
        {
            EditorApplication.update -= UpdateAutoRunFromFlags;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        TryAutoRunFromFlags();
    }

    static void TryAutoRunFromFlags()
    {
        if (!AutoRunRequested())
            return;
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += TryAutoRunFromFlags;
            return;
        }

        if (!RunInternal())
        {
            EditorApplication.update -= UpdateAutoRunFromFlags;
            EditorApplication.update += UpdateAutoRunFromFlags;
            return;
        }

        ClearAutoRunFlags();
        EditorApplication.update -= UpdateAutoRunFromFlags;
    }

    [MenuItem("Vivienda/Agente 2/Ejecutar pipeline materiales")]
    public static void RunMenu()
    {
        if (RunInternal())
            ClearAutoRunFlags();
    }

    static bool RunInternal()
    {
        var log = new StringBuilder();
        log.AppendLine("# Agente 2 — Materiales y texturas");
        log.AppendLine();
        log.AppendLine($"**Ejecutado:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        log.AppendLine();

        EnsureFolders();
        if (!TryAcquireEditorLock(log))
        {
            AppendRunLog(log);
            Debug.LogWarning("[Agente2] Editor lock ocupado; reintentará automáticamente.");
            return false;
        }

        try
        {
        DetectPipeline(log);
        EnsureUrPAssets(log);
        var pipeline = DetectPipeline(log);

        Dictionary<string, string> guidMap;
        if (SkipAssetCopy)
        {
            log.AppendLine("- Copia de assets: **omitida** (ya presente en `_Project/Art`).");
            guidMap = BuildGuidMapFromExisting(SourceMaterials, DestMaterials);
            guidMap = MergeGuidMaps(guidMap, BuildGuidMapFromExisting(FidelityMaterials, DestMaterials + "/Fidelity"));
        }
        else
        {
            guidMap = CopyTree(SourceMaterials, DestMaterials, ".mat");
            CopyTree(SourceTextures, DestTextures, ".png", ".jpg", ".jpeg", ".tga", ".exr");
            guidMap = MergeGuidMaps(guidMap, CopyTree(FidelityMaterials, DestMaterials + "/Fidelity", ".mat"));
            CopyTree(FidelityTextures, DestTextures + "/Fidelity", ".png", ".jpg", ".jpeg", ".tga", ".exr");
        }

        RemapFbxExternalMaterials(guidMap ?? new Dictionary<string, string>(), log);

        var allMats = AssetDatabase.FindAssets("t:Material", new[] { DestMaterials, LibraryPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .ToList();

        int converted = 0;
        int fixedDonors = 0;
        var missingAlbedo = new List<string>();
        var magentaRisk = new List<string>();

        foreach (var path in allMats)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                continue;

            var name = mat.name;
            if (TextureDonorByMaterial.TryGetValue(name, out var donorName) ||
                (ProceduralNames.Contains(name) && TextureDonorByMaterial.TryGetValue(name, out donorName)))
            {
                if (TryCopyMapsFromDonor(mat, donorName))
                    fixedDonors++;
            }

            if (IsPaintableWallMaterial(name))
                log.AppendLine($"- **Pintable (material):** `{path}` — usar `_BaseColor` / `_Color`");

            if (!HasAlbedoMap(mat))
                missingAlbedo.Add($"{name} ({path})");

            if (ConvertToActivePipeline(mat, pipeline))
                converted++;

            if (UsesErrorShader(mat))
                magentaRisk.Add(path);
        }

        foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Vivienda" })
                     .Select(AssetDatabase.GUIDToAssetPath))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                continue;
            if (ConvertToActivePipeline(mat, pipeline))
                converted++;
            if (UsesErrorShader(mat))
                magentaRisk.Add(path);
        }

        BuildLibrary(log);
        FixTextureImportSettings(DestTextures);
        FixTextureImportSettings(DestTextures + "/Fidelity");

        WriteBakeReport(missingAlbedo);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Vivienda");
        if (root != null)
            CaptureViews(root, log);
        else
            log.AppendLine("- WARN: sin raíz `Vivienda` para capturas.");

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        magentaRisk.Clear();
        foreach (var path in AssetDatabase.FindAssets("t:Material", new[] { DestMaterials, LibraryPath, "Assets/Vivienda" })
                     .Select(AssetDatabase.GUIDToAssetPath))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && UsesErrorShader(mat))
                magentaRisk.Add(path);
        }

        log.AppendLine();
        log.AppendLine("## Resumen técnico");
        log.AppendLine($"- Pipeline detectado: **{pipeline}**");
        log.AppendLine($"- Materiales copiados/remapeados FBX: **{guidMap.Count}** entradas en mapa GUID");
        log.AppendLine($"- Materiales convertidos de shader: **{converted}**");
        log.AppendLine($"- Materiales con texturas prestadas: **{fixedDonors}**");
        log.AppendLine($"- Materiales sin albedo (solo color o vacío): **{missingAlbedo.Count}**");
        log.AppendLine($"- Shaders error (magenta): **{magentaRisk.Count}**");
        if (magentaRisk.Count > 0)
        {
            log.AppendLine();
            log.AppendLine("### Magenta / shader roto");
            foreach (var p in magentaRisk)
                log.AppendLine($"- `{p}`");
        }

        log.AppendLine();
        log.AppendLine("## Nota lightmaps");
        log.AppendLine("- `Vivienda.fbx` importa con `generateSecondaryUV: 0`. Para bake de luz, habilitar **Generate Lightmap UVs** en el mesh import o unwrap en DCC.");

        AppendRunLog(log);
        AppendFase1Resumen(pipeline, converted, missingAlbedo.Count, magentaRisk.Count);
        Debug.Log($"[Agente2] Pipeline completado. Log: {LogPath}");
        return true;
        }
        catch (Exception ex)
        {
            log.AppendLine();
            log.AppendLine("## ERROR");
            log.AppendLine(ex.ToString());
            AppendRunLog(log);
            Debug.LogError("[Agente2] " + ex);
            return false;
        }
        finally
        {
            ReleaseEditorLock();
        }
    }

    static bool UsesErrorShader(Material mat) =>
        mat.shader != null && mat.shader.name.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0;

    static void AppendRunLog(StringBuilder log)
    {
        var body = log.ToString();
        if (File.Exists(Abs(LogPath)))
            File.AppendAllText(Abs(LogPath), Environment.NewLine + Environment.NewLine + "---" + Environment.NewLine + Environment.NewLine + body);
        else
            File.WriteAllText(Abs(LogPath), body);
    }

    static string Abs(string assetPath) =>
        Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length)).Replace("\\", "/");

    static bool TryAcquireEditorLock(StringBuilder log)
    {
        var lockPath = Abs(EditorLockPath);
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        if (File.Exists(lockPath))
        {
            var text = File.ReadAllText(lockPath);
            var owner = text.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
            var inactiveMin = (DateTime.UtcNow - File.GetLastWriteTimeUtc(lockPath)).TotalMinutes;

            if (owner == AgentName)
            {
                File.WriteAllText(lockPath, $"{AgentName}\n{DateTime.UtcNow:O}");
                return true;
            }

            if (inactiveMin < LockMaxAgeMinutes)
            {
                log.AppendLine($"- Lock editor: ocupado ({text.Trim()}); sin actividad {inactiveMin:F1} min (< {LockMaxAgeMinutes} min).");
                return false;
            }

            log.AppendLine($"- Lock editor inactivo {inactiveMin:F1} min; tomando lock.");
        }

        File.WriteAllText(lockPath, $"{AgentName}\n{DateTime.UtcNow:O}");
        log.AppendLine($"- Lock editor adquirido ({AgentName}).");
        return true;
    }

    static void ReleaseEditorLock()
    {
        var lockPath = Abs(EditorLockPath);
        if (!File.Exists(lockPath))
            return;
        var owner = File.ReadAllText(lockPath).Split('\n').FirstOrDefault()?.Trim();
        if (owner == AgentName)
            File.Delete(lockPath);
    }

    static Dictionary<string, string> MergeGuidMaps(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        a ??= new Dictionary<string, string>();
        if (b == null)
            return a;

        foreach (var kv in b)
        {
            if (string.IsNullOrEmpty(kv.Key) || string.IsNullOrEmpty(kv.Value))
                continue;
            a[kv.Key] = kv.Value;
        }

        return a;
    }

    static Dictionary<string, string> BuildGuidMapFromExisting(string sourceFolder, string destFolder)
    {
        var map = new Dictionary<string, string>();
        if (!AssetDatabase.IsValidFolder(sourceFolder) || !AssetDatabase.IsValidFolder(destFolder))
            return map;

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { sourceFolder }))
        {
            var src = AssetDatabase.GUIDToAssetPath(guid);
            var rel = Path.GetFileName(src);
            var dst = destFolder + "/" + rel;
            if (!File.Exists(Abs(dst)))
                continue;
            var dstGuid = AssetDatabase.AssetPathToGUID(dst);
            if (!string.IsNullOrEmpty(guid) && !string.IsNullOrEmpty(dstGuid))
                map[guid] = dstGuid;
        }

        return map;
    }

    static void CaptureViews(GameObject vivienda, StringBuilder log)
    {
        var captures = new (string file, Vector3 pos, Vector3 lookAt)[]
        {
            ("exterior_fachada.png", new Vector3(-12f, 4f, 18f), vivienda.transform.position + new Vector3(0f, 2f, 0f)),
            ("apt1_sala.png", new Vector3(-2f, 1.6f, 4f), new Vector3(-1.2f, 1.2f, 1.5f)),
        };

        foreach (var cap in captures)
        {
            var path = Path.Combine(CapturesDir, cap.file);
            if (RenderCapture(cap.pos, cap.lookAt, path))
                log.AppendLine($"- Captura: `{path}`");
            else
                log.AppendLine($"- WARN captura fallida: `{path}`");
        }
    }

    static bool RenderCapture(Vector3 camPos, Vector3 lookAt, string assetPath)
    {
        var go = new GameObject("Agente2_CaptureCam");
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            go.transform.position = camPos;
            go.transform.LookAt(lookAt);

            const int w = 1280;
            const int h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            var bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);

            var full = Abs(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, bytes);
            AssetDatabase.ImportAsset(assetPath);
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static void EnsureFolders()
    {
        foreach (var dir in new[]
                 {
                     "Assets/_Project", "Assets/_Project/Art", "Assets/_Project/Art/Materials",
                     "Assets/_Project/Art/Textures", DestMaterials, DestTextures, LibraryPath,
                     DeprecatedPath, "Assets/_Project/Docs/Logs", "Assets/_Project/Docs/Reports",
                     SettingsPath, "Assets/_Project/Scripts", "Assets/_Project/Editor"
                 })
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parent = Path.GetDirectoryName(dir)?.Replace("\\", "/");
                var leaf = Path.GetFileName(dir);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                    AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }

    static string DetectPipeline(StringBuilder log)
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null)
            rp = GraphicsSettings.defaultRenderPipeline;

        if (rp == null)
        {
            log.AppendLine("## Pipeline");
            log.AppendLine("- **Actual:** Built-in (Forward / Standard)");
            log.AppendLine("- **Objetivo:** URP (paquete añadido al manifest; asignar asset en Graphics Settings si aún no está activo).");
            return "Built-in";
        }

        var name = rp.GetType().Name;
        log.AppendLine("## Pipeline");
        log.AppendLine($"- **Actual:** {name} (`{AssetDatabase.GetAssetPath(rp)}`)");
        return name.Contains("Universal") ? "URP" : name;
    }

    static void EnsureUrPAssets(StringBuilder log)
    {
        var assetPath = SettingsPath + "/URP_Vivienda.asset";
        var existing = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assetPath);
        if (existing == null)
        {
            var urpType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            if (urpType == null)
            {
                log.AppendLine("- URP package aún no compilado; conversión usará Standard hasta el próximo domain reload.");
                return;
            }

            var instance = ScriptableObject.CreateInstance(urpType) as RenderPipelineAsset;
            AssetDatabase.CreateAsset(instance, assetPath);
            existing = instance;
            log.AppendLine($"- Creado asset URP: `{assetPath}`");
        }

        EnsureUrPRendererAssigned(assetPath, existing, log);

        if (GraphicsSettings.defaultRenderPipeline != existing)
        {
            GraphicsSettings.defaultRenderPipeline = existing;
            log.AppendLine("- URP asignado en Graphics Settings (defaultRenderPipeline).");
        }

        if (QualitySettings.renderPipeline != existing)
        {
            QualitySettings.renderPipeline = existing;
            log.AppendLine("- URP asignado en Quality Settings.");
        }
    }

    static void EnsureUrPRendererAssigned(string urpAssetPath, RenderPipelineAsset urpAsset, StringBuilder log)
    {
        if (urpAsset is not UniversalRenderPipelineAsset urp)
            return;

        var rendererPath = SettingsPath + "/URP_Vivienda_ForwardRenderer.asset";
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name = "URP_Vivienda_ForwardRenderer";
            AssetDatabase.CreateAsset(renderer, rendererPath);
            log.AppendLine($"- Creado Forward Renderer: `{rendererPath}`");
        }

        var list = urp.rendererDataList;
        if (list == null || list.Length == 0 || list[0] == null)
        {
            var so = new SerializedObject(urp);
            var rendererList = so.FindProperty("m_RendererDataList");
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
            log.AppendLine("- Forward Renderer enlazado al asset URP.");
        }
    }

    static Dictionary<string, string> CopyTree(string sourceFolder, string destFolder, params string[] extensions)
    {
        var map = new Dictionary<string, string>();
        if (!AssetDatabase.IsValidFolder(sourceFolder))
            return map;

        var guids = AssetDatabase.FindAssets("", new[] { sourceFolder });
        foreach (var guid in guids)
        {
            var src = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(src))
                continue;
            if (extensions.All(e => !src.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                continue;

            var rel = src.Substring(sourceFolder.Length).TrimStart('/');
            var dst = destFolder + "/" + rel;
            var dstDir = Path.GetDirectoryName(dst)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(dstDir) && !AssetDatabase.IsValidFolder(dstDir))
            {
                Directory.CreateDirectory(Path.GetFullPath(dstDir));
                AssetDatabase.Refresh();
            }

            if (File.Exists(dst))
            {
                var oldGuid = AssetDatabase.AssetPathToGUID(src);
                var existingGuid = AssetDatabase.AssetPathToGUID(dst);
                if (!string.IsNullOrEmpty(oldGuid) && !string.IsNullOrEmpty(existingGuid))
                    map[oldGuid] = existingGuid;
                continue;
            }

            if (!AssetDatabase.CopyAsset(src, dst))
                continue;

            var srcGuid = AssetDatabase.AssetPathToGUID(src);
            var newGuid = AssetDatabase.AssetPathToGUID(dst);
            if (!string.IsNullOrEmpty(srcGuid) && !string.IsNullOrEmpty(newGuid))
                map[srcGuid] = newGuid;
        }

        return map;
    }

    static void RemapFbxExternalMaterials(Dictionary<string, string> guidMap, StringBuilder log)
    {
        try
        {
            log ??= new StringBuilder();
            if (guidMap == null || guidMap.Count == 0)
                return;
            if (string.IsNullOrEmpty(FbxPath) || !File.Exists(Abs(FbxPath)))
            {
                log.AppendLine($"- WARN: FBX no encontrado en `{FbxPath}`; remap omitido.");
                return;
            }

            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                log.AppendLine($"- WARN: sin ModelImporter para `{FbxPath}`; remap omitido.");
                return;
            }

            var so = new SerializedObject(importer);
            if (so == null || !so.targetObject)
            {
                log.AppendLine("- WARN: SerializedObject del FBX inválido; remap omitido.");
                return;
            }

            var externals = so.FindProperty("m_ExternalObjects") ?? so.FindProperty("externalObjects");
            if (externals == null || !externals.isArray)
            {
                log.AppendLine("- WARN: FBX importer sin `externalObjects`; remap omitido.");
                return;
            }

            var changed = 0;
            for (var i = 0; i < externals.arraySize; i++)
            {
                var entry = externals.GetArrayElementAtIndex(i);
                if (entry == null)
                    continue;

                var second = entry.FindPropertyRelative("second");
                if (second == null)
                    continue;

                var guidProp = second.FindPropertyRelative("guid");
                if (guidProp == null)
                    continue;

                var oldGuid = guidProp.stringValue;
                if (string.IsNullOrEmpty(oldGuid) || !guidMap.TryGetValue(oldGuid, out var newGuid) ||
                    string.IsNullOrEmpty(newGuid))
                    continue;

                guidProp.stringValue = newGuid;
                changed++;
            }

            if (changed > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
                log.AppendLine($"- FBX `{FbxPath}`: **{changed}** materiales externos apuntan a `Assets/_Project/Art/Materials/Vivienda`.");
            }
        }
        catch (Exception ex)
        {
            log.AppendLine($"- WARN: remap FBX omitido ({ex.GetType().Name}: {ex.Message}).");
        }
    }

    static bool TryCopyMapsFromDonor(Material target, string donorMaterialName)
    {
        var donorGuids = AssetDatabase.FindAssets($"{donorMaterialName} t:Material", new[] { DestMaterials, SourceMaterials });
        if (donorGuids.Length == 0)
            return false;
        var donor = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(donorGuids[0]));
        if (donor == null)
            return false;

        CopyIfPresent(donor, target, "_MainTex", "_MainTex");
        CopyIfPresent(donor, target, "_MainTex", "_BaseMap");
        CopyIfPresent(donor, target, "_BumpMap", "_BumpMap");
        CopyIfPresent(donor, target, "_MetallicGlossMap", "_MetallicGlossMap");
        CopyIfPresent(donor, target, "_MetallicGlossMap", "_MetallicSpecGlossMap");
        if (target.HasProperty("_Glossiness") && donor.HasProperty("_Glossiness"))
            target.SetFloat("_Glossiness", donor.GetFloat("_Glossiness"));
        if (target.HasProperty("_Smoothness") && donor.HasProperty("_Glossiness"))
            target.SetFloat("_Smoothness", donor.GetFloat("_Glossiness"));
        EditorUtility.SetDirty(target);
        return true;
    }

    static void CopyIfPresent(Material from, Material to, string fromProp, string toProp)
    {
        if (!from.HasProperty(fromProp) || !to.HasProperty(toProp))
            return;
        var tex = from.GetTexture(fromProp);
        if (tex != null)
            to.SetTexture(toProp, tex);
    }

    static bool HasAlbedoMap(Material mat)
    {
        if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
            return true;
        if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
            return true;
        return false;
    }

    static bool IsPaintableWallMaterial(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("wall") || n.Contains("plaster") || n.Contains("blanco") ||
               n.Contains("microcemento") || n.Contains("pintur") || n.Contains("paint");
    }

    static bool ConvertToActivePipeline(Material mat, string pipeline)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        var standard = Shader.Find("Standard");
        var shaderName = mat.shader != null ? mat.shader.name : string.Empty;
        if (pipeline == "URP" && urpLit != null && mat.shader != urpLit)
        {
            if (UsesErrorShader(mat) || shaderName.IndexOf("Standard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                shaderName.IndexOf("Legacy", StringComparison.OrdinalIgnoreCase) >= 0)
                return UpgradeStandardToUrPLit(mat, urpLit);
        }

        if (pipeline == "Built-in" && standard != null && UsesErrorShader(mat))
        {
            mat.shader = standard;
            EditorUtility.SetDirty(mat);
            return true;
        }

        return false;
    }

    static bool UpgradeStandardToUrPLit(Material mat, Shader urpLit)
    {
        var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        var main = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        var bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
        var metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
        var smooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
        var mode = mat.HasProperty("_Mode") ? mat.GetFloat("_Mode") : 0f;
        var zWrite = mat.HasProperty("_ZWrite") ? mat.GetFloat("_ZWrite") : 1f;

        mat.shader = urpLit;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (main != null && mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", main);
        if (bump != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", bump);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smooth);

        var isGlass = mat.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                      mat.name.IndexOf("vidrio", StringComparison.OrdinalIgnoreCase) >= 0;
        if (isGlass || mode >= 2f || zWrite < 0.5f)
            ConfigureTransparentGlass(mat, smooth);
        else if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 0f);

        EditorUtility.SetDirty(mat);
        return true;
    }

    static void ConfigureTransparentGlass(Material mat, float smoothness)
    {
        if (!mat.HasProperty("_Surface"))
            return;
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", Mathf.Max(smoothness, 0.92f));
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    static void BuildLibrary(StringBuilder log)
    {
        log.AppendLine("## Biblioteca `_Project/Art/Materials/Library`");
        var defs = new (string file, string donor, Color? tint, bool glass)[]
        {
            ("Muro_Pintura.mat", "Beige Painted Plaster Wall", new Color(0.94f, 0.93f, 0.9f), false),
            ("Piso_Ceramica.mat", "Gray Ceramic Tile", null, false),
            ("Piso_Madera.mat", "Oak Wood", null, false),
            ("Techo.mat", "Acoustic Ceiling Tile", null, false),
            ("Vidrio.mat", "Glass", null, true),
            ("Metal_Manija.mat", "Black steel", null, false),
            ("Madera_Puerta.mat", "Walnut wood", null, false),
            ("Sanitario.mat", "White Ceramic Tiles", null, false),
        };

        foreach (var def in defs)
        {
            var path = $"{LibraryPath}/{def.file}";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var donor = FindMaterial(def.donor);
                mat = donor != null ? new Material(donor) : new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
                mat.name = Path.GetFileNameWithoutExtension(def.file);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (def.tint.HasValue && mat.HasProperty("_Color"))
                mat.SetColor("_Color", def.tint.Value);
            if (def.tint.HasValue && mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", def.tint.Value);
            if (def.glass)
                ConfigureTransparentGlass(mat, 0.95f);
            EditorUtility.SetDirty(mat);
            log.AppendLine($"- `{path}` ← donor `{def.donor}`");
        }
    }

    static Material FindMaterial(string name)
    {
        var guids = AssetDatabase.FindAssets($"{name} t:Material", new[] { DestMaterials, SourceMaterials, FidelityMaterials });
        if (guids.Length == 0)
            return null;
        return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void FixTextureImportSettings(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var lower = path.ToLowerInvariant();
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            var isNormal = lower.Contains("normal") || lower.Contains("_nor") || lower.Contains("nor_gl") || lower.Contains("normalgl");
            var isData = lower.Contains("rough") || lower.Contains("metal") || lower.Contains("spec") ||
                         lower.Contains("height") || lower.Contains("ao") || lower.Contains("occlusion");

            if (isNormal)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
            }
            else if (isData)
            {
                importer.sRGBTexture = false;
            }
            else
            {
                importer.sRGBTexture = true;
            }

            importer.SaveAndReimport();
        }
    }

    static void WriteBakeReport(List<string> missingAlbedo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Materiales que requieren bake / re-export desde Blender");
        sb.AppendLine();
        sb.AppendLine("Los nodos procedurales de Blender **no** viajan en FBX estándar. Opciones:");
        sb.AppendLine();
        sb.AppendLine("## Export recomendado (FBX)");
        sb.AppendLine("- **Path Mode:** Copy + Embed");
        sb.AppendLine("- **Apply Transform**, **Apply Modifiers**");
        sb.AppendLine("- **Forward:** -Z Forward, **Up:** Y Up, **Scale:** 1.0");
        sb.AppendLine("- Bakear texturas PBR (Base Color, Normal, Roughness, Metallic) antes de export, o usar **glTF 2.0** con texturas embebidas.");
        sb.AppendLine();
        sb.AppendLine("## Materiales procedurales detectados en Unity");
        foreach (var p in ProceduralNames)
            sb.AppendLine($"- `{p}` — sustituto temporal desde material donor en pipeline Agente 2.");
        sb.AppendLine();
        sb.AppendLine("## Otros materiales sin mapa albedo en import");
        foreach (var line in missingAlbedo.Distinct().OrderBy(x => x))
            sb.AppendLine($"- {line}");
        sb.AppendLine();
        sb.AppendLine("## Fuente Blender");
        sb.AppendLine("- `C:\\Users\\solan\\Downloads\\Untitled.blend`");
        File.WriteAllText(BakeReport, sb.ToString());
    }

    static void AppendFase1Resumen(string pipeline, int converted, int missingCount, int magentaCount)
    {
        const string path = "Assets/_Project/Docs/Reports/fase1_resumen.md";
        var section = new StringBuilder();
        section.AppendLine();
        section.AppendLine("## Agente 2 — Materiales y texturas");
        section.AppendLine($"- **Fecha:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        section.AppendLine($"- **Pipeline:** {pipeline} (objetivo URP; ver `Assets/_Project/Settings/URP_Vivienda.asset` cuando el paquete URP termine de importar).");
        section.AppendLine($"- **Copias:** `Assets/_Project/Art/Materials/Vivienda`, `Assets/_Project/Art/Textures/Vivienda` (originales intactos en `Assets/Vivienda/`).");
        section.AppendLine($"- **Biblioteca:** `Assets/_Project/Art/Materials/Library/` (Muro_Pintura, Piso_Ceramica, Piso_Madera, Techo, Vidrio, Metal_Manija, Madera_Puerta, Sanitario).");
        section.AppendLine($"- **Conversión shader:** {converted} materiales.");
        section.AppendLine($"- **Sin albedo:** {missingCount}; ver `materials_needing_bake.md`.");
        section.AppendLine($"- **Magenta pendiente:** {magentaCount} (debe ser 0 tras URP activo).");
        section.AppendLine($"- **Log detallado:** `{LogPath}`");
        section.AppendLine("- **Lightmaps:** habilitar UV2 en import del FBX antes de bake (no ejecutado en fase 1).");

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path);
            if (existing.Contains("## Agente 2 — Materiales"))
            {
                existing = Regex.Replace(existing, "## Agente 2 — Materiales[\\s\\S]*?(?=\\n## |\\z)", section.ToString().TrimEnd() + Environment.NewLine);
                File.WriteAllText(path, existing);
            }
            else
                File.AppendAllText(path, section.ToString());
        }
        else
            File.WriteAllText(path, "# Fase 1 — Resumen\n" + section);
    }
}
#endif
