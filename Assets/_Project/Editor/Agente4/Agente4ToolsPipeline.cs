using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vivienda.EditorTools;
using Vivienda.Tools;
using Object = UnityEngine.Object;

namespace Vivienda.EditorTools
{
public static class Agente4ToolsPipeline
{
    const string PalettePath = "Assets/_Project/Prefabs/Tools/ArchitecturalPaintPalette.asset";
    const string HostPrefabPath = "Assets/_Project/Prefabs/Tools/ViviendaToolHost.prefab";
    const string LogPath = "Assets/_Project/Docs/Logs/agente4_qa.md";
    const string ScenePath = "Assets/Scenes/Vivienda_Prueba.unity";
    const string PendingFlag = "Assets/_Project/Docs/Logs/agente4_autorun.flag";
    const string StatusPath = "Assets/_Project/Docs/Logs/agente4_last_run.txt";
    const string AgentName = "Agente4-Herramientas";

    static bool playTestQueued;
    static int autoRunFrames;

    [InitializeOnLoadMethod]
    static void RegisterPendingRunner() => QueueAutoRun();

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded() => QueueAutoRun();

    static void QueueAutoRun()
    {
        if (!File.Exists(AbsProject(PendingFlag)) && !File.Exists(AbsProject(LegacyPendingFlag)))
            return;
        EditorApplication.update -= RunAutoRunAfterDelay;
        autoRunFrames = 0;
        EditorApplication.update += RunAutoRunAfterDelay;
    }

    const string LegacyPendingFlag = "Assets/_Project/.agente4_pending";

    static void RunAutoRunAfterDelay()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            autoRunFrames = 0;
            return;
        }

        if (++autoRunFrames < 45)
            return;

