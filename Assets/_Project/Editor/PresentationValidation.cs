using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ProyectoVR;
using ProyectoVR.UI;

[InitializeOnLoad]
public static class PresentationValidation
{
    const string Flag = "Vivienda.PresentationValidation";
    static int stage, errors;
    static float next;
    static readonly StringBuilder Report = new();
    static PresentationValidation() => EditorApplication.playModeStateChanged += State;

    [MenuItem("Vivienda/Graficos/Comprobar presentacion en Play")]
    public static void Begin()
    {
        if (Lightmapping.isRunning || EditorApplication.isPlaying) throw new InvalidOperationException("Editor ocupado.");
        SessionState.SetBool(Flag, true); EditorApplication.EnterPlaymode();
    }

    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Flag, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            stage = errors = 0; next = 2; Report.Clear();
            Application.logMessageReceived += Log; EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Flag, false); Application.logMessageReceived -= Log; EditorApplication.update -= Tick;
        }
    }

    static void Log(string message, string trace, LogType type)
    {
        if (type is LogType.Error or LogType.Exception or LogType.Assert) { errors++; Report.AppendLine(type + ": " + message); }
    }

    static void Check(bool pass, string message) => Report.AppendLine((pass ? "PASS " : "FAIL ") + message);

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.time < next) return;
        var baked = UnityEngine.Object.FindAnyObjectByType<BakedDayNightLighting>();
        switch (stage++)
        {
            case 0:
                VrWorldMenuController.Instance.SetVisible(false);
                Check(baked != null && baked.day.color.Length > 0 && baked.night.color.Length > 0, "Day/night baked data present");
                Check(!baked.IsNight && LightmapSettings.lightmaps[0].lightmapColor == baked.day.color[0], "Day native lightmaps assigned");
                Report.AppendLine($"Day maps={baked.day.color.Length}; Night maps={baked.night.color.Length}; Receivers={baked.receivers.Length}; Probes={LightmapSettings.lightProbes.count}");
                foreach (string id in new[] { "718428", "693623", "718694" })
                {
                    var wall = UnityEngine.Object.FindObjectsByType<MeshRenderer>().First(r => r.name.Contains("[" + id + "]"));
                    Check(wall.sharedMaterials.Length == wall.GetComponent<MeshFilter>().sharedMesh.subMeshCount && wall.sharedMaterials.Length > 1, "Source material partitions " + id);
                }
                foreach (string name in new[] { "TP_Piso1", "TP_Piso2" })
                {
                    var p = GameObject.Find(name).transform.position;
                    var blocked = Physics.OverlapCapsule(p + Vector3.up * .3f, p + Vector3.up * 1.5f, .23f, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c.transform.IsChildOf(GameObject.Find("Presentacion arquitectonica").transform)).ToArray();
                    Check(blocked.Length == 0, name + " clear of furniture collisions");
                }
                Shot("Presentacion_Dia_Fachada", false);
                Shot("Fachada_Acabados_Dia", false);
                Shot("Presentacion_Dia_Interior", true);
                Shot("Muebles_Dia_Cocina", true);
                foreach (var room in UnityEngine.Object.FindObjectsByType<RoomLightGroup>())
                    if (room.name.Contains("Sala") || room.name.Contains("Cocina")) room.SetLightsOn(true);
                DayNightController.Instance.StartNight(); next = Time.time + 4;
                break;
            case 1:
                Check(baked.IsNight && LightmapSettings.lightmaps[0].lightmapColor == baked.night.color[0], "Night native lightmaps assigned after environment event");
                Check(UnityEngine.Object.FindObjectsByType<ReflectionProbe>().All(p => p.customBakedTexture != null && p.customBakedTexture.name.Contains("Noche")), "Night reflection cubemaps assigned");
                Shot("Fachada_Acabados_Noche", false); Shot("Presentacion_Noche_Fachada", false); Shot("Presentacion_Noche_Interior", true);
                Shot("Muebles_Noche_Cocina", true);
                DayNightController.Instance.StartDay(); next = Time.time + 4;
                break;
            default:
                Check(!baked.IsNight && LightmapSettings.lightmaps[0].lightmapColor == baked.day.color[0], "Return to day restores native maps");
                Report.AppendLine("Play errors=" + errors);
                Directory.CreateDirectory("Validation"); File.WriteAllText("Validation/Presentation-Runtime.txt", Report.ToString());
                EditorApplication.update -= Tick; EditorApplication.ExitPlaymode();
                break;
        }
    }

    static void Shot(string name, bool interior)
    {
        var source = Camera.main; var go = new GameObject("Temporary presentation camera");
        var camera = go.AddComponent<Camera>(); camera.CopyFrom(source);
        camera.usePhysicalProperties = true; camera.sensorSize = new Vector2(36, 27); camera.focalLength = interior ? 22 : 46;
        camera.cullingMask = ~(1 << 5);
        var position = interior ? new Vector3(2.5f, 1.55f, 2.35f) : new Vector3(-9, 4.6f, 22);
        var target = interior ? new Vector3(3.6f, 1.15f, -.4f) : new Vector3(1.5f, 3, 2.5f);
        if (name.Contains("Cocina"))
        {
            position = new Vector3(2.2f, 4.5f, .35f); target = new Vector3(4.05f, 4.75f, -3.15f);
            camera.focalLength = 24;
        }
        if (name.StartsWith("Fachada_Acabados"))
        {
            position = new Vector3(-8, 6, 10); target = new Vector3(0, 3.7f, .25f); camera.focalLength = 48;
        }
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.allowXRRendering = false;
        camera.enabled = false;
        var lines = UnityEngine.Object.FindObjectsByType<LineRenderer>().Where(line => line.enabled).ToArray();
        foreach (var line in lines) line.enabled = false;
        var rt = new RenderTexture(1280, 960, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        rt.Create(); camera.targetTexture = rt; var previous = RenderTexture.active;
        try
        {
            camera.Render(); RenderTexture.active = rt;
            var pixels = new Texture2D(1280, 960, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0); pixels.Apply();
            Directory.CreateDirectory("Captures"); File.WriteAllBytes("Captures/" + name + ".png", pixels.EncodeToPNG());
            UnityEngine.Object.Destroy(pixels);
        }
        finally
        {
            foreach (var line in lines) if (line != null) line.enabled = true;
            RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
            UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go);
        }
    }
}
