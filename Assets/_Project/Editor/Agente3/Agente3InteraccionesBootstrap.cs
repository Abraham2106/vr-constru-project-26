#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ProyectoVR;

namespace Vivienda.Project.Agent3
{
    public static class Agente3InteraccionesBootstrap
    {
        // Autorun: Assets/_Project/.agente3_pending
        const string PendingFlag = "Assets/_Project/.agente3_pending";
        const string LockPath = "Assets/_Project/Docs/.locks/editor.lock";
        const string LogPath = "Docs/Logs/agente3_interacciones.md";
        const string DecisionsPath = "Docs/DECISIONES.md";
        const string LicensesPath = "Docs/Licenses.md";
        const string SandboxScene = "Assets/_Project/Scenes/_Sandbox/Interacciones.unity";
        const string MainScene = "Assets/Scenes/Vivienda_Prueba.unity";
        const string MixerPath = ProyectoVrAudioBus.MixerAssetPath;
        const string BusAssetPath = "Assets/_Project/Audio/ProyectoVrAudioBus.asset";
        const string ResourcesBusPath = "Assets/_Project/Resources/ProyectoVrAudioBus.asset";
        const string AudioDir = "Assets/_Project/Audio/Generated";
        const string PrefabDir = "Assets/_Project/Prefabs/Interactables";

        [InitializeOnLoadMethod]
        static void AutoRunOnLoad() => EditorApplication.update += PollAutorun;

        static void PollAutorun()
        {
            if (!File.Exists(PendingFlag))
            {
                EditorApplication.update -= PollAutorun;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= PollAutorun;
            try
            {
                RunInternal(integrateMainScene: false);
                if (File.Exists(PendingFlag))
                    File.Delete(PendingFlag);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Agente3] Bootstrap falló (se reintentará con .agente3_pending): " + ex);
                EditorApplication.update += PollAutorun;
            }
        }

        [MenuItem("Vivienda/Agente3/Construir interacciones (prefabs + sandbox)")]
        public static void RunSandboxOnly() => RunInternal(integrateMainScene: false);

        [MenuItem("Vivienda/Agente3/Integrar en Vivienda_Prueba (requiere lock)")]
        public static void RunWithIntegration()
        {
            if (!CanIntegrateMainScene())
            {
                Debug.LogWarning("[Agente3] Lock activo (<30 min). Solo sandbox.");
                RunInternal(false);
                return;
            }

            AcquireLock();
            try
            {
                RunInternal(true);
            }
            finally
            {
                ReleaseLock();
            }
        }

        const double LockStaleMinutes = 30d;

        static bool CanIntegrateMainScene()
        {
            if (!File.Exists(LockPath))
                return false;

            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(LockPath);
            if (age.TotalMinutes < LockStaleMinutes)
                return false;

            var text = File.ReadAllText(LockPath);
            return text.Contains("Agente3", StringComparison.OrdinalIgnoreCase) || age.TotalMinutes >= LockStaleMinutes;
        }

