using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;
using ProyectoVR;

public static class NativeXrValidation
{
    [MenuItem("Vivienda/XR/Compilar scripts Android")]
    public static void CompileAndroid()
    {
        var settings = new UnityEditor.Build.Player.ScriptCompilationSettings
        {
            target = BuildTarget.Android,
            group = BuildTargetGroup.Android,
            options = UnityEditor.Build.Player.ScriptCompilationOptions.None
        };
        var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, "Temp/NativeXR-AndroidScripts");
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/NativeXR-AndroidScripts.txt", "Android player script assemblies=" + result.assemblies.Count + "\nEsta comprobacion no genera un APK ni sustituye la prueba en Quest.\n");
        if (result.assemblies.Count == 0) throw new System.InvalidOperationException("La compilacion Android no genero assemblies.");
        Debug.Log("Scripts Android compilados: " + result.assemblies.Count);
    }

    [MenuItem("Vivienda/XR/Validar proyecto")]
    public static void Validate()
    {
        var report = new StringBuilder("Validacion de arquitectura XR y configuracion OpenXR\n");
        var rig = Object.FindAnyObjectByType<XROrigin>();
        report.AppendLine("Scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        report.AppendLine("Mediator=" + rig.GetComponentsInChildren<LocomotionMediator>().Count(m => m.enabled));
        report.AppendLine("BodyTransformer=" + rig.GetComponentsInChildren<XRBodyTransformer>().Count(m => m.enabled));
        report.AppendLine("NativeContinuousMove=" + rig.GetComponentsInChildren<ContinuousMoveProvider>().Count(m => m.enabled && m.GetType() == typeof(ContinuousMoveProvider)));
        report.AppendLine("Gravity=" + rig.GetComponentsInChildren<GravityProvider>().Count(m => m.enabled));
        report.AppendLine("NativeTeleportation=" + rig.GetComponentsInChildren<TeleportationProvider>().Count(m => m.enabled));
        report.AppendLine("LegacyMoveEnabled=" + rig.GetComponentsInChildren<SimpleCharacterLocomotion>().Count(m => m.enabled));
        foreach (var target in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(target);
            bool loader = settings != null && settings.Manager != null && settings.Manager.activeLoaders.Any(l => l is OpenXRLoader);
            report.AppendLine(target + " OpenXRLoader=" + loader);
            report.AppendLine(target + " InitializeXR=" + (settings != null && settings.InitManagerOnStart));
            var issues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, target);
            foreach (var issue in issues) report.AppendLine((issue.error ? "ERROR " : "WARNING ") + target + ": " + issue.message);
            report.AppendLine(target + " errors=" + issues.Count(i => i.error) + " warnings=" + issues.Count(i => !i.error));
        }
        report.AppendLine("Android=" + PlayerSettings.Android.targetArchitectures + " minSDK=" + PlayerSettings.Android.minSdkVersion + " backend=" + PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android));
        report.AppendLine("La validacion del editor no acredita rendimiento ni comodidad en un visor fisico.");
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/NativeXR-Configuration.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
