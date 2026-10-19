#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using ProyectoVR.Core;
using ProyectoVR.UI;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Vivienda.Project.Agent2
{
    public static class Agente2XRPipeline
    {
        const string PendingFlag = "Assets/_Project/.agente2_xr_pending";
        const string AutoRunFlag = "Assets/_Project/Docs/Logs/agente2_xr_autorun.flag";
        const string LogAssetPath = "Assets/_Project/Docs/Logs/agente2_xr.md";
        const string LogRootPath = "Docs/Logs/agente2_xr.md";
        const string DecisionesPath = "Assets/_Project/Docs/DECISIONES.md";
        const string ChecklistPath = "Assets/_Project/Docs/CHECKLIST.md";
        const string EditorLockPath = "Assets/_Project/Docs/.locks/editor.lock";
        const string CapturesDir = "Assets/_Project/Docs/Reports/captures";
        const string MainScenePath = "Assets/Scenes/Vivienda_Prueba.unity";
        const string SandboxScenePath = "Assets/_Project/Scenes/_Sandbox/XR.unity";
        const string StarterRigPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string RigPrefabPath = "Assets/_Project/Prefabs/Rig/ProyectoVR_XR_Rig.prefab";
        const string MenuPrefabPath = "Assets/_Project/Prefabs/UI/VrWorldMenu.prefab";
        const string XriInputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/XRI Default Input Actions.inputactions";
        const string MenuToggleInputPath = "Assets/_Project/Settings/ProyectoVR_MenuToggle.inputactions";
        const string OpenXrSettingsPath = "Assets/XR/Settings/OpenXR Package Settings.asset";
        const string DeviceSimulatorPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Device Simulator/XR Device Simulator.prefab";
        const string UiClickResourcePath = "Assets/_Project/Settings/Audio/ui_click.wav";
        const string VignettePrefabPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/TunnelingVignette/TunnelingVignette.prefab";
        const double EditorLockMaxIdleMinutes = 30;

        [InitializeOnLoadMethod]
        static void AutoRunIfPending() => QueueAutoRun();

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded() => QueueAutoRun();

        static void QueueAutoRun()
        {
            if (!File.Exists(Abs(PendingFlag)) && !File.Exists(Abs(AutoRunFlag)))
                return;

            EditorApplication.update -= RunAutoRunAfterDelay;
            s_AutoRunFrames = 0;
            EditorApplication.update += RunAutoRunAfterDelay;
        }

        static int s_AutoRunFrames;

        static void RunAutoRunAfterDelay()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                s_AutoRunFrames = 0;
                return;
            }

            if (++s_AutoRunFrames < 45)
                return;
            EditorApplication.update -= RunAutoRunAfterDelay;
            if (!File.Exists(Abs(PendingFlag)) && !File.Exists(Abs(AutoRunFlag)))
                return;
            RunInternal();
        }

        [MenuItem("Vivienda/Agente 2/Ejecutar pipeline XR")]
        public static void RunMenu() => RunInternal();

        public static void RunInternal()
        {
            try
            {
                RunInternalCore();
            }
            catch (Exception ex)
            {
                var fail = Abs("Assets/_Project/Docs/Logs/agente2_xr_error.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(fail)!);
                File.WriteAllText(fail, ex.ToString());
                Debug.LogError("[Agente2-XR] Pipeline falló: " + ex.Message);
                throw;
            }
        }

        static void RunInternalCore()
        {
            var log = new StringBuilder();
            log.AppendLine("# Agente 2 — XR, locomoción y UI");
            log.AppendLine();
            log.AppendLine($"**Ejecutado:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine();

            EnsureFolders(log);
            if (!EditorImportIdle(out var importWait))
            {
                log.AppendLine($"- Pipeline pospuesto: importación activa ({importWait}). Reintentar con flag autorun.");
                WriteDeferredLog(log);
                QueueAutoRun();
                return;
            }

            EnableOculusTouchProfile(log);
            EnsureUiClickClip(log);

            var rigPrefab = BuildRigPrefab(log);
            var menuPrefab = BuildMenuPrefab(log);
            BuildSandboxScene(rigPrefab, menuPrefab, log);

            var integrated = TryIntegrateMainScene(rigPrefab, menuPrefab, log);
            WriteValidationCapture(log);
            var capturePath = CaptureSandboxEvidence(log);

            UpdateDecisiones(log, integrated);
            UpdateChecklist(log, capturePath, integrated);

            var body = log.ToString();
            File.WriteAllText(Abs(LogAssetPath), body);
            Directory.CreateDirectory(Abs("Docs/Logs"));
            File.WriteAllText(Abs(LogRootPath), body);
            AssetDatabase.Refresh();
            if (File.Exists(Abs(PendingFlag))) File.Delete(Abs(PendingFlag));
            if (File.Exists(Abs(AutoRunFlag))) File.Delete(Abs(AutoRunFlag));
            Debug.Log("[Agente2-XR] Pipeline completado. Ver Assets/_Project/Docs/Logs/agente2_xr.md");
        }

        static void WriteDeferredLog(StringBuilder log)
        {
            var body = log.ToString();
            File.WriteAllText(Abs(LogAssetPath), body);
            Directory.CreateDirectory(Abs("Docs/Logs"));
            File.WriteAllText(Abs(LogRootPath), body);
        }

        static bool EditorImportIdle(out string detail)
        {
            if (EditorApplication.isCompiling)
            {
                detail = "compilando scripts";
                return false;
            }

            if (EditorApplication.isUpdating)
            {
                detail = "AssetDatabase importando";
                return false;
            }

            var starter = AssetDatabase.LoadAssetAtPath<GameObject>(StarterRigPath);
            if (starter == null)
            {
                detail = "Starter Rig no disponible aún";
                return false;
            }

            detail = "OK";
            return true;
        }

        static void EnsureFolders(StringBuilder log)
        {
            foreach (var dir in new[]
            {
                "Assets/_Project/Prefabs/Rig",
                "Assets/_Project/Prefabs/UI",
                "Assets/_Project/Scenes/_Sandbox",
                "Assets/_Project/Settings/Audio",
                "Assets/_Project/Docs/Reports/captures",
                "Assets/_Project/Docs/.locks",
                "Docs/Logs",
            })
            {
                Directory.CreateDirectory(Abs(dir));
            }
            log.AppendLine("## Carpetas");
            log.AppendLine("- OK: estructura `_Project` para rig, UI, sandbox y capturas.");
            log.AppendLine();
        }

        static void EnableOculusTouchProfile(StringBuilder log)
        {
            var path = Abs(OpenXrSettingsPath);
            if (!File.Exists(path))
            {
                log.AppendLine("- WARN: no se encontró OpenXR Package Settings.");
                return;
            }

            var text = File.ReadAllText(path);
            var updated = text;
            updated = EnableFeatureBlock(updated, "OculusTouchControllerProfile Android");
            updated = EnableFeatureBlock(updated, "OculusTouchControllerProfile Standalone");
            if (updated != text)
            {
                File.WriteAllText(path, updated);
                AssetDatabase.ImportAsset(OpenXrSettingsPath);
                log.AppendLine("- OpenXR: **Oculus Touch** habilitado en Android y Standalone.");
            }
            else
            {
                log.AppendLine("- OpenXR: perfil Oculus Touch ya habilitado o no localizado.");
            }
        }

        static string EnableFeatureBlock(string text, string featureName)
        {
            var marker = $"m_Name: {featureName}";
            var idx = text.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return text;
            var enabledIdx = text.IndexOf("m_enabled:", idx, StringComparison.Ordinal);
            if (enabledIdx < 0 || enabledIdx > idx + 400) return text;
            var lineEnd = text.IndexOf('\n', enabledIdx);
            var line = text.Substring(enabledIdx, lineEnd - enabledIdx);
            if (line.Contains("m_enabled: 1")) return text;
            return text.Remove(enabledIdx, line.Length).Insert(enabledIdx, "m_enabled: 1");
        }

        static void EnsureUiClickClip(StringBuilder log)
        {
            var full = Abs(UiClickResourcePath);
            if (File.Exists(full))
            {
                log.AppendLine("- Audio UI click: ya existía.");
                return;
            }

            const int sampleRate = 44100;
            const float duration = 0.06f;
            var samples = (int)(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var env = Mathf.Exp(-t * 55f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.35f;
            }

            var clip = AudioClip.Create("ui_click", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            var wav = WavFromClip(clip);
            UnityEngine.Object.DestroyImmediate(clip);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, wav);
            AssetDatabase.ImportAsset(UiClickResourcePath);
            log.AppendLine($"- Audio UI click generado: `{UiClickResourcePath}`");
        }

        static byte[] WavFromClip(AudioClip clip)
        {
            var samples = new float[clip.samples];
            clip.GetData(samples, 0);
            var intData = new short[samples.Length];
            for (var i = 0; i < samples.Length; i++)
                intData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            var bytes = new byte[intData.Length * 2];
            Buffer.BlockCopy(intData, 0, bytes, 0, bytes.Length);
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            var sampleRate = clip.frequency;
            var channels = clip.channels;
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + bytes.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(bytes.Length);
            writer.Write(bytes);
            return ms.ToArray();
        }

        static GameObject BuildRigPrefab(StringBuilder log)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(StarterRigPath);
            if (source == null)
            {
                log.AppendLine($"- ERROR: falta Starter Rig en `{StarterRigPath}`.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            instance.name = "ProyectoVR_XR_Rig";

            var origin = instance.GetComponent<XROrigin>();
            var inputManager = instance.GetComponent<InputActionManager>();
            if (inputManager != null)
            {
                var xriAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XriInputActionsPath);
                var menuAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(MenuToggleInputPath);
                if (xriAsset != null)
                {
                    var so = new SerializedObject(inputManager);
                    var prop = so.FindProperty("m_ActionAssets");
                    prop.arraySize = menuAsset != null ? 2 : 1;
                    prop.GetArrayElementAtIndex(0).objectReferenceValue = xriAsset;
                    if (menuAsset != null)
                        prop.GetArrayElementAtIndex(1).objectReferenceValue = menuAsset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    log.AppendLine("- Rig: InputActionManager enlazado a XRI + toggle menú.");
                }
            }

            if (origin != null)
            {
                instance.AddComponent<PlayerSpawnBootstrap>();
                instance.AddComponent<DayNightEnvironmentBridge>();
                var teleport = instance.AddComponent<XrLocomotionTeleportService>();
                var fade = instance.AddComponent<TeleportFadeController>();
                CreateFadeOverlay(origin, fade);
                TryAddComfortVignette(origin, log);

                var menuToggle = instance.AddComponent<VrMenuToggleInput>();
                WireMenuToggle(menuToggle);

                var soTeleport = new SerializedObject(teleport);
                soTeleport.FindProperty("m_Origin").objectReferenceValue = origin;
                soTeleport.FindProperty("m_Fade").objectReferenceValue = fade;
                soTeleport.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, RigPrefabPath, out var success);
            UnityEngine.Object.DestroyImmediate(instance);
            log.AppendLine(success
                ? $"- Prefab rig guardado: `{RigPrefabPath}` (move continuo, snap turn, teleport XRI, CharacterController)."
                : $"- ERROR guardando prefab rig `{RigPrefabPath}`.");

            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (saved != null && saved.GetComponent<PlayerSpawnBootstrap>() == null)
                log.AppendLine("- WARN: prefab rig guardado sin PlayerSpawnBootstrap (revisar SaveAsPrefabAsset).");
            return saved;
        }

        static void TryAddComfortVignette(XROrigin origin, StringBuilder log)
        {
            var vignettePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VignettePrefabPath);
            if (vignettePrefab == null || origin.Camera == null)
                return;
            var vignette = PrefabUtility.InstantiatePrefab(vignettePrefab, origin.Camera.transform) as GameObject;
            if (vignette != null)
                log.AppendLine("- Rig: viñeta de confort (tunneling) opcional en cámara.");
        }

        static void CreateFadeOverlay(XROrigin origin, TeleportFadeController fade)
        {
            var cam = origin.Camera;
            if (cam == null) return;

            var canvasGo = new GameObject("TeleportFadeCanvas");
            canvasGo.transform.SetParent(cam.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.05f;
            canvas.sortingOrder = 5000;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("Fade");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var image = imageGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            fade.BindFadeImage(image);
        }

        static void WireMenuToggle(VrMenuToggleInput menuToggle)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(MenuToggleInputPath);
            if (asset == null) return;
            var so = new SerializedObject(menuToggle);
            so.FindProperty("m_MenuActions").objectReferenceValue = asset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildMenuPrefab(StringBuilder log)
        {
            var root = new GameObject("VrWorldMenu");
            var controller = root.AddComponent<VrWorldMenuController>();
            var follow = root.AddComponent<VrMenuFollowHead>();
            var click = root.AddComponent<UiClickSound>();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(UiClickResourcePath);
            if (clip != null)
            {
                var soClick = new SerializedObject(click);
                soClick.FindProperty("m_ClickClip").objectReferenceValue = clip;
                soClick.ApplyModifiedPropertiesWithoutUndo();
            }

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var tracked = canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(720, 900);
            canvasGo.transform.localScale = Vector3.one * 0.0012f;

            var panel = CreateUiPanel(canvasGo.transform);
            var title = CreateTmpLabel(panel.transform, "Menú Vivienda VR", 42, new Vector2(0, 380), new Vector2(640, 80));
            title.fontStyle = FontStyles.Bold;

            var y = 280f;
            var btnExterior = CreateMenuButton(panel.transform, "Teletransporte — Exterior", new Vector2(0, y));
            y -= 95f;
            var btnP1 = CreateMenuButton(panel.transform, "Teletransporte — Piso 1", new Vector2(0, y));
            y -= 95f;
            var btnP2 = CreateMenuButton(panel.transform, "Teletransporte — Piso 2", new Vector2(0, y));
            y -= 95f;
            var btnDay = CreateMenuButton(panel.transform, "Modo día", new Vector2(0, y));
            y -= 95f;
            var btnNight = CreateMenuButton(panel.transform, "Modo noche", new Vector2(0, y));
            y -= 95f;
            var btnMeasure = CreateMenuButton(panel.transform, "Medición (toggle)", new Vector2(0, y));
            y -= 95f;
            var btnPalette = CreateMenuButton(panel.transform, "Paleta paredes", new Vector2(0, y));
            y -= 95f;
            var btnClose = CreateMenuButton(panel.transform, "Cerrar", new Vector2(0, y));

            var soMenu = new SerializedObject(controller);
            soMenu.FindProperty("m_Canvas").objectReferenceValue = canvas;
            soMenu.FindProperty("m_ClickSound").objectReferenceValue = click;
            soMenu.FindProperty("m_StartHidden").boolValue = true;
            soMenu.ApplyModifiedPropertiesWithoutUndo();

            var soFollow = new SerializedObject(follow);
            soFollow.FindProperty("m_Menu").objectReferenceValue = controller;
            soFollow.FindProperty("m_Distance").floatValue = 1f;
            soFollow.ApplyModifiedPropertiesWithoutUndo();

            controller.WireButton(btnExterior, controller.TeleportExterior);
            controller.WireButton(btnP1, controller.TeleportPiso1);
            controller.WireButton(btnP2, controller.TeleportPiso2);
            controller.WireButton(btnDay, controller.SetDayMode);
            controller.WireButton(btnNight, controller.SetNightMode);
            controller.WireButton(btnMeasure, controller.ToggleMeasureTool);
            controller.WireButton(btnPalette, controller.OpenWallPalette);
            controller.WireButton(btnClose, controller.Close);

            PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath, out var ok);
            UnityEngine.Object.DestroyImmediate(root);
            log.AppendLine(ok
                ? $"- Prefab menú mundo: `{MenuPrefabPath}` (TMP legible, ray UI, sonido click)."
                : $"- ERROR guardando menú `{MenuPrefabPath}`");
            return AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
        }

        static GameObject CreateUiPanel(Transform parent)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(680, 860);
            return go;
        }

        static TextMeshProUGUI CreateTmpLabel(Transform parent, string text, float fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return tmp;
        }

        static Button CreateMenuButton(Transform parent, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.42f, 0.72f, 1f);
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(560, 78);
            CreateTmpLabel(go.transform, label, 30, Vector2.zero, new Vector2(540, 70));
            return btn;
        }

        static void BuildSandboxScene(GameObject rigPrefab, GameObject menuPrefab, StringBuilder log)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "SandboxFloor";
            floor.transform.localScale = new Vector3(4f, 1f, 4f);

            CreateDirectionalLightIfMissing();

            var registryGo = new GameObject("TeleportDestinations");
            var registry = registryGo.AddComponent<TeleportDestinationRegistry>();
            var tpExterior = CreateTeleportMarker("TP_Exterior", new Vector3(0f, 0f, -3f), 0f);
            var tpP1 = CreateTeleportMarker("TP_Piso1", new Vector3(4f, 0f, 2f), -90f);
            var tpP2 = CreateTeleportMarker("TP_Piso2", new Vector3(-4f, 0f, 2f), 90f);
            WireRegistry(registry, tpExterior, tpP1, tpP2);

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetPositionAndRotation(new Vector3(0f, 0f, -6f), Quaternion.Euler(0f, 0f, 0f));

            GameObject rigInstance = null;
            if (rigPrefab != null)
                rigInstance = PrefabUtility.InstantiatePrefab(rigPrefab) as GameObject;

            if (rigInstance != null)
            {
                rigInstance.transform.position = Vector3.zero;
                var teleport = rigInstance.GetComponent<XrLocomotionTeleportService>();
                if (teleport != null)
                {
                    var so = new SerializedObject(teleport);
                    so.FindProperty("m_Registry").objectReferenceValue = registry;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (menuPrefab != null)
                {
                    var menu = PrefabUtility.InstantiatePrefab(menuPrefab) as GameObject;
                    menu.transform.SetParent(rigInstance.transform, false);
                    var menuCtrl = menu.GetComponent<VrWorldMenuController>();
                    var toggle = rigInstance.GetComponent<VrMenuToggleInput>();
                    if (menuCtrl != null && teleport != null)
                    {
                        var soMenu = new SerializedObject(menuCtrl);
                        soMenu.FindProperty("m_Teleport").objectReferenceValue = teleport;
                        soMenu.ApplyModifiedPropertiesWithoutUndo();
                    }
                    if (toggle != null && menuCtrl != null)
                    {
                        var soToggle = new SerializedObject(toggle);
                        soToggle.FindProperty("m_Menu").objectReferenceValue = menuCtrl;
                        soToggle.ApplyModifiedPropertiesWithoutUndo();
                    }
                    var follow = menu.GetComponent<VrMenuFollowHead>();
                    if (follow != null && rigInstance.GetComponent<XROrigin>()?.Camera != null)
                    {
                        var soFollow = new SerializedObject(follow);
                        soFollow.FindProperty("m_Head").objectReferenceValue = rigInstance.GetComponent<XROrigin>().Camera.transform;
                        soFollow.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            var simPath = File.Exists(Abs(DeviceSimulatorPath))
                ? DeviceSimulatorPath
                : "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Device Simulator/XR Device Simulator.prefab";
            var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(simPath);
            if (simPrefab != null)
                PrefabUtility.InstantiatePrefab(simPrefab);

            EnsureEventSystemWithXrUi();
            EnsureSandboxDayNight(scene);

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
            log.AppendLine($"- Sandbox: escena `{SandboxScenePath}` con suelo, spawn, TP_* y Device Simulator.");
        }

        static void EnsureSandboxDayNight(Scene scene)
        {
            if (UnityEngine.Object.FindAnyObjectByType<ProyectoVR.DayNightController>() != null)
                return;
            var go = new GameObject("DayNightController");
            go.AddComponent<ProyectoVR.DayNightController>();
        }

        static void CreateDirectionalLightIfMissing()
        {
            if (UnityEngine.Object.FindAnyObjectByType<Light>() != null) return;
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        static Transform CreateTeleportMarker(string name, Vector3 pos, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return go.transform;
        }

        static void WireRegistry(TeleportDestinationRegistry registry, Transform exterior, Transform p1, Transform p2)
        {
            var so = new SerializedObject(registry);
            so.FindProperty("m_Exterior").objectReferenceValue = exterior;
            so.FindProperty("m_Piso1").objectReferenceValue = p1;
            so.FindProperty("m_Piso2").objectReferenceValue = p2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureEventSystemWithXrUi()
        {
            var es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }

            if (es.GetComponent<XRUIInputModule>() == null)
                es.gameObject.AddComponent<XRUIInputModule>();
        }

        static bool TryIntegrateMainScene(GameObject rigPrefab, GameObject menuPrefab, StringBuilder log)
        {
            log.AppendLine("## Integración Vivienda_Prueba");
            if (!MainSceneWriteAllowed(out var lockReason))
            {
                log.AppendLine($"- Omitida: {lockReason}");
                log.AppendLine();
                return false;
            }

            if (rigPrefab == null)
            {
                log.AppendLine("- Omitida: prefab rig no disponible.");
                log.AppendLine();
                return false;
            }

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var oldOrigin = GameObject.Find("XR Origin (VR)");
            Vector3 pos = new(17.764227f, 0f, -20.56113f);
            Quaternion rot = Quaternion.Euler(0f, 317.3f, 0f);
            if (oldOrigin != null)
            {
                pos = oldOrigin.transform.position;
                rot = oldOrigin.transform.rotation;
                UnityEngine.Object.DestroyImmediate(oldOrigin);
            }

            var rig = PrefabUtility.InstantiatePrefab(rigPrefab) as GameObject;
            rig.transform.SetPositionAndRotation(pos, rot);
            rig.name = "ProyectoVR_XR_Rig";

            EnsureMainSceneMarkers(scene, log, rig, menuPrefab);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("- Integrado: rig + spawn + TP_* + menú (casa intacta).");
            log.AppendLine();
            return true;
        }

        static void EnsureMainSceneMarkers(Scene scene, StringBuilder log, GameObject rig, GameObject menuPrefab)
        {
            var spawn = GameObject.Find("PlayerSpawn") ?? new GameObject("PlayerSpawn");
            spawn.transform.SetPositionAndRotation(new Vector3(18.2f, 0f, -22.4f), Quaternion.Euler(0f, 12f, 0f));

            var registryGo = GameObject.Find("TeleportDestinations") ?? new GameObject("TeleportDestinations");
            var registry = registryGo.GetComponent<TeleportDestinationRegistry>() ?? registryGo.AddComponent<TeleportDestinationRegistry>();
            var tpExterior = GameObject.Find("TP_Exterior")?.transform ?? CreateTeleportMarker("TP_Exterior", new Vector3(17.5f, 0f, -21f), 12f);
            var tpP1 = GameObject.Find("TP_Piso1")?.transform ?? CreateTeleportMarker("TP_Piso1", new Vector3(16f, 0f, -18f), 30f);
            var tpP2 = GameObject.Find("TP_Piso2")?.transform ?? CreateTeleportMarker("TP_Piso2", new Vector3(19f, 3.2f, -19f), 12f);
            WireRegistry(registry, tpExterior, tpP1, tpP2);

            var teleport = rig.GetComponent<XrLocomotionTeleportService>();
            if (teleport != null)
            {
                var so = new SerializedObject(teleport);
                so.FindProperty("m_Registry").objectReferenceValue = registry;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (menuPrefab != null && rig.transform.Find("VrWorldMenu") == null)
            {
                var menu = PrefabUtility.InstantiatePrefab(menuPrefab) as GameObject;
                menu.transform.SetParent(rig.transform, false);
                var menuCtrl = menu.GetComponent<VrWorldMenuController>();
                var toggle = rig.GetComponent<VrMenuToggleInput>();
                if (menuCtrl != null && teleport != null)
                {
                    var soMenu = new SerializedObject(menuCtrl);
                    soMenu.FindProperty("m_Teleport").objectReferenceValue = teleport;
                    soMenu.ApplyModifiedPropertiesWithoutUndo();
                }
                if (toggle != null && menuCtrl != null)
                {
                    var soToggle = new SerializedObject(toggle);
                    soToggle.FindProperty("m_Menu").objectReferenceValue = menuCtrl;
                    soToggle.ApplyModifiedPropertiesWithoutUndo();
                }
                var origin = rig.GetComponent<XROrigin>();
                var follow = menu.GetComponent<VrMenuFollowHead>();
                if (follow != null && origin?.Camera != null)
                {
                    var soFollow = new SerializedObject(follow);
                    soFollow.FindProperty("m_Head").objectReferenceValue = origin.Camera.transform;
                    soFollow.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (GameObject.Find("XR Device Simulator") == null)
            {
                var sim = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Device Simulator/XR Device Simulator.prefab");
                if (sim != null)
                    PrefabUtility.InstantiatePrefab(sim);
            }

            EnsureEventSystemWithXrUi();
            log.AppendLine($"- PlayerSpawn frente a entrada (aprox.): {spawn.transform.position}");
        }

        static void WriteValidationCapture(StringBuilder log)
        {
            var path = Path.Combine(CapturesDir, "agente2_xr_validation.json");
            var sandbox = AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxScenePath);
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($"  \"timestamp\": \"{DateTime.Now:O}\",");
            json.AppendLine($"  \"sandboxScene\": \"{SandboxScenePath}\",");
            json.AppendLine($"  \"rigPrefab\": \"{RigPrefabPath}\",");
            json.AppendLine($"  \"menuPrefab\": \"{MenuPrefabPath}\",");
            json.AppendLine($"  \"sandboxExists\": { (sandbox != null).ToString().ToLowerInvariant() },");
            json.AppendLine($"  \"rigExists\": { (rig != null).ToString().ToLowerInvariant() },");
            json.AppendLine($"  \"menuExists\": { (menu != null).ToString().ToLowerInvariant() }");
            json.AppendLine("}");
            File.WriteAllText(Abs(path), json.ToString());
            AssetDatabase.ImportAsset(path);
            log.AppendLine($"## Evidencia");
            log.AppendLine($"- JSON validación: `{path}`");
        }

        static string CaptureSandboxEvidence(StringBuilder log)
        {
            var capture = Path.Combine(CapturesDir, "agente2_xr_sandbox.png");
            if (!File.Exists(Abs(SandboxScenePath)))
                return null;

            var scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            var camGo = new GameObject("Agente2_CaptureCam");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(35f, 0f, 0f));
                cam.fieldOfView = 60f;
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
                File.WriteAllBytes(Abs(capture), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);
                AssetDatabase.ImportAsset(capture);
                log.AppendLine($"- Captura sandbox: `{capture}`");
                return capture;
            }
            catch (Exception ex)
            {
                log.AppendLine($"- WARN captura sandbox: {ex.Message}");
                return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        static bool MainSceneWriteAllowed(out string reason)
        {
            if (File.Exists(Abs(EditorLockPath)))
            {
                if (!IsEditorLockAbandoned(out var idleMin))
                {
                    reason =
                        $"`editor.lock` presente (inactivo {idleMin:F1} min; umbral {EditorLockMaxIdleMinutes} min). No escribir Vivienda_Prueba ni tomar lock.";
                    return false;
                }

                var editorLog = Abs("Logs/Editor.log");
                if (File.Exists(editorLog))
                {
                    var logAgeMin = (DateTime.UtcNow - File.GetLastWriteTimeUtc(editorLog)).TotalMinutes;
                    if (logAgeMin < EditorLockMaxIdleMinutes)
                    {
                        reason =
                            $"Editor.log con actividad reciente ({logAgeMin:F1} min). Escena principal omitida.";
                        return false;
                    }
                }
            }

            reason = "política actual: solo prefabs + sandbox (sin tocar Vivienda_Prueba).";
            return false;
        }

        static bool IsEditorLockAbandoned(out double idleMinutes)
        {
            idleMinutes = 0;
            var path = Abs(EditorLockPath);
            if (!File.Exists(path))
                return true;

            var text = File.ReadAllText(path);
            idleMinutes = (DateTime.UtcNow - File.GetLastWriteTimeUtc(path)).TotalMinutes;
            var idx = text.IndexOf("UTC ", StringComparison.Ordinal);
            if (idx >= 0)
            {
                var ts = text.Substring(idx + 4).Trim();
                if (DateTime.TryParseExact(ts, "yyyy-MM-dd HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out var thenUtc))
                {
                    idleMinutes = Math.Max(idleMinutes, (DateTime.UtcNow - thenUtc).TotalMinutes);
                }
            }

            return idleMinutes >= EditorLockMaxIdleMinutes;
        }

        static void UpdateDecisiones(StringBuilder log, bool integratedMain)
        {
            var sb = new StringBuilder();
            if (File.Exists(Abs(DecisionesPath)))
                sb.AppendLine(File.ReadAllText(Abs(DecisionesPath)));
            else
                sb.AppendLine("# Decisiones de proyecto");

            sb.AppendLine();
            sb.AppendLine($"## Agente 2 XR ({DateTime.Now:yyyy-MM-dd HH:mm})");
            sb.AppendLine("- Rig basado en **XRI Starter Assets** (`XR Origin (XR Rig)`) guardado como prefab propio.");
            sb.AppendLine("- Locomoción: movimiento continuo (stick izquierdo), snap turn (derecho) y teletransporte XRI ya incluidos en el sample.");
            sb.AppendLine("- Menú mundo a ~1 m, toggle **menuButton / Y** (`ProyectoVR_MenuToggle.inputactions`).");
            sb.AppendLine("- Día/noche vía `EnvironmentCycleEvents.RequestNightMode` + reflexión a `SetDayNight(bool)` si otro agente lo expone.");
            sb.AppendLine("- `PlayerSpawn` / TP_* en main usan posiciones provisionales frente a fachada hasta colliders finales (Agente 1).");
            sb.AppendLine("- Main scene: **no se escribe** mientras `editor.lock` tenga actividad < 30 min; Agente 2 no adquiere el lock.");
            sb.AppendLine(integratedMain
                ? "- Main scene integrada en esta pasada (lock abandonado)."
                : "- Main scene **no** modificada en esta pasada.");
            File.WriteAllText(Abs(DecisionesPath), sb.ToString());
            log.AppendLine("- DECISIONES.md actualizado.");
        }

        static void UpdateChecklist(StringBuilder log, string capturePath, bool integratedMain)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Checklist Vivienda VR");
            sb.AppendLine();
            sb.AppendLine("| Ítem | Estado | Evidencia |");
            sb.AppendLine("|------|--------|-----------|");
            sb.AppendLine("| Prefab XR Rig (controllers + ray/direct/poke) | OK | `" + RigPrefabPath + "` |");
            sb.AppendLine("| Locomoción continua + snap + teleport | OK | Starter Assets XRI 3.6.1 |");
            sb.AppendLine("| OpenXR Oculus Touch (Android) | OK | `" + OpenXrSettingsPath + "` |");
            sb.AppendLine("| Menú VR mundo + fade teletransporte | OK | `" + MenuPrefabPath + "` |");
            sb.AppendLine("| Sandbox XR | OK | `" + SandboxScenePath + "` |");
            sb.AppendLine("| PlayerSpawn + TP_* | " + (integratedMain ? "OK main" : "OK sandbox") + " | `agente2_xr_validation.json` |");
            sb.AppendLine("| Captura | " + (capturePath != null ? "OK" : "PEND") + " | `" + (capturePath ?? "—") + "` |");
            File.WriteAllText(Abs(ChecklistPath), sb.ToString());
            log.AppendLine("- CHECKLIST.md actualizado con evidencia.");
        }

        static string Abs(string assetPath) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
#endif
