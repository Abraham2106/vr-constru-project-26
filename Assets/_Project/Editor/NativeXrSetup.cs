using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Unity.XR.CoreUtils;
using ProyectoVR;
using ProyectoVR.Core;
using ProyectoVR.UI;
using Vivienda.Tools;

public static class NativeXrSetup
{
    const string Folder = "Assets/_Project/Input";
    static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static InputActionAsset CreateInput()
    {
        JourneyGeometry.EnsureFolder(Folder + "/References");
        string path = Folder + "/Vivienda.inputactions";
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        if (asset != null)
        {
            var map = asset.FindActionMap("Locomotion", true);
            bool changed = false;
            for (int i = 0; i < map.bindings.Count; i++)
            {
                var binding = map.bindings[i];
                if (binding.action == "Move" && binding.groups == "Desktop")
                {
                    binding.groups = "Desktop;Simulator";
                    map.ChangeBinding(i).To(binding);
                    changed = true;
                }
            }
            var menuAction = asset.FindAction("UI/ToggleMenu", true);
            if (!menuAction.bindings.Any(binding => binding.groups == "Simulator"))
            {
                menuAction.AddBinding("<Keyboard>/escape", groups: "Simulator");
                changed = true;
            }
            if (changed)
            {
                File.WriteAllText(path, asset.ToJson());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var locomotion = asset.AddActionMap("Locomotion");
        var move = locomotion.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        move.AddBinding("<XRController>{LeftHand}/primary2DAxis", groups: "XR");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w", "Desktop;Simulator").With("Down", "<Keyboard>/s", "Desktop;Simulator").With("Left", "<Keyboard>/a", "Desktop;Simulator").With("Right", "<Keyboard>/d", "Desktop;Simulator");
        var turn = locomotion.AddAction("Turn", InputActionType.Value, expectedControlLayout: "Vector2");
        turn.AddBinding("<XRController>{RightHand}/primary2DAxis", groups: "XR");
        var ui = asset.AddActionMap("UI");
        var menu = ui.AddAction("ToggleMenu", InputActionType.Button);
        menu.AddBinding("<XRController>{LeftHand}/secondaryButton", groups: "XR");
        menu.AddBinding("<XRController>{LeftHand}/menuButton", groups: "XR");
        menu.AddBinding("<Keyboard>/m", groups: "Desktop");
        menu.AddBinding("<Keyboard>/escape", groups: "Desktop");
        menu.AddBinding("<Keyboard>/escape", groups: "Simulator");
        var tools = asset.AddActionMap("Tools");
        var trigger = tools.AddAction("Trigger", InputActionType.Button);
        trigger.AddBinding("<XRController>{RightHand}/{TriggerButton}", groups: "XR");
        trigger.AddBinding("<Keyboard>/space", groups: "Desktop");
        var secondary = tools.AddAction("Secondary", InputActionType.Button);
        secondary.AddBinding("<XRController>{RightHand}/secondaryButton", groups: "XR");
        secondary.AddBinding("<Keyboard>/leftCtrl", groups: "Desktop");
        var desktop = asset.AddActionMap("Desktop");
        desktop.AddAction("Interact", InputActionType.Button).AddBinding("<Keyboard>/e", groups: "Desktop");
        desktop.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2").AddBinding("<Mouse>/delta", groups: "Desktop");
        desktop.AddAction("LookHold", InputActionType.Button).AddBinding("<Mouse>/rightButton", groups: "Desktop");
        File.WriteAllText(path, asset.ToJson());
        UnityEngine.Object.DestroyImmediate(asset);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
    }

    static InputActionReference Action(InputActionAsset asset, string name)
    {
        string path = Folder + "/References/" + name.Replace('/', '_') + ".asset";
        var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
        if (reference == null)
        {
            reference = InputActionReference.Create(asset.FindAction(name, true));
            AssetDatabase.CreateAsset(reference, path);
        }
        return reference;
    }

    static XRInputValueReader<Vector2> Reader(InputActionReference reference) => new XRInputValueReader<Vector2>(reference.action.name)
    {
        inputSourceMode = XRInputValueReader.InputSourceMode.InputActionReference,
        inputActionReference = reference
    };

    [MenuItem("Vivienda/XR/Configurar locomocion nativa")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de configurar XR.");
        var rig = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
        if (rig == null) throw new InvalidOperationException("Falta XR Origin en la escena.");
        var asset = CreateInput();
        var capsule = rig.Origin.GetComponent<CharacterController>();
        capsule.height = 1.7f;
        capsule.radius = .3f;
        capsule.skinWidth = .02f;
        capsule.stepOffset = .4f;
        capsule.slopeLimit = 45;
        capsule.center = Vector3.up * .85f;
        capsule.minMoveDistance = 0;
        rig.CameraYOffset = 1.65f;
        EditorUtility.SetDirty(capsule);
        var mediator = rig.GetComponentInChildren<LocomotionMediator>(true);
        if (mediator == null)
        {
            var root = new GameObject("Locomotion");
            root.transform.SetParent(rig.transform, false);
            mediator = root.AddComponent<LocomotionMediator>();
        }
        mediator.gameObject.SetActive(true);
        mediator.enabled = true;
        var transformer = mediator.GetComponent<XRBodyTransformer>();
        transformer.xrOrigin = rig;
        transformer.useCharacterControllerIfExists = true;
        transformer.enabled = true;
        var move = mediator.GetComponentsInChildren<ContinuousMoveProvider>(true).FirstOrDefault(m => m.GetType() == typeof(ContinuousMoveProvider));
        if (move == null)
        {
            var root = new GameObject("Continuous Move");
            root.transform.SetParent(mediator.transform, false);
            move = root.AddComponent<ContinuousMoveProvider>();
        }
        var teleport = mediator.GetComponentInChildren<TeleportationProvider>(true);
        if (teleport == null) teleport = mediator.gameObject.AddComponent<TeleportationProvider>();
        var turn = mediator.GetComponentInChildren<SnapTurnProvider>(true);
        if (turn == null) turn = mediator.gameObject.AddComponent<SnapTurnProvider>();
        var gravity = mediator.GetComponentInChildren<GravityProvider>(true);
        if (gravity == null) gravity = mediator.gameObject.AddComponent<GravityProvider>();
        foreach (var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))
        {
            bool active = provider == move || provider == teleport || provider == turn || provider == gravity;
            provider.enabled = active;
            if (active) { provider.gameObject.SetActive(true); provider.mediator = mediator; }
            EditorUtility.SetDirty(provider);
        }
        var stairRamp = UnityEngine.Object.FindObjectsByType<BoxCollider>().FirstOrDefault(c => c.name == "Escaleras_Rampa_PROV");
        if (stairRamp != null && stairRamp.enabled)
        {
            // A continuous collision surface avoids a head-height step on every visible tread.
            foreach (var tread in UnityEngine.Object.FindObjectsByType<Collider>().Where(c => c.name.StartsWith("Suelo Escaleras")))
            {
                tread.enabled = false;
                EditorUtility.SetDirty(tread);
            }
        }
        move.moveSpeed = 1.4f;
        move.inAirControlModifier = 1;
        move.enableFly = false;
        move.enableStrafe = true;
        move.forwardSource = rig.Camera.transform;
        move.leftHandMoveInput = Reader(Action(asset, "Locomotion/Move"));
        move.rightHandMoveInput = new XRInputValueReader<Vector2>("Unused", XRInputValueReader.InputSourceMode.Unused);
        move.gravityProvider = gravity;
        move.transformationPriority = 10;
        gravity.useGravity = true;
        gravity.useLocalSpaceGravity = false;
        gravity.terminalVelocity = 20;
        gravity.sphereCastRadius = .08f;
        gravity.sphereCastDistanceBuffer = 0;
        gravity.updateCharacterControllerCenterEachFrame = true;
        gravity.transformationPriority = 20;
        gravity.gravityControllers.Clear();
        gravity.gravityControllers.Add(move);
        turn.leftHandTurnInput = new XRInputValueReader<Vector2>("Unused", XRInputValueReader.InputSourceMode.Unused);
        turn.rightHandTurnInput = Reader(Action(asset, "Locomotion/Turn"));
        turn.turnAmount = 30;
        turn.enableTurnAround = false;
        turn.debounceTime = .3f;
        turn.transformationPriority = 0;
        teleport.delayTime = 0;
        teleport.transformationPriority = 100;
        foreach (var legacy in rig.GetComponentsInChildren<SimpleCharacterLocomotion>(true)) Undo.DestroyObjectImmediate(legacy);
        foreach (var profile in rig.GetComponentsInChildren<GroundedXrLocomotionProfile>(true)) Undo.DestroyObjectImmediate(profile);
        foreach (var spawn in rig.GetComponentsInChildren<PlayerSpawnBootstrap>(true)) Undo.DestroyObjectImmediate(spawn);
        foreach (var sprint in rig.GetComponentsInChildren<GroundedXrSprintModifier>(true)) Undo.DestroyObjectImmediate(sprint);
        foreach (var enforcer in UnityEngine.Object.FindObjectsByType<XrDeviceSimulatorGroundedEnforcer>(FindObjectsInactive.Include)) Undo.DestroyObjectImmediate(enforcer);
        foreach (var controller in rig.GetComponentsInChildren<ControllerInputActionManager>(true))
        {
            Reference(controller, "m_Move", null);
            Reference(controller, "m_Turn", null);
            Reference(controller, "m_SnapTurn", null);
        }
        var manager = rig.GetComponent<InputActionManager>();
        if (manager == null) manager = rig.gameObject.AddComponent<InputActionManager>();
        var assets = manager.actionAssets;
        if (assets == null) assets = new List<InputActionAsset>();
        if (!assets.Contains(asset)) assets.Add(asset);
        manager.actionAssets = assets;
        manager.enabled = true;
        var fade = rig.GetComponentInChildren<TeleportFadeController>(true);
        var gate = rig.GetComponent<XrInputModeGate>();
        if (gate == null) gate = rig.gameObject.AddComponent<XrInputModeGate>();
        Reference(gate, "actions", asset);
        Reference(gate, "fade", fade);
        var desktop = rig.GetComponent<DesktopPreview>();
        Reference(desktop, "lookAction", Action(asset, "Desktop/Look"));
        Reference(desktop, "lookHoldAction", Action(asset, "Desktop/LookHold"));
        Reference(rig.GetComponent<DesktopDoorInteraction>(), "interactAction", Action(asset, "Desktop/Interact"));
        var host = rig.GetComponentInChildren<ViviendaToolHost>(true);
        Reference(host, "triggerAction", Action(asset, "Tools/Trigger"));
        Reference(host, "secondaryAction", Action(asset, "Tools/Secondary"));
        foreach (var menu in UnityEngine.Object.FindObjectsByType<VrMenuToggleInput>(FindObjectsInactive.Include)) Reference(menu, "m_ToggleAction", Action(asset, "UI/ToggleMenu"));
        foreach (var service in UnityEngine.Object.FindObjectsByType<XrLocomotionTeleportService>(FindObjectsInactive.Include)) Reference(service, "m_Provider", teleport);
        foreach (var bridge in UnityEngine.Object.FindObjectsByType<DayNightEnvironmentBridge>(FindObjectsInactive.Include)) bridge.enabled = false;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        foreach (var o in new UnityEngine.Object[] {rig, transformer, mediator, move, gravity, turn, teleport, manager, asset}) EditorUtility.SetDirty(o);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("XR nativo configurado: Mediator, Body Transformer, Continuous Move, Gravity, Snap Turn y Teleportation. Input Actions: " + AssetDatabase.GetAssetPath(asset));
    }
}
