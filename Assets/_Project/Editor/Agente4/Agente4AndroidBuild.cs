using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Vivienda.EditorTools;

public static class Agente4AndroidBuild
{
    const string ApkPath = "Builds/ViviendaUnity.apk";

    [MenuItem("Vivienda/Agente 4/Build Android APK (Quest)")]
    public static void BuildApkMenu() => BuildApk();

    public static void BuildApk()
    {
        if (!Agente4EditorLock.TryAcquire("Agente4-Build", out var lockReason))
        {
            Debug.LogWarning("[Agente4] Build cancelado: " + lockReason);
            WriteFase4Blocker(lockReason);
            return;
        }

        try
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                WriteFase4Blocker("BuildTarget Android no soportado en esta instalación Unity.");
                return;
            }

            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs/Editor.log");
            if (File.Exists(logPath))
            {
                var tail = File.ReadAllLines(logPath).TakeLast(400);
                if (tail.Any(l => l.Contains("Native extension for Android target not found")))
                {
                    WriteFase4Blocker(
                        "Unity reporta `Native extension for Android target not found` — falta Android SDK/NDK/OpenJDK en Hub o módulo Android Player incompleto.");
                    return;
                }
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                scenes = new[] { "Assets/Scenes/Vivienda_Prueba.unity" };

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            if (!Directory.Exists("Builds"))
                Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log("[Agente4] APK OK: " + ApkPath);
            else
                WriteFase4Blocker($"Build falló: {summary.result} — {summary.totalErrors} errores.");
        }
        finally
        {
            Agente4EditorLock.Release();
        }
    }

    static void WriteFase4Blocker(string message)
    {
        var path = Path.Combine(Application.dataPath, "_Project/Docs/Reports/fase4_qa.md");
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var block = $"\n## Build Android — {System.DateTime.UtcNow:u}\n- **BLOQUEADO:** {message}\n";
        File.AppendAllText(path, block);
        AssetDatabase.Refresh();
        Debug.LogWarning("[Agente4] " + message);
    }
}
