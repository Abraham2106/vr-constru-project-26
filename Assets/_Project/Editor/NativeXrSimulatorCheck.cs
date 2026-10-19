using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;
using ProyectoVR;
using ProyectoVR.UI;

[InitializeOnLoad]
public static class NativeXrSimulatorCheck
{
    const string Flag = "Vivienda.NativeSimulator.Check";
    static float ready;
    static NativeXrSimulatorCheck() => EditorApplication.playModeStateChanged += State;

    [MenuItem("Vivienda/XR/Probar simulador oficial")]
    public static void Begin()
    {
        NativeXrProjectSettings.Simulator();
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }

    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            ready = Time.realtimeSinceStartup + 3;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Flag, false);
            NativeXrProjectSettings.Desktop();
        }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.realtimeSinceStartup < ready) return;
        var report = new StringBuilder();
        int failures = 0;
        void Check(bool value, string text) { report.AppendLine((value ? "PASS " : "FAIL ") + text); if (!value) failures++; }
        var origin = Object.FindAnyObjectByType<XROrigin>();
        Check(XRInteractionSimulator.instance != null && XRInteractionSimulator.instance.isActiveAndEnabled, "XR Interaction Simulator oficial activo");
        Check(!DesktopPreview.IsActive, "Vista de escritorio desactivada en modo XR");
        Check(origin.Camera.GetComponent<TrackedPoseDriver>().enabled, "Seguimiento mediante Tracked Pose Driver");
        Check(InputSystem.devices.Any(d => d.layout.Contains("HMD")), "HMD del Input System disponible");
        Check(InputSystem.devices.Any(d => d.usages.Any(u => u.ToString() == "LeftHand")), "Dispositivo izquierdo disponible");
        Check(InputSystem.devices.Any(d => d.usages.Any(u => u.ToString() == "RightHand")), "Dispositivo derecho disponible");
        int active = origin.GetComponentsInChildren<NearFarInteractor>().Count(i => i.isActiveAndEnabled);
        Check(active == 2, "Interactores de ambos mandos activos=" + active);
        float height = origin.Camera.transform.position.y - origin.transform.position.y;
        Check(height > 1.2f && height < 2.1f, "Altura de ojos simulada=" + height.ToString("F2") + "m");
        Check(VrWorldMenuController.Instance.IsVisible, "Menu XR visible");
        report.AppendLine("Fallos=" + failures);
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/NativeXR-Simulator.txt", report.ToString());
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }
}
