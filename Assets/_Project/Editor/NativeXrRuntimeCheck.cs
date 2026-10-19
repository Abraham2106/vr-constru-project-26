using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;
using ProyectoVR;
using ProyectoVR.Core;
using ProyectoVR.UI;

[InitializeOnLoad]
public static class NativeXrRuntimeCheck
{
    const string Flag = "Vivienda.NativeXR.Check";
    static readonly StringBuilder report = new();
    static XROrigin rig;
    static ContinuousMoveProvider move;
    static TeleportationProvider teleport;
    static XRBodyTransformer transformer;
    static VrWorldMenuController menu;
    static Keyboard keyboard;
    static JourneyNavigationCheck.Routes routes;
    static int phase, failures, errors, applied, routeIndex, corner;
    static float next, routeStarted, startingTimeScale;
    static Vector3 before;
    static NativeXrRuntimeCheck() => EditorApplication.playModeStateChanged += State;

    [MenuItem("Vivienda/XR/Probar locomocion nativa")]
    public static void Begin()
    {
        JourneyNavigationCheck.Check();
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }

    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            report.Clear(); phase = failures = errors = applied = routeIndex = corner = 0;
            next = Time.realtimeSinceStartup + 2;
            startingTimeScale = Time.timeScale;
            Application.logMessageReceived += Log;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
        if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Flag, false);
    }

    static void Log(string text, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception) return;
        errors++;
        if (errors < 4) report.AppendLine(text + "\n" + stack);
    }

    static void Applied(ApplyBodyTransformationsEventArgs args) => applied++;
    static void Check(bool success, string text)
    {
        report.AppendLine((success ? "PASS " : "FAIL ") + text);
        if (!success) failures++;
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/NativeXR-Progress.txt", report.ToString());
    }

    static void Queue(Vector3 position) => teleport.QueueTeleportRequest(new TeleportRequest
    {
        destinationPosition = position,
        destinationRotation = Quaternion.Euler(0, 180, 0),
        matchOrientation = MatchOrientation.TargetUpAndForward,
        requestTime = Time.time
    });

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.realtimeSinceStartup < next) return;
        try
        {
            if (phase == 0)
            {
                rig = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
                move = rig.GetComponentsInChildren<ContinuousMoveProvider>().Single(p => p.enabled && p.GetType() == typeof(ContinuousMoveProvider));
                teleport = rig.GetComponentsInChildren<TeleportationProvider>().Single(p => p.enabled);
                transformer = rig.GetComponentsInChildren<XRBodyTransformer>().Single(p => p.enabled);
                transformer.afterApplyTransformations += Applied;
                menu = VrWorldMenuController.Instance;
                Check(rig.GetComponentsInChildren<LocomotionMediator>().Count(m => m.enabled) == 1, "Un unico Locomotion Mediator activo");
                Check(move.mediator == teleport.mediator && move.mediator.bodyTransformer == transformer, "Movimiento y teletransporte comparten Body Transformer");
                Check(!rig.GetComponentsInChildren<SimpleCharacterLocomotion>().Any(m => m.enabled), "Movimiento anterior inactivo");
                Check(rig.GetComponentInChildren<GravityProvider>().enabled && transformer.constrainedBodyManipulator is CharacterControllerBodyManipulator, "Gravedad y capsula nativas");
                Check(DesktopPreview.IsActive && menu.IsVisible, "Escritorio separado de XR; menu visible");
                keyboard = InputSystem.AddDevice<Keyboard>("Vivienda XR Test Keyboard");
                before = rig.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                phase = 1; next = Time.realtimeSinceStartup + .6f; return;
            }
            if (phase == 1)
            {
                Check(Vector3.ProjectOnPlane(rig.transform.position - before, Vector3.up).magnitude < .03f, "Menu bloquea locomocion por Input Actions");
                menu.SetVisible(false);
                before = rig.transform.position;
                next = Time.realtimeSinceStartup + 1; phase = 2; return;
            }
            if (phase == 2)
            {
                float distance = Vector3.ProjectOnPlane(rig.transform.position - before, Vector3.up).magnitude;
                Check(distance > .8f && distance < 1.9f && applied > 5, "WASD mueve por proveedor y Body Transformer: " + distance.ToString("F2") + "m");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                menu.TeleportPiso2(); next = Time.realtimeSinceStartup + 1; phase = 3; return;
            }
            if (phase == 3)
            {
                Check(rig.transform.position.y > 2.8f && rig.transform.position.y < 3.3f, "Menu usa Teleportation Provider al segundo piso");
                menu.TeleportPiso1(); next = Time.realtimeSinceStartup + 1; phase = 4; return;
            }
            if (phase == 4)
            {
                Check(Mathf.Abs(rig.transform.position.y) < .3f, "Teletransporte nativo al primer piso");
                menu.TeleportExterior(); next = Time.realtimeSinceStartup + 1; phase = 5; return;
            }
            if (phase == 5)
            {
                Check(rig.transform.position.z > 9, "Teletransporte nativo al exterior");
                routes = JsonUtility.FromJson<JourneyNavigationCheck.Routes>(File.ReadAllText("Temp/JourneyRoutes.json"));
                Check(routes.complete == 12, "Rutas estructurales completas: " + routes.complete + "/12");
                foreach (var door in UnityEngine.Object.FindObjectsByType<DoorHinge>()) if (door.enabled) door.Toggle();
                move.leftHandMoveInput.inputSourceMode = XRInputValueReader.InputSourceMode.ManualValue;
                move.leftHandMoveInput.manualValue = Vector2.zero;
                Time.timeScale = 3;
                next = Time.realtimeSinceStartup + .6f; phase = 6; return;
            }
            if (phase == 6)
            {
                if (routeIndex >= routes.routes.Length) { Finish(); return; }
                move.leftHandMoveInput.manualValue = Vector2.zero;
                Queue(routes.spawn);
                corner = 0;
                next = Time.realtimeSinceStartup + .2f; phase = 7; return;
            }
            if (phase == 7)
            {
                routeStarted = Time.realtimeSinceStartup;
                phase = 8;
            }
            var route = routes.routes[routeIndex];
            if (corner >= route.corners.Length)
            {
                var delta = rig.transform.position - route.sampledTarget;
                Check(Vector3.ProjectOnPlane(delta, Vector3.up).magnitude < .22f && Mathf.Abs(delta.y) < .4f, "Recorrido nativo " + route.room);
                move.leftHandMoveInput.manualValue = Vector2.zero;
                routeIndex++; phase = 6; return;
            }
            if (Time.realtimeSinceStartup - routeStarted > 25)
            {
                Check(false, route.room + " bloqueado en esquina " + corner + " posicion=" + rig.transform.position);
                move.leftHandMoveInput.manualValue = Vector2.zero;
                routeIndex++; phase = 6; return;
            }
            var target = route.corners[corner] - rig.transform.position;
            var horizontal = Vector3.ProjectOnPlane(target, Vector3.up);
            if (horizontal.magnitude < .12f && Mathf.Abs(target.y) < .45f) { corner++; return; }
            var forward = Vector3.ProjectOnPlane(rig.Camera.transform.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            float scale = Mathf.Max(.001f, move.moveSpeed * Time.deltaTime);
            move.leftHandMoveInput.manualValue = Vector2.ClampMagnitude(new Vector2(Vector3.Dot(horizontal, right), Vector3.Dot(horizontal, forward)) / scale, 1);
        }
        catch (Exception exception) { Check(false, exception.ToString()); Finish(); }
    }

    static void Finish()
    {
        Check(errors == 0, "Errores durante prueba=" + errors);
        report.AppendLine("Fallos=" + failures);
        report.AppendLine("Prueba automatizada en editor; visor fisico pendiente.");
        File.WriteAllText("Validation/NativeXR-Runtime.txt", report.ToString());
        Cleanup();
        EditorApplication.ExitPlaymode();
    }

    static void Cleanup()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        Time.timeScale = startingTimeScale;
        if (transformer != null) transformer.afterApplyTransformations -= Applied;
        if (move != null) move.leftHandMoveInput.manualValue = Vector2.zero;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
    }
}
