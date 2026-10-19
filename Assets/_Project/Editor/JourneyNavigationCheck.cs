using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProyectoVR;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class JourneyNavigationCheck
{
    [Serializable] public class Route
    {
        public string room;
        public string status;
        public Vector3 requestedTarget;
        public Vector3 sampledTarget;
        public Vector3[] corners;
        public string bodyStatus;
        public Vector3 bodyFinal;
        public int bodySteps;
    }
    [Serializable] public class Routes
    {
        public string scene;
        public Vector3 spawn;
        public int total;
        public int complete;
        public int bodyComplete;
        public Route[] routes;
    }

    [MenuItem("Vivienda/Journey/Comprobar rutas")]
    public static void Check()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de comprobar rutas.");
        Directory.CreateDirectory("Temp");
        var report = new StringBuilder("Rutas con puertas abiertas; colision fisica estructural.\n");
        var routes = new List<Route>();
        var result = new Routes { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
        var doorPoses = new Dictionary<Transform, Quaternion>();
        Transform body = null;
        Vector3 bodyPosition = default;
        Quaternion bodyRotation = default;
        CharacterController controller = null;
        bool controllerEnabled = false;
        NavMeshData data = null;
        NavMeshDataInstance instance = default;
        try
        {
            Physics.SyncTransforms();
            if (NavMesh.GetSettingsCount() == 0) throw new InvalidOperationException("No existe configuracion de agente NavMesh.");
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = .3f;
            settings.agentHeight = 1.7f;
            settings.agentClimb = .4f;
            settings.agentSlope = 45;
            settings.overrideVoxelSize = true;
            settings.voxelSize = .05f;
            settings.overrideTileSize = true;
            settings.tileSize = 128;
            settings.minRegionArea = 0;
            var ignored = new HashSet<Transform>();
            var rig = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (rig != null) ignored.Add(rig.transform);
            foreach (var door in UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None))
            {
                if (!door.enabled) continue;
                var serialized = new SerializedObject(door);
                var pivot = serialized.FindProperty("hingePivot").objectReferenceValue as Transform;
                if (pivot == null) continue;
                doorPoses[pivot] = pivot.localRotation;
                float angle = serialized.FindProperty("maxAngle").floatValue - serialized.FindProperty("minAngle").floatValue;
                bool positive = serialized.FindProperty("hingeOnPositiveY").boolValue;
                pivot.localRotation *= Quaternion.AngleAxis(angle, positive ? Vector3.up : Vector3.down);
            }
            Physics.SyncTransforms();
            // Bake the same physical state used by the capsule: doors open, windows closed.
            var markups = ignored.Select(t => new NavMeshBuildMarkup { root = t, ignoreFromBuild = true }).ToList();
            var bounds = new Bounds(new Vector3(0, 3, 2), new Vector3(55, 12, 50));
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("No se pudo construir NavMesh temporal.");
            instance = NavMesh.AddNavMeshData(data);
            if (!instance.valid) throw new InvalidOperationException("No se pudo registrar NavMesh temporal.");
            var spawn = GameObject.Find("PlayerSpawn");
            if (spawn == null) throw new InvalidOperationException("Falta PlayerSpawn.");
            result.spawn = spawn.transform.position;
            var filter = new NavMeshQueryFilter { agentTypeID = settings.agentTypeID, areaMask = NavMesh.AllAreas };
            bool startFound = NavMesh.SamplePosition(result.spawn, out var start, .75f, filter);
            report.AppendLine("Sources=" + sources.Count + "; ignored rig roots=" + ignored.Count + "; start sampled=" + startFound);
            report.AppendLine("Spawn=" + Vector(result.spawn) + "; nav=" + Vector(start.position));
            var rooms = UnityEngine.Object.FindObjectsByType<RoomLightGroup>(FindObjectsSortMode.None).OrderBy(r => r.name).ToArray();
            result.total = rooms.Length;
            foreach (var room in rooms)
            {
                var route = new Route { room = room.name, requestedTarget = room.transform.position, corners = Array.Empty<Vector3>() };
                bool endFound = NavMesh.SamplePosition(route.requestedTarget, out var end, .75f, filter);
                route.sampledTarget = end.position;
                if (!startFound) route.status = "StartNotOnNavMesh";
                else if (!endFound) route.status = "RoomNotOnNavMesh";
                else if (Mathf.Abs(end.position.y - route.requestedTarget.y) > .4f) route.status = "WrongFloor";
                else
                {
                    var path = new NavMeshPath();
                    bool calculated = NavMesh.CalculatePath(start.position, end.position, filter, path);
                    route.corners = path.corners;
                    route.status = calculated ? path.status.ToString() : "CalculatePathFailed";
                    if (calculated && path.status == NavMeshPathStatus.PathComplete) result.complete++;
                }
                routes.Add(route);
                report.AppendLine(route.room + "\t" + route.status + "\ttarget=" + Vector(route.requestedTarget) + "\tsample=" + Vector(route.sampledTarget) + "\tcorners=" + route.corners.Length);
            }
            report.AppendLine("Complete=" + result.complete + "/" + result.total + "; expected rooms=12");
            if (rig == null) throw new InvalidOperationException("Falta XROrigin para prueba fisica.");
            body = rig.Origin != null ? rig.Origin.transform : rig.transform;
            controller = body.GetComponent<CharacterController>();
            if (controller == null) throw new InvalidOperationException("Falta CharacterController para prueba fisica.");
            bodyPosition = body.position;
            bodyRotation = body.rotation;
            controllerEnabled = controller.enabled;
            foreach (var route in routes)
            {
                if (route.status != NavMeshPathStatus.PathComplete.ToString()) { route.bodyStatus = "NoCompleteRoute"; report.AppendLine("BODY\t" + route.room + "\t" + route.bodyStatus); continue; }
                controller.enabled = false;
                body.position = result.spawn + Vector3.up * .03f;
                Physics.SyncTransforms();
                controller.enabled = true;
                // The same native capsule performs every step. Doors physically rotate; glass stays solid.
                int corner = 0;
                int blockedSteps = 0;
                const float step = .028f; // 1.4 m/s at 50 Hz, plus grounded downward velocity.
                for (; route.bodySteps < 20000 && corner < route.corners.Length; route.bodySteps++)
                {
                    var delta = route.corners[corner] - body.position;
                    var horizontal = Vector3.ProjectOnPlane(delta, Vector3.up);
                    if (horizontal.magnitude < .10f && Mathf.Abs(delta.y) < .45f) { corner++; blockedSteps = 0; continue; }
                    var movement = Vector3.ClampMagnitude(horizontal, step) + Vector3.down * .04f;
                    var before = body.position;
                    controller.Move(movement);
                    Physics.SyncTransforms();
                    float progress = Vector3.ProjectOnPlane(body.position - before, Vector3.up).magnitude;
                    blockedSteps = progress < .0005f ? blockedSteps + 1 : 0;
                    if (blockedSteps >= 75) break;
                }
                route.bodyFinal = body.position;
                float finalPlanarDistance = Vector3.ProjectOnPlane(body.position - route.sampledTarget, Vector3.up).magnitude;
                route.bodyStatus = corner == route.corners.Length && finalPlanarDistance < .18f && Mathf.Abs(body.position.y - route.sampledTarget.y) < .45f ? "BodyComplete" : "BodyBlockedAtCorner" + corner;
                if (route.bodyStatus == "BodyComplete") result.bodyComplete++;
                report.AppendLine("BODY\t" + route.room + "\t" + route.bodyStatus + "\tsteps=" + route.bodySteps + "\tfinal=" + Vector(route.bodyFinal) + "\tdistance=" + finalPlanarDistance.ToString("F3", CultureInfo.InvariantCulture));
            }
            report.AppendLine("CharacterController complete=" + result.bodyComplete + "/" + result.total);
            report.AppendLine("NavMesh comprueba conectividad; BODY realiza movimiento fisico en Editor con puertas abiertas y ventanas cerradas. Visor no probado.");
        }
        catch (Exception error)
        {
            report.AppendLine("ERROR=" + error);
            Debug.LogError(error);
        }
        finally
        {
            if (controller != null && body != null)
            {
                controller.enabled = false;
                body.SetPositionAndRotation(bodyPosition, bodyRotation);
                Physics.SyncTransforms();
                controller.enabled = controllerEnabled;
            }
            foreach (var pose in doorPoses) if (pose.Key != null) pose.Key.localRotation = pose.Value;
            Physics.SyncTransforms();
            if (instance.valid) instance.Remove();
            if (data != null) UnityEngine.Object.DestroyImmediate(data);
            result.routes = routes.ToArray();
            File.WriteAllText("Temp/JourneyNavigation.txt", report.ToString());
            File.WriteAllText("Temp/JourneyRoutes.json", JsonUtility.ToJson(result, true));
            var tsv = new StringBuilder("room\tstatus\tcorner\tx\ty\tz\n");
            foreach (var route in routes)
                for (int i = 0; i < route.corners.Length; i++)
                    tsv.AppendLine(route.room + "\t" + route.status + "\t" + i + "\t" + Vector(route.corners[i], "\t"));
            File.WriteAllText("Temp/JourneyRoutes.tsv", tsv.ToString());
            Debug.Log(report.ToString());
        }
    }

    static string Vector(Vector3 p, string separator = ",") => p.x.ToString("F3", CultureInfo.InvariantCulture) + separator + p.y.ToString("F3", CultureInfo.InvariantCulture) + separator + p.z.ToString("F3", CultureInfo.InvariantCulture);
}
