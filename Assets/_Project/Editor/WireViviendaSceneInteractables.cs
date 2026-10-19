#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProyectoVR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Vivienda.Project.EditorTools
{
    public static class WireViviendaSceneInteractables
    {
        const string MainScene = "Assets/Scenes/Vivienda_Prueba.unity";
        const string LockPath = "Assets/_Project/Docs/.locks/editor.lock";
        const string LogPath = "Assets/_Project/Docs/Logs/interacciones_escena.md";
        const string LatchPath = "Assets/_Project/Audio/Generated/sfx_latch.wav";
        const string WinOpenPath = "Assets/_Project/Audio/Generated/sfx_window_open.wav";
        const string WinClosePath = "Assets/_Project/Audio/Generated/sfx_window_close.wav";
        const string BusPath = "Assets/_Project/Resources/ProyectoVrAudioBus.asset";
        const string XriActionsPath =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/XRI Default Input Actions.inputactions";
        const string LeftInteractorPath =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/Interactors/Left_NearFarInteractor.prefab";
        const string RightInteractorPath =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/Interactors/Right_NearFarInteractor.prefab";

        const int LayerInteractuable = 7;

        [MenuItem("Vivienda/Wire Vivienda_Prueba doors and windows")]
        public static void RunFromMenu() => RunInternal();

        public static void RunInternal()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("[Interacciones] Unity ocupado; reintentar tras compilar.");
                return;
            }

            var log = new StringBuilder();
            log.AppendLine($"# Interacciones escena ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
            log.AppendLine();

            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            var bus = AssetDatabase.LoadAssetAtPath<ProyectoVrAudioBus>(BusPath);
            var latch = AssetDatabase.LoadAssetAtPath<AudioClip>(LatchPath);
            var winOpen = AssetDatabase.LoadAssetAtPath<AudioClip>(WinOpenPath);
            var winClose = AssetDatabase.LoadAssetAtPath<AudioClip>(WinClosePath);

            EnsureMinimalInteractionStack(log);

            var doors = 0;
            var windows = 0;
            foreach (var root in FindPlaceholderRoots())
            {
                try
                {
                    var n = root.name;
                    if (n.StartsWith("Puerta_Auto_", StringComparison.Ordinal))
                    {
                        if (WireDoor(root, latch, bus, log))
                            doors++;
                    }
                    else if (n.StartsWith("Ventana_Auto_", StringComparison.Ordinal))
                    {
                        if (WireWindow(root, winOpen, winClose, bus, log))
                            windows++;
                    }
                }
                catch (Exception ex)
                {
                    log.AppendLine($"- ERROR `{root.name}`: {ex.Message}");
                    Debug.LogException(ex);
                }
            }

            log.AppendLine();
            log.AppendLine("## Resumen");
            log.AppendLine($"- Puertas interactivas: **{doors}** / 9 (malla fusionada door-trustile + manija _PROV).");
            log.AppendLine($"- Ventanas interactivas: **{windows}** / 2 (Simple-A fusionada, bisagra lateral).");
            log.AppendLine("- Simulador / XR: **Select / Grip (tecla G)** en manija; rayo Near/Far también agarra.");

            if (MainSceneSaveAllowed())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("- Escena principal guardada.");
            }
            else
            {
                log.AppendLine("- Escena principal **no guardada** (editor.lock activo < 30 min).");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.WriteAllText(LogPath, log.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[Interacciones] Cableadas {doors} puertas y {windows} ventanas. Log: {LogPath}");
        }

        static bool MainSceneSaveAllowed()
        {
            if (!File.Exists(LockPath))
                return true;
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(LockPath);
            return age.TotalMinutes >= 30d;
        }

        static void EnsureMinimalInteractionStack(StringBuilder log)
        {
            if (UnityEngine.Object.FindAnyObjectByType<XRInteractionManager>() == null)
            {
                var mgrGo = new GameObject("XR Interaction Manager");
                mgrGo.AddComponent<XRInteractionManager>();
                log.AppendLine("- Añadido **XR Interaction Manager** (no modifica locomoción).");
            }

            var origin = GameObject.Find("XR Origin (VR)") ?? GameObject.Find("ProyectoVR_XR_Rig");
            if (origin == null)
            {
                log.AppendLine("- WARN: no se encontró XR Origin; interactores no añadidos.");
                return;
            }

            var inputMgr = origin.GetComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>();
            if (inputMgr != null && (inputMgr.actionAssets == null || inputMgr.actionAssets.Count == 0))
            {
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XriActionsPath);
                if (asset != null)
                {
                    inputMgr.actionAssets = new List<InputActionAsset> { asset };
                    EditorUtility.SetDirty(inputMgr);
                    log.AppendLine("- InputActionManager: enlazado XRI Default Input Actions.");
                }
            }

            if (UnityEngine.Object.FindAnyObjectByType<NearFarInteractor>() == null)
            {
                var offset = origin.transform.Find("Camera Offset") ?? origin.transform;
                AttachInteractor(LeftInteractorPath, offset, new Vector3(-0.25f, 0f, 0.35f), "Left_NearFarInteractor");
                AttachInteractor(RightInteractorPath, offset, new Vector3(0.25f, 0f, 0.35f), "Right_NearFarInteractor");
                log.AppendLine("- Interactores Near/Far izq/der bajo Camera Offset (sin mover XR Origin).");
            }
        }

        static void AttachInteractor(string prefabPath, Transform parent, Vector3 localPos, string name)
        {
            if (parent.Find(name) != null)
                return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = name;
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = Quaternion.identity;
        }

        static bool WireDoor(GameObject root, AudioClip latch, ProyectoVrAudioBus bus, StringBuilder log)
        {
            var existingHinge = root.GetComponent<DoorHinge>();
            if (existingHinge != null)
            {
                var check = new SerializedObject(existingHinge);
                var grabOk = check.FindProperty("handleGrab").objectReferenceValue != null;
                var audioOk = check.FindProperty("latchAudio").objectReferenceValue != null;
                if (grabOk && audioOk)
                    return true;
                UnityEngine.Object.DestroyImmediate(existingHinge);
            }

            var hoja = FindHoja(root.transform);
            var marco = root.transform.Find("Marco");
            if (hoja == null)
            {
                log.AppendLine($"- SKIP puerta `{root.name}`: sin Hoja.");
                return false;
            }

            var meshGo = FindDoorMesh(hoja);
            if (meshGo == null)
            {
                log.AppendLine($"- SKIP puerta `{root.name}`: sin malla door-trustile.");
                return false;
            }

            var renderer = meshGo.GetComponent<Renderer>();
            var hingeSide = ComputeHingeSide(root.transform, renderer, marco);
            var pivot = EnsureHingePivot(root.transform, hoja, renderer, hingeSide, log, root.name);

            var grab = EnsureHandleInteractable(hoja, renderer, root.transform, window: false);

            DisableMarcoDoorBlocker(marco);

            foreach (var stale in root.GetComponents<DoorHinge>())
                UnityEngine.Object.DestroyImmediate(stale);

            var audio = root.GetComponent<AudioSource>();
            if (audio == null)
                audio = root.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.playOnAwake = false;
            bus?.RouteToSfx(audio);

            var hinge = root.AddComponent<DoorHinge>();
            var so = new SerializedObject(hinge);
            so.FindProperty("hingePivot").objectReferenceValue = pivot;
            so.FindProperty("doorLeaf").objectReferenceValue = hoja;
            so.FindProperty("handleGrab").objectReferenceValue = grab;
            so.FindProperty("latchAudio").objectReferenceValue = audio;
            so.FindProperty("latchClip").objectReferenceValue = latch;
            so.FindProperty("maxAngle").floatValue = 110f;
            so.FindProperty("hingeOnPositiveY").boolValue = hingeSide.openPositiveY;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLayerRecursive(root, LayerInteractuable);
            log.AppendLine(
                $"- Puerta `{root.name}` → `{meshGo.name}` **fusionada**; bisagra **{hingeSide.label}**.");
            return true;
        }

        static bool WireWindow(GameObject root, AudioClip open, AudioClip close, ProyectoVrAudioBus bus, StringBuilder log)
        {
            var existingWin = root.GetComponent<WindowHinge>();
            if (existingWin != null)
            {
                var check = new SerializedObject(existingWin);
                var grabOk = check.FindProperty("handleGrab").objectReferenceValue != null;
                var audioOk = check.FindProperty("openCloseAudio").objectReferenceValue != null;
                if (grabOk && audioOk)
                    return true;
                UnityEngine.Object.DestroyImmediate(existingWin);
            }

            var hoja = FindHoja(root.transform);
            var marco = root.transform.Find("Marco");
            if (hoja == null)
            {
                log.AppendLine($"- SKIP ventana `{root.name}`: sin Hoja.");
                return false;
            }

            var meshGo = FindWindowMesh(hoja);
            if (meshGo == null)
            {
                log.AppendLine($"- SKIP ventana `{root.name}`: sin malla Simple-A.");
                return false;
            }

            var renderer = meshGo.GetComponent<Renderer>();
            var hingeSide = ComputeHingeSide(root.transform, renderer, marco);
            var pivot = EnsureHingePivot(root.transform, hoja, renderer, hingeSide, log, root.name);

            var grab = EnsureHandleInteractable(hoja, renderer, root.transform, window: true);

            DisableMarcoDoorBlocker(marco);

            foreach (var stale in root.GetComponents<WindowHinge>())
                UnityEngine.Object.DestroyImmediate(stale);

            var audio = root.GetComponent<AudioSource>();
            if (audio == null)
                audio = root.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.playOnAwake = false;
            bus?.RouteToSfx(audio);

            var window = root.AddComponent<WindowHinge>();
            var so = new SerializedObject(window);
            so.FindProperty("hingePivot").objectReferenceValue = pivot;
            so.FindProperty("handleGrab").objectReferenceValue = grab;
            so.FindProperty("openCloseAudio").objectReferenceValue = audio;
            so.FindProperty("openClip").objectReferenceValue = open;
            so.FindProperty("closeClip").objectReferenceValue = close;
            so.FindProperty("openAngle").floatValue = 75f;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLayerRecursive(root, LayerInteractuable);
            log.AppendLine(
                $"- Ventana `{root.name}` → `{meshGo.name}` **fusionada**; bisagra **{hingeSide.label}**.");
            return true;
        }

        struct HingeSide
        {
            public bool useMinX;
            public bool openPositiveY;
            public string label;
        }

        static HingeSide ComputeHingeSide(Transform root, Renderer renderer, Transform marco)
        {
            var side = new HingeSide { openPositiveY = true };
            if (renderer == null)
            {
                side.label = "centro (fallback)";
                side.useMinX = true;
                return side;
            }

            var bounds = renderer.bounds;
            var corners = new[]
            {
                new Vector3(bounds.min.x, bounds.center.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.center.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.center.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.center.y, bounds.max.z)
            };

            float minDot = float.PositiveInfinity, maxDot = float.NegativeInfinity;
            foreach (var c in corners)
            {
                var local = root.InverseTransformPoint(c);
                minDot = Mathf.Min(minDot, local.x);
                maxDot = Mathf.Max(maxDot, local.x);
            }

            var marcoLocalX = marco != null ? root.InverseTransformPoint(marco.position).x : 0f;
            side.useMinX = Mathf.Abs(minDot - marcoLocalX) <= Mathf.Abs(maxDot - marcoLocalX);
            side.openPositiveY = !side.useMinX;
            side.label = side.useMinX ? "borde izquierdo (cerca marco)" : "borde derecho (cerca marco)";
            return side;
        }

        static Transform FindHoja(Transform root)
        {
            var direct = root.Find("Hoja");
            if (direct != null)
                return direct;
            var pivot = root.Find("HingePivot");
            if (pivot != null)
            {
                var underPivot = pivot.Find("Hoja");
                if (underPivot != null)
                    return underPivot;
            }

            foreach (Transform child in root)
            {
                if (child.name == "Hoja")
                    return child;
                if (child.name == "HingePivot")
                {
                    var under = child.Find("Hoja");
                    if (under != null)
                        return under;
                }
            }

            return null;
        }

        static Transform EnsureHingePivot(Transform root, Transform hoja, Renderer renderer, HingeSide side,
            StringBuilder log, string id)
        {
            if (hoja.parent != null && hoja.parent.name == "HingePivot")
                return hoja.parent;

            var existing = root.Find("HingePivot");
            if (existing != null && hoja.IsChildOf(existing))
                return existing;

            var pivot = new GameObject("HingePivot").transform;
            pivot.SetParent(root, false);

            if (renderer != null)
            {
                var bounds = renderer.bounds;
                pivot.position = side.useMinX
                    ? new Vector3(bounds.min.x, bounds.center.y, bounds.center.z)
                    : new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
            }

            var hojaScale = hoja.localScale;
            hoja.SetParent(pivot, true);
            hoja.localScale = hojaScale;

            log.AppendLine($"  - `{id}`: pivote HingePivot en {side.label}.");
            return pivot;
        }

        static GameObject FindDoorMesh(Transform hoja)
        {
            foreach (var mf in hoja.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.IndexOf("door-trustile", StringComparison.OrdinalIgnoreCase) >= 0)
                    return mf.gameObject;
            }

            return hoja.GetComponentInChildren<MeshFilter>(true)?.gameObject;
        }

        static GameObject FindWindowMesh(Transform hoja)
        {
            foreach (var mf in hoja.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.IndexOf("simple-a", StringComparison.OrdinalIgnoreCase) >= 0)
                    return mf.gameObject;
            }

            return hoja.GetComponentInChildren<MeshFilter>(true)?.gameObject;
        }

        static List<GameObject> FindPlaceholderRoots()
        {
            var list = new List<GameObject>();
            foreach (var bucketName in new[] { "Puertas_PLACEHOLDER", "Ventanas_PLACEHOLDER" })
            {
                var bucket = GameObject.Find(bucketName);
                if (bucket == null)
                    continue;
                for (var i = 0; i < bucket.transform.childCount; i++)
                    list.Add(bucket.transform.GetChild(i).gameObject);
            }

            return list;
        }

        static XRGrabInteractable EnsureHandleInteractable(Transform hoja, Renderer renderer, Transform root, bool window)
        {
            var legacy = hoja.Find("HandleInteractable");
            if (legacy != null)
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);

            var proxy = hoja.Find("Manija_PROV") ?? hoja.Find("Manija_PLACEHOLDER");
            if (proxy == null)
            {
                proxy = new GameObject("Manija_PROV").transform;
                proxy.SetParent(hoja, false);
            }
            else
            {
                proxy.name = "Manija_PROV";
                foreach (var c in proxy.GetComponents<Component>())
                {
                    if (c is Transform)
                        continue;
                    UnityEngine.Object.DestroyImmediate(c);
                }
            }

            if (renderer != null)
            {
                var ext = renderer.bounds.extents;
                var center = renderer.bounds.center;
                var right = root.right.sqrMagnitude > 0.0001f ? root.right.normalized : Vector3.right;
                proxy.position = center + right * (ext.x * 0.35f);
            }

            var vis = GameObject.CreatePrimitive(window ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            vis.name = "Manija_VIS_PROV";
            UnityEngine.Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.transform.SetParent(proxy, false);
            vis.transform.localScale = window
                ? new Vector3(0.06f, 0.02f, 0.03f)
                : new Vector3(0.05f, 0.02f, 0.05f);
            if (!window)
                vis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var handleGo = new GameObject("HandleInteractable");
            handleGo.transform.SetParent(hoja, false);
            handleGo.transform.SetPositionAndRotation(proxy.position, proxy.rotation);
            handleGo.layer = LayerInteractuable;

            var box = handleGo.AddComponent<BoxCollider>();
            box.size = new Vector3(0.08f, 0.08f, 0.08f);
            var rb = handleGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var grab = handleGo.AddComponent<XRGrabInteractable>();
            grab.movementType = XRGrabInteractable.MovementType.Kinematic;
            grab.trackPosition = false;
            grab.trackRotation = false;
            return grab;
        }

        static void DisableMarcoDoorBlocker(Transform marco)
        {
            if (marco == null)
                return;
            var box = marco.GetComponent<BoxCollider>();
            if (box != null)
                box.enabled = false;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }
    }
}
#endif