        static void AcquireLock()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LockPath)!);
            File.WriteAllText(LockPath, $"Agente3|{DateTime.UtcNow:O}");
        }

        static void ReleaseLock()
        {
            if (File.Exists(LockPath))
            {
                var text = File.ReadAllText(LockPath);
                if (text.Contains("Agente3", StringComparison.OrdinalIgnoreCase))
                    File.Delete(LockPath);
            }
        }

        static void RunInternal(bool integrateMainScene)
        {
            var log = new StringBuilder();
            log.AppendLine($"# Agente 3 — interacciones ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
            log.AppendLine();

            EnsureFolders(log);
            var clips = GeneratePlaceholderAudio(log);
            var bus = EnsureAudioMixer(log);
            var doorPrefab = EnsureDoorPrefab(clips, bus, log);
            var windowPrefab = EnsureWindowPrefab(clips, bus, log);
            var switchPrefab = EnsureSwitchPrefab(clips, bus, log);
            var dayNightPrefab = EnsureDayNightPrefab(log);
            EnsureRoomCatalog(log);
            BuildSandboxScene(doorPrefab, windowPrefab, switchPrefab, dayNightPrefab, bus, log);

            if (integrateMainScene)
            {
                AcquireLock();
                try
                {
                    IntegrateMainScene(doorPrefab, windowPrefab, switchPrefab, dayNightPrefab, log);
                }
                finally
                {
                    ReleaseLock();
                }
            }
            else
            {
                log.AppendLine("- Integración escena principal: **omitida** (lock ajeno o modo sandbox).");
            }

            WriteDecisions(log);
            WriteLicensesIfMissing(log);
            AppendBlenderTasks(log);
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.WriteAllText(LogPath, log.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Agente3] Bootstrap completado. Ver Docs/Logs/agente3_interacciones.md");
        }

        static void EnsureFolders(StringBuilder log)
        {
            foreach (var dir in new[]
                     {
                         PrefabDir, AudioDir, "Assets/_Project/Scenes/_Sandbox",
                         "Assets/_Project/Docs/.locks", "Assets/_Project/Resources",
                         "Assets/_Project/Docs/Reports/captures"
                     })
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    log.AppendLine($"- Carpeta creada: `{dir}`");
                }
            }
        }

        static Dictionary<string, AudioClip> GeneratePlaceholderAudio(StringBuilder log)
        {
            var map = new Dictionary<string, AudioClip>();
            WriteToneWav($"{AudioDir}/sfx_latch.wav", 820f, 0.08f, 0.55f);
            WriteToneWav($"{AudioDir}/sfx_switch_click.wav", 2400f, 0.035f, 0.45f);
            WriteToneWav($"{AudioDir}/sfx_window_open.wav", 420f, 0.12f, 0.35f);
            WriteToneWav($"{AudioDir}/sfx_window_close.wav", 310f, 0.1f, 0.32f);
            WriteNoiseLoopWav($"{AudioDir}/amb_exterior_loop.wav", 6f, 0.08f);

            AssetDatabase.Refresh();
            map["latch"] = LoadClip("sfx_latch.wav");
            map["click"] = LoadClip("sfx_switch_click.wav");
            map["winOpen"] = LoadClip("sfx_window_open.wav");
            map["winClose"] = LoadClip("sfx_window_close.wav");
            map["amb"] = LoadClip("amb_exterior_loop.wav");
            log.AppendLine("- Audio placeholder WAV generado (senoidal/rudo procedural, dominio público).");
            return map;
        }

        static AudioClip LoadClip(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{fileName}");
        }

        static void WriteToneWav(string path, float frequency, float duration, float amplitude)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(1, (int)(sampleRate * duration));
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            WriteWavHeader(writer, sampleCount, sampleRate, 1);
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(1f - t / duration);
                var sample = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope;
                WriteSample(writer, sample);
            }
        }

        static void WriteNoiseLoopWav(string path, float duration, float amplitude)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(1, (int)(sampleRate * duration));
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            WriteWavHeader(writer, sampleCount, sampleRate, 1);
            var rng = new System.Random(3);
            for (var i = 0; i < sampleCount; i++)
            {
                var n = (float)(rng.NextDouble() * 2.0 - 1.0);
                var sample = n * amplitude;
                WriteSample(writer, sample);
            }
        }

        static void WriteWavHeader(BinaryWriter writer, int sampleCount, int sampleRate, short channels)
        {
            var byteRate = sampleRate * channels * 2;
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(sampleCount * 2);
        }

        static void WriteSample(BinaryWriter writer, float sample)
        {
            var s = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
            writer.Write(s);
        }

        static ProyectoVrAudioBus EnsureAudioMixer(StringBuilder log)
        {
            var mixer = AudioMixerUtil.EnsureMixer(MixerPath, log);
            var master = mixer.FindMatchingGroups("Master")[0];
            var sfx = AudioMixerUtil.EnsureGroup(mixer, master, "SFX");
            var amb = AudioMixerUtil.EnsureGroup(mixer, master, "Ambiente");
            var ui = AudioMixerUtil.EnsureGroup(mixer, master, "UI");
            AudioMixerUtil.EnsureExposedFloat(mixer, ProyectoVrAudioBus.ParamExtAmbience, -24f);

            var bus = AssetDatabase.LoadAssetAtPath<ProyectoVrAudioBus>(BusAssetPath);
            if (bus == null)
            {
                bus = ScriptableObject.CreateInstance<ProyectoVrAudioBus>();
                AssetDatabase.CreateAsset(bus, BusAssetPath);
            }

            bus.mixer = mixer;
            bus.sfxGroup = sfx;
            bus.ambienteGroup = amb;
            bus.uiGroup = ui;
            EditorUtility.SetDirty(bus);

            if (!File.Exists(ResourcesBusPath))
            {
                if (!AssetDatabase.CopyAsset(BusAssetPath, ResourcesBusPath))
                    AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(bus), ResourcesBusPath);
            }

            log.AppendLine("- Grupos mixer: Master / SFX / Ambiente / UI; expuesto `ExtAmbience`.");
            return bus;
        }

        static class AudioMixerUtil
        {
            static Type ControllerType => typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController");

            public static AudioMixer EnsureMixer(string path, StringBuilder log)
            {
                var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
                if (mixer != null)
                    return mixer;

                var create = ControllerType?.GetMethod("CreateNewAudioMixerControllerAtPath",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (create == null)
                    throw new InvalidOperationException("No se pudo crear AudioMixer (API editor).");

                mixer = create.Invoke(null, new object[] { path }) as AudioMixer;
                log.AppendLine($"- AudioMixer creado: `{path}`");
                return mixer;
            }

            public static AudioMixerGroup EnsureGroup(AudioMixer mixer, AudioMixerGroup parent, string name)
            {
                foreach (var g in mixer.FindMatchingGroups(name))
                {
                    if (g.name == name)
                        return g;
                }

                var controller = AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GetAssetPath(mixer));
                var createGroup = ControllerType?.GetMethod("CreateNewGroup",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (createGroup == null)
                    throw new InvalidOperationException("CreateNewGroup no disponible.");

                return createGroup.Invoke(controller, new object[] { name, false, false, false, 0f, new[] { parent } })
                    as AudioMixerGroup;
            }

            public static void EnsureExposedFloat(AudioMixer mixer, string param, float value)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GetAssetPath(mixer));
                var exposed = new List<string>();
                var getExposed = ControllerType?.GetMethod("GetExposedParameters",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                getExposed?.Invoke(controller, new object[] { exposed });
                if (exposed == null || !exposed.Contains(param))
                {
                    var add = ControllerType?.GetMethod("AddExposedParameter",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    add?.Invoke(controller, new object[] { param });
                }

                mixer.SetFloat(param, value);
            }
        }

        static GameObject EnsureDoorPrefab(Dictionary<string, AudioClip> clips, ProyectoVrAudioBus bus, StringBuilder log)
        {
            var path = $"{PrefabDir}/Puerta_Estandar_PROV.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                log.AppendLine("- Prefab puerta PROV ya existía (sin recrear).");
                return existing;
            }

            var root = new GameObject("Puerta_Estandar_PROV");
            var marco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marco.name = "Marco";
            marco.transform.SetParent(root.transform, false);
            marco.transform.localScale = new Vector3(0.95f, 2.1f, 0.08f);

            var hinge = new GameObject("HingePivot").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(-0.45f, 0f, 0f);

            var hoja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hoja.name = "Hoja";
            hoja.transform.SetParent(hinge, false);
            hoja.transform.localPosition = new Vector3(0.45f, 0f, 0f);
            hoja.transform.localScale = new Vector3(0.9f, 2.1f, 0.04f);

            var manija = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manija.name = "Manija";
            manija.transform.SetParent(hoja.transform, false);
            manija.transform.localPosition = new Vector3(0.32f, 0f, 0.06f);
            manija.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
            manija.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SetLayerRecursive(root, LayerMask.NameToLayer("Interactuable"));

            var rb = manija.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var grab = manija.AddComponent<XRGrabInteractable>();
            grab.movementType = XRGrabInteractable.MovementType.Kinematic;

            var latchSource = root.AddComponent<AudioSource>();
            latchSource.spatialBlend = 1f;
            latchSource.playOnAwake = false;
            bus.RouteToSfx(latchSource);

            var hingeScript = root.AddComponent<DoorHinge>();
            var so = new SerializedObject(hingeScript);
            so.FindProperty("hingePivot").objectReferenceValue = hinge;
            so.FindProperty("doorLeaf").objectReferenceValue = hoja.transform;
            so.FindProperty("handleGrab").objectReferenceValue = grab;
            so.FindProperty("latchAudio").objectReferenceValue = latchSource;
            so.FindProperty("latchClip").objectReferenceValue = clips["latch"];
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            log.AppendLine($"- Prefab **Puerta_Estandar_PROV** (0.9×2.1 m, bisagra, manija XR).");
            return prefab;
        }

        static GameObject EnsureWindowPrefab(Dictionary<string, AudioClip> clips, ProyectoVrAudioBus bus, StringBuilder log)
        {
            var path = $"{PrefabDir}/Ventana_Abatible_PROV.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                log.AppendLine("- Prefab ventana PROV ya existía.");
                return existing;
            }

            var root = new GameObject("Ventana_Abatible_PROV");
            var marco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marco.name = "Marco";
            marco.transform.SetParent(root.transform, false);
            marco.transform.localScale = new Vector3(1f, 2.2f, 0.06f);

            var pivot = new GameObject("HingePivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(-0.48f, 0f, 0f);

            var hoja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hoja.name = "Hoja_Cristal";
            hoja.transform.SetParent(pivot, false);
            hoja.transform.localPosition = new Vector3(0.48f, 0f, 0f);
            hoja.transform.localScale = new Vector3(0.96f, 2.1f, 0.02f);

            var manija = GameObject.CreatePrimitive(PrimitiveType.Cube);
            manija.name = "Manija";
            manija.transform.SetParent(hoja.transform, false);
            manija.transform.localPosition = new Vector3(0.35f, -0.2f, 0.03f);
            manija.transform.localScale = new Vector3(0.08f, 0.02f, 0.03f);
            SetLayerRecursive(root, LayerMask.NameToLayer("Interactuable"));

            var rb = manija.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var grab = manija.AddComponent<XRGrabInteractable>();
            grab.movementType = XRGrabInteractable.MovementType.Kinematic;
            grab.trackPosition = false;
            grab.trackRotation = false;

            var audio = root.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.playOnAwake = false;
            bus.RouteToSfx(audio);

            var window = root.AddComponent<WindowHinge>();
            var so = new SerializedObject(window);
            so.FindProperty("hingePivot").objectReferenceValue = pivot;
            so.FindProperty("handleGrab").objectReferenceValue = grab;
            so.FindProperty("openCloseAudio").objectReferenceValue = audio;
            so.FindProperty("openClip").objectReferenceValue = clips["winOpen"];
            so.FindProperty("closeClip").objectReferenceValue = clips["winClose"];
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            log.AppendLine("- Prefab **Ventana_Abatible_PROV** (bisagra lateral, no corrediza).");
            return prefab;
        }

        static GameObject EnsureSwitchPrefab(Dictionary<string, AudioClip> clips, ProyectoVrAudioBus bus, StringBuilder log)
        {
            var path = $"{PrefabDir}/Interruptor_Habitacion_PROV.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                log.AppendLine("- Prefab interruptor ya existía.");
                return existing;
            }

            var root = new GameObject("Interruptor_Habitacion_PROV");
            var placa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placa.name = "Placa";
            placa.transform.SetParent(root.transform, false);
            placa.transform.localScale = new Vector3(0.08f, 0.12f, 0.02f);

            var palanca = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palanca.name = "Palanca";
            palanca.transform.SetParent(root.transform, false);
            palanca.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            palanca.transform.localScale = new Vector3(0.03f, 0.06f, 0.02f);

            var interactable = placa.AddComponent<XRSimpleInteractable>();
            var clickSource = root.AddComponent<AudioSource>();
            clickSource.spatialBlend = 1f;
            clickSource.playOnAwake = false;
            bus.RouteToSfx(clickSource);

            var room = root.AddComponent<RoomLightGroup>();
            var lightGo = new GameObject("LuzRealtime");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.15f, 0.3f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f;
            light.intensity = 0.85f;
            light.shadows = LightShadows.None;

            var soRoom = new SerializedObject(room);
            soRoom.FindProperty("roomId").stringValue = "Sandbox";
            soRoom.FindProperty("controlledLights").InsertArrayElementAtIndex(0);
            soRoom.FindProperty("controlledLights").GetArrayElementAtIndex(0).objectReferenceValue = light;
            soRoom.ApplyModifiedPropertiesWithoutUndo();

            var sw = root.AddComponent<LightSwitch>();
            var so = new SerializedObject(sw);
            so.FindProperty("interactable").objectReferenceValue = interactable;
            so.FindProperty("switchMesh").objectReferenceValue = palanca.transform;
            so.FindProperty("roomLights").objectReferenceValue = room;
            so.FindProperty("clickAudio").objectReferenceValue = clickSource;
            so.FindProperty("clickClip").objectReferenceValue = clips["click"];
            so.ApplyModifiedPropertiesWithoutUndo();

            SetLayerRecursive(root, LayerMask.NameToLayer("Interactuable"));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            log.AppendLine("- Prefab **Interruptor_Habitacion_PROV** + RoomLightGroup (1 luz realtime).");
            return prefab;
        }

        static void EnsureRoomCatalog(StringBuilder log)
        {
            const string path = "Assets/_Project/Audio/InteractablesRoomCatalog.asset";
            if (AssetDatabase.LoadAssetAtPath<InteractablesRoomCatalog>(path) != null)
            {
                log.AppendLine("- Catálogo de habitaciones ya existía.");
                return;
            }

            var catalog = ScriptableObject.CreateInstance<InteractablesRoomCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
            log.AppendLine("- Catálogo `InteractablesRoomCatalog` con habitaciones sin cablear.");
        }

        static GameObject EnsureDayNightPrefab(StringBuilder log)
        {
            var path = $"{PrefabDir}/DayNight_Rig.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                log.AppendLine("- Prefab DayNight_Rig ya existía.");
                return existing;
            }

            var root = new GameObject("DayNight_Rig");
            var sun = new GameObject("Directional Sun");
            sun.transform.SetParent(root.transform, false);
            sun.transform.rotation = Quaternion.Euler(50f, 170f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;

            var ext = new GameObject("ExteriorNightLight");
            ext.transform.SetParent(root.transform, false);
            ext.transform.localPosition = new Vector3(2f, 2.5f, 2f);
            var extLight = ext.AddComponent<Light>();
            extLight.type = LightType.Point;
            extLight.range = 8f;
            extLight.intensity = 1.2f;
            extLight.enabled = false;

            var ctrl = root.AddComponent<DayNightController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("directionalLight").objectReferenceValue = light;
            so.FindProperty("exteriorNightLights").InsertArrayElementAtIndex(0);
            so.FindProperty("exteriorNightLights").GetArrayElementAtIndex(0).objectReferenceValue = extLight;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            log.AppendLine("- Prefab **DayNight_Rig** (sol + luz exterior nocturna).");
            return prefab;
        }

        static void BuildSandboxScene(GameObject door, GameObject window, GameObject sw, GameObject dayNight,
            ProyectoVrAudioBus bus, StringBuilder log)
        {
            var scene = File.Exists(SandboxScene)
                ? EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            if (!File.Exists(SandboxScene))
                Directory.CreateDirectory(Path.GetDirectoryName(SandboxScene)!);

            var rigRoot = GameObject.Find("DayNight_Rig") ?? (GameObject)PrefabUtility.InstantiatePrefab(dayNight);
            rigRoot.name = "DayNight_Rig";

            var loader = rigRoot.GetComponent<ProyectoVrAudioBusLoader>() ??
                         rigRoot.AddComponent<ProyectoVrAudioBusLoader>();
            loader.bus = bus;

            var ambGo = GameObject.Find("AmbienteExterior") ?? new GameObject("AmbienteExterior");
            var ambSource = ambGo.GetComponent<AudioSource>() ?? ambGo.AddComponent<AudioSource>();
            ambSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/amb_exterior_loop.wav");
            ambSource.loop = true;
            ambSource.spatialBlend = 0f;
            ambSource.volume = 0.35f;
            bus.RouteToAmbiente(ambSource);
            if (!ambSource.isPlaying)
                ambSource.Play();

            PlacePrefab(door, new Vector3(0f, 1.05f, 2f), "Puerta_Demo");
            PlacePrefab(window, new Vector3(2.2f, 1.1f, 2f), "Ventana_Demo");
            PlacePrefab(sw, new Vector3(-1.2f, 1.4f, 2f), "Interruptor_Demo");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "SandboxFloor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            EditorSceneManager.SaveScene(scene, SandboxScene);
            log.AppendLine($"- Escena sandbox guardada: `{SandboxScene}`");
        }

        static void IntegrateMainScene(GameObject door, GameObject window, GameObject sw, GameObject dayNight,
            StringBuilder log)
        {
            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            if (GameObject.Find("Agente3_Interacciones") == null)
            {
                var anchor = new GameObject("Agente3_Interacciones");
                PlacePrefabUnder(door, anchor.transform, new Vector3(0f, 0f, 0f), "Puerta_PROV_Placed");
                PlacePrefabUnder(window, anchor.transform, new Vector3(2.5f, 1.1f, 0f), "Ventana_PROV_Placed");
                PlacePrefabUnder(sw, anchor.transform, new Vector3(-2f, 1.4f, 0.5f), "Interruptor_PROV_Placed");
                if (GameObject.Find("DayNight_Rig") == null)
                {
                    var rig = (GameObject)PrefabUtility.InstantiatePrefab(dayNight);
                    rig.name = "DayNight_Rig";
                }

                log.AppendLine("- Integración en `Vivienda_Prueba`: ancla `Agente3_Interacciones` + DayNight_Rig.");
            }
            else
            {
                log.AppendLine("- Integración: ancla Agente3 ya presente (sin duplicar).");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void PlacePrefab(GameObject prefab, Vector3 pos, string name)
        {
            if (GameObject.Find(name) != null)
                return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
        }

        static void PlacePrefabUnder(GameObject prefab, Transform parent, Vector3 localPos, string name)
        {
            if (parent.Find(name) != null)
                return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            if (layer < 0)
                return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        static void WriteDecisions(StringBuilder log)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Decisiones — Agente 3 (Interacciones)");
            sb.AppendLine();
            sb.AppendLine("| Tema | Decisión |");
            sb.AppendLine("|------|----------|");
            sb.AppendLine("| Puerta estándar | 0.9 × 2.1 m, bisagra 0–110°, cierre magnético 3° |");
            sb.AppendLine("| Ventana | **Abatible** (bisagra lateral); corrediza descartada por mallas fusionadas en FBX |");
            sb.AppendLine("| Interruptor | Select XR (Trigger en perfil Quest); palanca ±18° |");
            sb.AppendLine("| Luces habitación | Máx. 1 realtime por prefab demo; resto baked/off |");
            sb.AppendLine("| Día/noche | Blend 4.5 s (rango 3–6), sol + ambiente + skybox tint + luces exteriores |");
            sb.AppendLine("| Audio | Mixer SFX / Ambiente / UI; `ExtAmbience` sube con ventanas abiertas |");
            sb.AppendLine();
            sb.AppendLine($"Actualizado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Directory.CreateDirectory(Path.GetDirectoryName(DecisionsPath)!);
            File.WriteAllText(DecisionsPath, sb.ToString());
            log.AppendLine("- `Docs/DECISIONES.md` actualizado.");
        }

        static void WriteLicensesIfMissing(StringBuilder log)
        {
            if (File.Exists(LicensesPath))
            {
                log.AppendLine("- `Docs/Licenses.md` ya existía.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Licencias de audio — Agente 3");
            sb.AppendLine();
            sb.AppendLine("| Asset | Origen | Licencia |");
            sb.AppendLine("|-------|--------|----------|");
            sb.AppendLine("| `Assets/_Project/Audio/Generated/*.wav` | Generados proceduralmente en editor (seno/ruido) | Uso libre / dominio público |");
            sb.AppendLine();
            File.WriteAllText(LicensesPath, sb.ToString());
            log.AppendLine("- `Docs/Licenses.md` creado.");
        }

        static void AppendBlenderTasks(StringBuilder log)
        {
            const string blenderPath = "Assets/_Project/Docs/Reports/tareas_para_blender.md";
            if (!File.Exists(blenderPath))
                return;

            var text = File.ReadAllText(blenderPath);
            const string marker = "## Agente 3 — puertas fusionadas";
            if (text.Contains(marker))
            {
                log.AppendLine("- Blender tasks: entrada Agente 3 ya presente.");
                return;
            }

            text += "\n\n" + marker + "\n\n";
            text += "- Usar prefab `Puerta_Estandar_PROV` hasta re-export con Marco/Hoja/Manija separados.\n";
            text += "- Reemplazar `_PROV` por instancias ancladas a `door-trustile_*` tras separación en Blender.\n";
            File.WriteAllText(blenderPath, text);
            log.AppendLine("- Añadida nota en `tareas_para_blender.md` (puertas PROV).");
        }
    }
}
#endif