        EditorApplication.update -= RunAutoRunAfterDelay;
        if (!File.Exists(AbsProject(PendingFlag)) && !File.Exists(AbsProject(LegacyPendingFlag)))
            return;
        TryRunFromPending();
    }

    [MenuItem("Vivienda/Agente 4/Ejecutar pipeline (lock + escena)")]
    public static void RunPipelineFromMenu()
    {
        if (!Agente4EditorLock.TryAcquire(AgentName, out var reason))
        {
            Debug.LogWarning("[Agente4] Pipeline cancelado: " + reason);
            return;
        }

        try
        {
            RunPipelineInternal();
        }
        finally
        {
            Agente4EditorLock.Release();
        }
    }

    static void TryRunFromPending()
    {
        if (!File.Exists(AbsProject(PendingFlag)))
            return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRunFromPending;
            return;
        }

        if (!Agente4EditorLock.TryAcquire(AgentName, out _))
            return;

        try
        {
            RunPipelineInternal();
        }
        finally
        {
            Agente4EditorLock.Release();
            if (File.Exists(AbsProject(PendingFlag)))
                File.Delete(AbsProject(PendingFlag));
            if (File.Exists(AbsProject(LegacyPendingFlag)))
                File.Delete(AbsProject(LegacyPendingFlag));
        }
    }

    static void RunPipelineInternal()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"utc={DateTime.UtcNow:O}");
        sb.AppendLine($"scene={ScenePath}");
        EnsureSceneOpen(sb);

        SetupPrefabs();
        sb.AppendLine("setup_prefabs=ok");

        var hadHostBefore = Object.FindFirstObjectByType<ViviendaToolHost>() != null;
        InstallInActiveScene();
        var host = Object.FindFirstObjectByType<ViviendaToolHost>();
        sb.AppendLine($"toolhost_in_scene={(host != null ? "yes" : "no")}");
        sb.AppendLine($"toolhost_preexisting={(hadHostBefore ? "yes" : "no")}");

        var paintableBefore = Object.FindObjectsByType<PaintableWall>(FindObjectsSortMode.None).Length;
        Agente4PaintableWallTagger.TagPaintableWalls();
        var paintableAfter = Object.FindObjectsByType<PaintableWall>(FindObjectsSortMode.None).Length;
        sb.AppendLine($"paintable_walls={paintableAfter}");
        sb.AppendLine($"paintable_added={paintableAfter - paintableBefore}");

        EditorSceneManager.SaveOpenScenes();
        sb.AppendLine("scene_saved=yes");

        if (!playTestQueued && host != null && !EditorApplication.isPlaying)
        {
            playTestQueued = true;
            sb.AppendLine($"play_test=scheduled errors_before={CountToolRelatedLogErrors()}");
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        else
            sb.AppendLine("play_test=skipped");

        File.WriteAllText(AbsProject(StatusPath), sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log("[Agente4] Pipeline completado. Ver " + StatusPath);
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
        else if (state == PlayModeStateChange.EnteredEditMode && playTestQueued)
        {
            playTestQueued = false;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AppendPlayTestResult();
        }
    }

    static void AppendPlayTestResult()
    {
        var path = AbsProject(StatusPath);
        if (!File.Exists(path))
            return;
        var block = File.ReadAllText(path);
        if (!block.Contains("play_test_result="))
            File.WriteAllText(path, block.TrimEnd() + $"\nplay_test_result=done errors_after={CountToolRelatedLogErrors()}\n");
    }

    static int CountToolRelatedLogErrors()
    {
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs/Editor.log");
        if (!File.Exists(logPath))
            return -1;
        return File.ReadAllLines(logPath).Count(l =>
            l.Contains("error CS") ||
            (l.Contains("Error") && (l.Contains("MeasureTool") || l.Contains("WallPaintTool") ||
                                    l.Contains("ViviendaToolHost") || l.Contains("[Agente4]"))));
    }

    static void EnsureSceneOpen(StringBuilder sb)
    {
        var active = SceneManager.GetActiveScene();
        if (active.path == ScenePath)
        {
            sb.AppendLine("scene_open=already");
            return;
        }

        if (!File.Exists(AbsProject(ScenePath)))
        {
            sb.AppendLine("scene_open=missing");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        sb.AppendLine("scene_open=loaded");
    }

    static string AbsProject(string assetsRelative) =>
        Path.Combine(Application.dataPath, assetsRelative.Replace("Assets/", "").Replace('/', Path.DirectorySeparatorChar));

    [MenuItem("Vivienda/Agente 4/Setup herramientas (prefabs + paleta)")]
    public static void SetupPrefabsMenu() => SetupPrefabs();

    [MenuItem("Vivienda/Agente 4/Instalar ToolHost en escena activa")]
    public static void InstallInActiveSceneMenu() => InstallInActiveScene();

    [MenuItem("Vivienda/Agente 4/QA — medición puerta (bounds)")]
    public static void DoorMeasureMenu() => WriteDoorWidthReport();

    [MenuItem("Vivienda/Agente 4/QA — escanear consola reciente")]
    public static void ScanConsoleMenu() => ScanEditorLogForErrors();

    public static void SetupPrefabs()
    {
        EnsureFolder("Assets/_Project/Prefabs/Tools");
        EnsureFolder("Assets/_Project/Docs/Reports");

        var palette = AssetDatabase.LoadAssetAtPath<ArchitecturalPaintPalette>(PalettePath);
        if (palette == null)
        {
            palette = ScriptableObject.CreateInstance<ArchitecturalPaintPalette>();
            AssetDatabase.CreateAsset(palette, PalettePath);
        }

        var hostGo = new GameObject("ViviendaToolHost");
        var host = hostGo.AddComponent<ViviendaToolHost>();
        hostGo.AddComponent<ViviendaToolEventBridge>();
        var measure = hostGo.AddComponent<MeasureTool>();
        var paint = hostGo.AddComponent<WallPaintTool>();
        paint.Palette = palette;

        var ray = new GameObject("RightControllerRay");
        ray.transform.SetParent(hostGo.transform, false);
        ray.transform.localPosition = new Vector3(0.15f, 1.4f, 0.35f);

        var soHost = new SerializedObject(host);
        soHost.FindProperty("rightControllerRay").objectReferenceValue = ray.transform;
        soHost.FindProperty("measureTool").objectReferenceValue = measure;
        soHost.FindProperty("wallPaintTool").objectReferenceValue = paint;
        soHost.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(hostGo, HostPrefabPath);
        Object.DestroyImmediate(hostGo);

        AssetDatabase.SaveAssets();
        Debug.Log("[Agente4] Prefabs herramientas listos en " + HostPrefabPath);
    }

    public static void InstallInActiveScene()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(HostPrefabPath) == null)
            SetupPrefabs();

        var existing = Object.FindFirstObjectByType<ViviendaToolHost>();
        if (existing != null)
        {
            Debug.Log("[Agente4] Ya existe ViviendaToolHost en escena.");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HostPrefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return;

        var xrOrigin = GameObject.Find("XR Origin (VR)") ?? GameObject.Find("XR Origin");
        if (xrOrigin != null)
            instance.transform.SetParent(xrOrigin.transform, false);

        var rightController = FindDeep(xrOrigin != null ? xrOrigin.transform : null, "Right Controller") ??
                              FindDeep(xrOrigin != null ? xrOrigin.transform : null, "RightHand Controller");
        if (rightController != null)
        {
            var host = instance.GetComponent<ViviendaToolHost>();
            var so = new SerializedObject(host);
            so.FindProperty("rightControllerRay").objectReferenceValue = rightController;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Agente4] ViviendaToolHost instalado en escena activa.");
    }

    public static void WriteDoorWidthReport()
    {
        var reportPath = "Assets/_Project/Docs/Reports/medicion_puerta.md";
        var sb = new StringBuilder();
        sb.AppendLine("# Medición puerta (bounds escena / FBX)");
        sb.AppendLine();
        sb.AppendLine("| Objeto | Ancho X (m) | Alto Y (m) | Prof. Z (m) |");
        sb.AppendLine("|--------|-------------|------------|-------------|");

        var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(mf => mf.sharedMesh != null && mf.name.ToLowerInvariant().Contains("door-trustile"))
            .Take(12);

        var count = 0;
        foreach (var mf in filters)
        {
            var b = mf.sharedMesh.bounds;
            var s = Vector3.Scale(b.size, mf.transform.lossyScale);
            sb.AppendLine($"| `{mf.name}` | {s.x:F2} | {s.y:F2} | {s.z:F2} |");
            count++;
        }

        if (count == 0)
            sb.AppendLine("| (sin puertas en escena) | — | — | — |");

        sb.AppendLine();
        sb.AppendLine("**Referencia esperada:** ancho ~0,90 m (tolerancia ±0,10 m vs Blender ~0,96 m).");
        sb.AppendLine();
        sb.AppendLine("**Validación MeasureTool:** medir entre jambas en Play Mode; registrar captura en checklist.");

        File.WriteAllText(Path.Combine(Application.dataPath, "_Project/Docs/Reports/medicion_puerta.md"), sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log("[Agente4] Reporte puerta: " + reportPath);
    }

    public static void ScanEditorLogForErrors()
    {
        var logFile = Path.Combine(Directory.GetCurrentDirectory(), "Logs/Editor.log");
        if (!File.Exists(logFile))
        {
            AppendQaLog("- No se encontró Logs/Editor.log");
            return;
        }

        var lines = File.ReadAllLines(logFile);
        var csErrors = lines.Where(l => l.Contains("error CS")).TakeLast(20).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("## Escaneo consola — " + System.DateTime.UtcNow.ToString("u"));
        if (csErrors.Count == 0)
            sb.AppendLine("- Sin `error CS` recientes en Editor.log (últimas ocurrencias).");
        else
        {
            sb.AppendLine("- **Errores CS detectados:**");
            foreach (var e in csErrors)
                sb.AppendLine("  - " + e.Trim());
        }

        AppendQaLog(sb.ToString());
    }

    static void AppendQaLog(string block)
    {
        var path = Path.Combine(Application.dataPath, "_Project/Docs/Logs/agente4_qa.md");
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.AppendAllText(path, block + "\n");
        AssetDatabase.Refresh();
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (var i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
}
