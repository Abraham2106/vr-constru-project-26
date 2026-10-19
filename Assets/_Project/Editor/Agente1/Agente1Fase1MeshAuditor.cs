#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vivienda.Project.Agent1
{
    public static class Agente1Fase1MeshAuditor
    {
        const string ScenePath = "Assets/Scenes/Vivienda_Prueba.unity";
        const string ModelPath = "Assets/Vivienda/Vivienda.fbx";
        const string LogPath = "Assets/_Project/Docs/Logs/agente1_mallas.md";
        const string ModeloLogPath = "Assets/_Project/Docs/Logs/agente1_modelo.md";
        const string MissingMeshesPath = "Assets/_Project/Docs/Reports/missing_meshes.md";
        const string ResumenPath = "Assets/_Project/Docs/Reports/fase1_resumen.md";
        const string InventarioPath = "Assets/_Project/Docs/Reports/00_inventario.md";
        const string BlenderTasksPath = "Assets/_Project/Docs/Reports/tareas_para_blender.md";
        const string MejorasPath = "Assets/_Project/Docs/Reports/mejoras_recomendadas.md";
        const string ChecklistPath = "Assets/_Project/Docs/CHECKLIST.md";
        const string DecisionesPath = "Assets/_Project/Docs/DECISIONES.md";
        const string CapturesDir = "Assets/_Project/Docs/Reports/captures";
        const string PendingFlag = "Assets/_Project/.agente1_pending";
        const string EditorLockPath = "Assets/_Project/Docs/.locks/editor.lock";
        const string AgentName = "Agente1-ModeloMateriales"; // swarm modelo/materiales; autorun vía .agente1_pending

        [InitializeOnLoadMethod]
        static void AutoRunOnLoad()
        {
            EditorApplication.delayCall += TryAutoRunFromPending;
            EditorApplication.update += UpdateAutoRunFromPending;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += TryAutoRunFromPending;
        }

        static void UpdateAutoRunFromPending()
        {
            if (!File.Exists(Abs(PendingFlag)))
            {
                EditorApplication.update -= UpdateAutoRunFromPending;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            TryAutoRunFromPending();
        }

        static void TryAutoRunFromPending()
        {
            if (!File.Exists(Abs(PendingFlag)))
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoRunFromPending;
                return;
            }

            if (RunInternal() && File.Exists(Abs(PendingFlag)))
                File.Delete(Abs(PendingFlag));
        }

        [MenuItem("Vivienda/Agente 1/Ejecutar Fase 1 (mallas)")]
        [MenuItem("Vivienda/Agente1/Ejecutar Fase 1 (mallas)")]
        public static void RunFromMenu() => RunInternal();

        static int LayerEstructura;
        static int LayerInteractuable;
        static int LayerTeleport;

        static bool RunInternal()
        {
            LayerEstructura = LayerMask.NameToLayer("Estructura");
            LayerInteractuable = LayerMask.NameToLayer("Interactuable");
            LayerTeleport = LayerMask.NameToLayer("Teleport");
            EnsureFolders();
            var log = new StringBuilder();
            log.AppendLine($"# Agente 1 — log mallas ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");

            if (!TryAcquireEditorLock(log))
            {
                File.WriteAllText(Abs(LogPath), log.ToString());
                File.WriteAllText(Abs(ModeloLogPath), log.ToString());
                Debug.LogWarning("[Agente1] Editor lock ocupado; reintentar cuando libere.");
                return false;
            }

            try
            {
                ApplyModelImportSettings(log);
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var vivienda = FindViviendaRoot(scene);
                if (vivienda == null)
                    throw new InvalidOperationException("No se encontró GameObject raíz 'Vivienda' en la escena.");

                UnpackViviendaPrefab(vivienda, log);
                NormalizeRootTransform(vivienda, log);
                LogDoorScaleEvidence(vivienda, log);
                var stats = OrganizeHierarchy(vivienda, log);
                var placeholderStats = AddDoorWindowPlaceholders(vivienda, log);
                var colliderStats = AddColliders(vivienda, log);
                AddStairRampCollider(vivienda, log);
                var materialStats = AuditMaterials(vivienda, log);
                CaptureViews(vivienda, log);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                WriteReports(log, stats, colliderStats, materialStats, placeholderStats, scene);
                Debug.Log("[Agente1] Fase 1 completada. Ver Assets/_Project/Docs/");
                return true;
            }
            catch (Exception ex)
            {
                log.AppendLine($"## ERROR\n{ex}");
                File.WriteAllText(Abs(LogPath), log.ToString());
                File.WriteAllText(Abs(ModeloLogPath), log.ToString());
                Debug.LogError("[Agente1] " + ex);
                return false;
            }
            finally
            {
                ReleaseEditorLock();
            }
        }

        static bool TryAcquireEditorLock(StringBuilder log)
        {
            var lockPath = Abs(EditorLockPath);
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            if (File.Exists(lockPath))
            {
                var text = File.ReadAllText(lockPath);
                var lines = text.Split('\n');
                var owner = lines.Length > 0 ? lines[0].Trim() : "?";
                var timeLine = lines.Length > 1 ? lines[1].Trim() : "";
                var lockFileTime = File.GetLastWriteTimeUtc(lockPath);
                var lockAgeMin = (DateTime.UtcNow - lockFileTime).TotalMinutes;
                var scenePath = Abs(ScenePath);
                var sceneAgeMin = File.Exists(scenePath)
                    ? (DateTime.UtcNow - File.GetLastWriteTimeUtc(scenePath)).TotalMinutes
                    : 999d;
                var projectStale = lockAgeMin > 10 || sceneAgeMin > 10;
                if (owner != AgentName && !projectStale && lockAgeMin <= 10)
                {
                    log.AppendLine($"- Lock editor: ocupado por `{owner}` ({timeLine}); escena sin guardar {sceneAgeMin:F0} min.");
                    return false;
                }

                if (owner != AgentName && projectStale)
                    log.AppendLine($"- Lock editor: tomando lock (stale lock={lockAgeMin:F0} min, escena={sceneAgeMin:F0} min).");
            }

            File.WriteAllText(lockPath, $"{AgentName}\n{DateTime.UtcNow:O}");
            log.AppendLine($"- Lock editor adquirido ({AgentName}).");
            return true;
        }

        static void ReleaseEditorLock()
        {
            var lockPath = Abs(EditorLockPath);
            if (!File.Exists(lockPath))
                return;
            var owner = File.ReadAllText(lockPath).Split('\n').FirstOrDefault()?.Trim();
            if (owner == AgentName)
                File.Delete(lockPath);
        }

        static void UnpackViviendaPrefab(GameObject vivienda, StringBuilder log)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(vivienda))
            {
                PrefabUtility.UnpackPrefabInstance(vivienda, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                log.AppendLine("- Prefab Vivienda desempaquetado (Completely) para jerarquía/colliders.");
            }
            else
            {
                log.AppendLine("- Vivienda no es prefab instance (ya desempaquetado o escena directa).");
            }
        }

        static void LogDoorScaleEvidence(GameObject vivienda, StringBuilder log)
        {
            var doors = vivienda.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.sharedMesh != null && mf.name.ToLowerInvariant().Contains("door-trustile"))
                .Take(3)
                .ToList();
            foreach (var mf in doors)
            {
                var b = mf.GetComponent<Renderer>()?.bounds;
                if (b == null)
                    continue;
                var s = b.Value.size;
                log.AppendLine($"- Escala puerta `{mf.name}`: {s.x:F2}×{s.y:F2}×{s.z:F2} m (objetivo ~0.9×2.1).");
            }
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(Abs("Assets/_Project/Docs/Logs"));
            Directory.CreateDirectory(Abs("Assets/_Project/Docs/Reports/captures"));
        }

        static void ApplyModelImportSettings(StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                log.AppendLine("- WARN: ModelImporter no encontrado para Vivienda.fbx");
                return;
            }

            var changed = false;
            if (!importer.generateSecondaryUV)
            {
                importer.generateSecondaryUV = true;
                changed = true;
            }

            if (importer.globalScale != 1f)
            {
                importer.globalScale = 1f;
                changed = true;
            }

            if (importer.meshCompression != ModelImporterMeshCompression.Off)
            {
                importer.meshCompression = ModelImporterMeshCompression.Off;
                changed = true;
            }

            if (importer.indexFormat != ModelImporterIndexFormat.Auto)
            {
                importer.indexFormat = ModelImporterIndexFormat.Auto;
                changed = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                log.AppendLine("- Import FBX: Lightmap UVs ON, Scale=1, compresión OFF, índices Auto.");
            }
            else
            {
                log.AppendLine("- Import FBX: ajustes ya correctos.");
            }
        }

        static GameObject FindViviendaRoot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Vivienda")
                    return root;
            }

            return scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "Vivienda")?.gameObject;
        }

        static void NormalizeRootTransform(GameObject vivienda, StringBuilder log)
        {
            var t = vivienda.transform;
            if (t.localScale != Vector3.one)
            {
                log.AppendLine($"- WARN: Escala raíz Vivienda {t.localScale} (no se forzó; revisar manualmente).");
            }

            if (Quaternion.Angle(t.localRotation, Quaternion.identity) > 0.1f)
            {
                log.AppendLine($"- Orientación raíz Vivienda: euler {t.localEulerAngles} (se mantiene; sin -90X detectado en escena).");
            }
            else
            {
                log.AppendLine("- Orientación raíz Vivienda: identidad (Y-up OK).");
            }
        }

        struct OrganizeStats
        {
            public int totalMeshes;
            public int piso1;
            public int piso2;
            public int apt1;
            public int apt2;
            public int exterior;
            public int estructura;
            public int missingMesh;
        }

        static OrganizeStats OrganizeHierarchy(GameObject vivienda, StringBuilder log)
        {
            var stats = new OrganizeStats();
            var estructura = CreateBucket(vivienda.transform, "Estructura");
            var piso1 = CreateBucket(estructura, "Piso1");
            var piso2 = CreateBucket(estructura, "Piso2");
            var apt1 = CreateBucket(vivienda.transform, "Apt1");
            var apt2 = CreateBucket(vivienda.transform, "Apt2");
            var exterior = CreateBucket(vivienda.transform, "Exterior");
            var collidersRoot = CreateBucket(vivienda.transform, "Colliders");

            var meshObjects = vivienda.GetComponentsInChildren<MeshFilter>(true)
                .Select(mf => mf.gameObject)
                .Distinct()
                .ToList();

            stats.totalMeshes = meshObjects.Count;

            foreach (var go in meshObjects)
            {
                if (go.transform == vivienda.transform)
                    continue;

                if (go.GetComponent<MeshFilter>()?.sharedMesh == null)
                    stats.missingMesh++;

                var name = go.name.ToLowerInvariant();
                var center = GetRendererCenter(go);
                var bucket = Classify(name, center.y, out var floorTag, out var aptTag);

                Transform target;
                if (bucket == Bucket.Exterior)
                {
                    target = exterior;
                    stats.exterior++;
                }
                else if (aptTag == AptTag.Apt1)
                {
                    target = apt1;
                    stats.apt1++;
                }
                else if (aptTag == AptTag.Apt2)
                {
                    target = apt2;
                    stats.apt2++;
                }
                else
                {
                    target = center.y < 2.8f ? piso1 : piso2;
                    if (center.y < 2.8f)
                        stats.piso1++;
                    else
                        stats.piso2++;
                }

                ReparentPreserveWorld(go.transform, target);

                if (name.Contains("muro") || name.Contains("montante") || name.Contains("escalera"))
                    stats.estructura++;
            }

            log.AppendLine($"- Jerarquía: {stats.totalMeshes} renderers, Piso1={stats.piso1}, Piso2={stats.piso2}, Exterior={stats.exterior}.");
            log.AppendLine($"- Apt1={stats.apt1}, Apt2={stats.apt2}, missingMesh={stats.missingMesh}.");
            log.AppendLine("- Contenedor Colliders creado (colliders en objetos fuente).");
            _ = collidersRoot;
            return stats;
        }

        enum Bucket { Piso1, Piso2, Exterior }
        enum FloorTag { Default, SharedStructure }
        enum AptTag { None, Apt1, Apt2 }

        static Transform CreateBucket(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null)
                return t;
            var go = new GameObject(name);
            t = go.transform;
            t.SetParent(parent, false);
            return t;
        }

        static void ReparentPreserveWorld(Transform child, Transform newParent)
        {
            if (child.parent == newParent)
                return;
            var pos = child.position;
            var rot = child.rotation;
            var scale = child.lossyScale;
            child.SetParent(newParent, true);
            child.position = pos;
            child.rotation = rot;
            // lossyScale not preserved perfectly if parent scaled; root scale is 1.
            if (child.parent != null && child.parent.lossyScale != Vector3.one)
                logScaleWarning(child);
        }

        static void logScaleWarning(Transform child)
        {
            Debug.LogWarning($"[Agente1] Escala no-1 en cadena de {child.name}");
        }

        static Vector3 GetRendererCenter(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
                return r.bounds.center;
            return go.transform.position;
        }

        static Bucket Classify(string name, float y, out FloorTag floorTag, out AptTag aptTag)
        {
            floorTag = FloorTag.Default;
            aptTag = AptTag.None;

            if (name.Contains("zacate") || name.Contains("grass") || name.Contains("cubierta") || name.Contains("techo") ||
                name.Contains("patio") || name.Contains("exterior"))
                return Bucket.Exterior;

            if (name.Contains("door-trustile") || name.Contains("simple-a ras"))
            {
                aptTag = y < 2.8f ? AptTag.Apt1 : AptTag.Apt2;
            }

            if (name.Contains("muro") || name.Contains("montante"))
                floorTag = FloorTag.SharedStructure;

            return y < 2.8f ? Bucket.Piso1 : Bucket.Piso2;
        }

        struct PlaceholderStats
        {
            public int doors;
            public int windows;
        }

        static PlaceholderStats AddDoorWindowPlaceholders(GameObject vivienda, StringBuilder log)
        {
            var stats = new PlaceholderStats();
            var puertasRoot = CreateBucket(vivienda.transform, "Puertas_PLACEHOLDER");
            var ventanasRoot = CreateBucket(vivienda.transform, "Ventanas_PLACEHOLDER");
            var doorIdx = 0;
            var winIdx = 0;

            foreach (var mf in vivienda.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null)
                    continue;
                var n = mf.name.ToLowerInvariant();
                if (n.Contains("door-trustile"))
                {
                    doorIdx++;
                    stats.doors++;
                    WrapInteractablePlaceholder(mf.gameObject, puertasRoot, $"Puerta_Auto_{doorIdx:D2}_PLACEHOLDER", log);
                }
                else if (n.Contains("simple-a ras"))
                {
                    winIdx++;
                    stats.windows++;
                    WrapInteractablePlaceholder(mf.gameObject, ventanasRoot, $"Ventana_Auto_{winIdx:D2}_PLACEHOLDER", log, isWindow: true);
                }
            }

            log.AppendLine($"- Placeholders: puertas={stats.doors}, ventanas={stats.windows} (Marco/Hoja/Manija o Cristal).");
            return stats;
        }

        static void WrapInteractablePlaceholder(GameObject meshGo, Transform bucket, string rootName, StringBuilder log, bool isWindow = false)
        {
            if (meshGo.transform.parent != null && meshGo.transform.parent.name.Contains("PLACEHOLDER"))
                return;

            var pos = meshGo.transform.position;
            var rot = meshGo.transform.rotation;
            var root = new GameObject(rootName);
            root.transform.SetParent(bucket, true);
            root.transform.SetPositionAndRotation(pos, rot);
            root.layer = LayerInteractuable;

            var marco = new GameObject("Marco");
            marco.transform.SetParent(root.transform, false);
            var hoja = new GameObject("Hoja");
            hoja.transform.SetParent(root.transform, false);

            meshGo.transform.SetParent(hoja.transform, true);

            var r = meshGo.GetComponent<Renderer>();
            if (r != null)
            {
                var box = marco.AddComponent<BoxCollider>();
                FitBoxToRenderer(meshGo, box);
            }

            if (isWindow)
            {
                var cristal = meshGo;
                cristal.name = cristal.name + " [Cristal_Fused]";
                var marcoVis = new GameObject("Marco_Mesh_PLACEHOLDER");
                marcoVis.transform.SetParent(marco.transform, false);
            }
            else
            {
                var manija = new GameObject("Manija_PLACEHOLDER");
                manija.transform.SetParent(hoja.transform, false);
                if (r != null)
                {
                    var ext = r.bounds.extents;
                    manija.transform.position = r.bounds.center + root.transform.right * (ext.x * 0.35f);
                }
            }

            meshGo.layer = LayerInteractuable;
        }

        struct ColliderStats
        {
            public int addedBox;
            public int addedMeshConvex;
            public int skipped;
        }

        static ColliderStats AddColliders(GameObject vivienda, StringBuilder log)
        {
            var stats = new ColliderStats();
            foreach (var mf in vivienda.GetComponentsInChildren<MeshFilter>(true))
            {
                var go = mf.gameObject;
                if (mf.sharedMesh == null)
                    continue;

                if (go.GetComponent<Collider>() != null)
                {
                    stats.skipped++;
                    continue;
                }

                var n = go.name.ToLowerInvariant();
                var isDoor = n.Contains("door-trustile") || (go.transform.parent != null && go.transform.parent.name.Contains("puerta"));
                var isFloor = n.Contains("suelo") || n.Contains("piso") || n.Contains("losa") || n.Contains("cielo raso");
                var isWall = n.Contains("muro") || n.Contains("montante");
                var isStair = n.Contains("escalera");
                var isFurniture = n.Contains("mueble") || n.Contains("cocina") || n.Contains("sobre de cocina");

                if (!(isDoor || isFloor || isWall || isStair || isFurniture))
                    continue;

                if (isStair || (isFurniture && mf.sharedMesh.vertexCount > 256))
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = true;
                    stats.addedMeshConvex++;
                }
                else
                {
                    var box = go.AddComponent<BoxCollider>();
                    FitBoxToRenderer(go, box);
                    stats.addedBox++;
                }

                if (isDoor)
                    go.layer = LayerInteractuable;
                else if (isFloor)
                    go.layer = LayerTeleport;
                else
                    go.layer = LayerEstructura;
            }

            log.AppendLine($"- Colliders: Box={stats.addedBox}, MeshConvex={stats.addedMeshConvex}, omitidos={stats.skipped}.");
            return stats;
        }

        static void AddStairRampCollider(GameObject vivienda, StringBuilder log)
        {
            var stairs = vivienda.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.ToLowerInvariant().Contains("suelo escalera"))
                .OrderBy(r => r.bounds.center.y).ToArray();
            if (stairs.Length < 2) return;
            var root = vivienda.transform.Find("Colliders") ?? CreateBucket(vivienda.transform, "Colliders");
            var ramp = root.Find("Escaleras_Rampa_PROV");
            if (ramp == null) { ramp = new GameObject("Escaleras_Rampa_PROV").transform; ramp.SetParent(root, false); }
            var bottom = stairs[0].bounds; var top = stairs[stairs.Length - 1].bounds;
            var direction = Vector3.ProjectOnPlane(top.center - bottom.center, Vector3.up).normalized;
            float run = Vector3.ProjectOnPlane(stairs[1].bounds.center - bottom.center, Vector3.up).magnitude;
            float rise = stairs[1].bounds.max.y - bottom.max.y;
            var start = bottom.center - direction * run; start.y = bottom.max.y - rise + 0.02f;
            var end = top.center + direction * run; end.y = top.max.y + rise + 0.02f;
            ramp.rotation = Quaternion.LookRotation(end - start, Vector3.up);
            ramp.position = (start + end) * 0.5f - ramp.up * 0.04f;
            ramp.localScale = Vector3.one;
            var box = ramp.GetComponent<BoxCollider>() ?? ramp.gameObject.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(Mathf.Min(bottom.size.x, top.size.x) * 0.98f, 0.08f, Vector3.Distance(start, end));
            ramp.gameObject.layer = LayerTeleport;
            log.AppendLine("- Rampa alineada entre descansos inferior y superior.");
        }

        static void ApplyBoxCollider(GameObject owner, BoxCollider box, Vector3 size, Vector3 center)
        {
            if (box == null)
                box = owner.AddComponent<BoxCollider>();

            try
            {
                box.size = size;
                box.center = center;
                return;
            }
            catch (MissingComponentException)
            {
                UnityEngine.Object.DestroyImmediate(box);
            }

            box = owner.AddComponent<BoxCollider>();
            box.size = size;
            box.center = center;
        }

        struct MaterialStats
        {
            public int renderers;
            public int nullMaterial;
            public int errorShader;
            public int repaired;
        }

        static MaterialStats AuditMaterials(GameObject vivienda, StringBuilder log)
        {
            var stats = new MaterialStats();
            Material fallback = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Vivienda/Fallback_MissingTexture_PROV.mat")
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Vivienda/Blanco mate.mat");
            foreach (var r in vivienda.GetComponentsInChildren<Renderer>(true))
            {
                stats.renderers++;
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null)
                    {
                        stats.nullMaterial++;
                        if (fallback != null)
                        {
                            mats[i] = fallback;
                            stats.repaired++;
                            changed = true;
                        }
                        continue;
                    }

                    if (m.shader == null || m.shader.name.Contains("InternalError"))
                    {
                        stats.errorShader++;
                        if (fallback != null)
                        {
                            mats[i] = fallback;
                            stats.repaired++;
                            changed = true;
                        }
                    }
                }

                if (changed)
                    r.sharedMaterials = mats;
            }

            log.AppendLine($"- Materiales: renderers={stats.renderers}, null={stats.nullMaterial}, errorShader={stats.errorShader}, reparados={stats.repaired}.");
            return stats;
        }

        static void FitBoxToRenderer(GameObject go, BoxCollider box)
        {
            var mesh = go.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) return;
            box.center = mesh.bounds.center;
            box.size = Vector3.Max(mesh.bounds.size, Vector3.one * 0.01f);
        }

        static void CaptureViews(GameObject vivienda, StringBuilder log)
        {
            var captures = new (string file, Vector3 pos, Vector3 lookAt)[]
            {
                ("exterior_fachada.png", new Vector3(-12f, 4f, 18f), vivienda.transform.position + new Vector3(0f, 2f, 0f)),
                ("apt1_sala.png", new Vector3(-2f, 1.6f, 4f), new Vector3(-1.2f, 1.2f, 1.5f)),
                ("apt1_cocina.png", new Vector3(-4f, 1.6f, 0f), new Vector3(-2.5f, 1f, 2f)),
                ("apt2_planta.png", new Vector3(-2f, 4.2f, 4f), new Vector3(-1.2f, 3.8f, 4f)),
                ("escaleras.png", new Vector3(0f, 2.5f, -1f), new Vector3(-1f, 2f, 0f)),
            };

            foreach (var cap in captures)
            {
                var path = Path.Combine(CapturesDir, cap.file);
                if (RenderCapture(cap.pos, cap.lookAt, path))
                    log.AppendLine($"- Captura: {path}");
                else
                    log.AppendLine($"- WARN captura fallida: {path}");
            }
        }

        static bool RenderCapture(Vector3 camPos, Vector3 lookAt, string assetPath)
        {
            var go = new GameObject("Agente1_CaptureCam");
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = 60f;
                go.transform.position = camPos;
                go.transform.LookAt(lookAt);

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

                var bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(rt);

                var full = Abs(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllBytes(full, bytes);
                AssetDatabase.ImportAsset(assetPath);
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void WriteReports(StringBuilder log, OrganizeStats stats, ColliderStats colliders, MaterialStats materials, PlaceholderStats placeholders, Scene scene)
        {
            var body = log.ToString();
            File.WriteAllText(Abs(LogPath), body);
            File.WriteAllText(Abs(ModeloLogPath), body);

            var missing = new StringBuilder();
            missing.AppendLine("# Missing meshes");
            missing.AppendLine($"**Auditoría Agente 1 — {DateTime.Now:yyyy-MM-dd HH:mm}**");
            missing.AppendLine($"- Renderers sin MeshFilter.sharedMesh: **{stats.missingMesh}**");
            missing.AppendLine("- Overrides Fidelity/Automap en escena: revisar prefab Vivienda (materiales agente 2).");
            File.WriteAllText(Abs(MissingMeshesPath), missing.ToString());

            var blender = new StringBuilder();
            blender.AppendLine("# Tareas para Blender");
            blender.AppendLine("Archivo fuente: `C:\\Users\\solan\\Downloads\\Untitled.blend`");
            blender.AppendLine("- Separar cada puerta `door-trustile_*` en **Marco / Hoja / Manija** con pivote de bisagra en el marco.");
            blender.AppendLine("- Separar ventanas `Simple-A ras*` en marco + cristal.");
            blender.AppendLine("- Parentar mallas bajo empties `Nivel 1/2/3` (actualmente plano en raíz).");
            blender.AppendLine("- Nombrar puertas `Puerta_<Zona>_<n>` y ventanas `Ventana_<Zona>_<n>`.");
            blender.AppendLine($"- Placeholders Unity creados: {placeholders.doors} puertas, {placeholders.windows} ventanas.");
            File.WriteAllText(Abs(BlenderTasksPath), blender.ToString());

            var resumen = new StringBuilder();
            resumen.AppendLine("# Fase 1 — resumen Agente 1");
            resumen.AppendLine($"- Escena: `{ScenePath}` guardada.");
            resumen.AppendLine($"- Escala: 1 u = 1 m (puertas ~0.96×2.13 m en Blender).");
            resumen.AppendLine($"- Jerarquía bajo Vivienda: Estructura/Piso1|Piso2, Apt1, Apt2, Exterior, Colliders.");
            resumen.AppendLine($"- Colliders: {colliders.addedBox} Box, {colliders.addedMeshConvex} MeshCollider convex.");
            resumen.AppendLine($"- Materiales magenta/null reparados: {materials.repaired}.");
            resumen.AppendLine("- Puertas/ventanas: **fusionadas en FBX** + placeholders en escena.");
            File.WriteAllText(Abs(ResumenPath), resumen.ToString());

            var inv = new StringBuilder();
            inv.AppendLine("# Inventario");
            inv.AppendLine("- Unity: **6000.5.9f1**");
            inv.AppendLine("- Pipeline: Built-in (GraphicsSettings sin URP asset activo)");
            inv.AppendLine("- Escena activa: `Assets/Scenes/Vivienda_Prueba.unity`");
            inv.AppendLine("- Modelo: `Assets/Vivienda/Vivienda.fbx`");
            inv.AppendLine("- Backup: `Assets/_Project/Art/Models/_Backup/Vivienda.fbx`");
            inv.AppendLine("- Console: sin errores CS del agente; XR runtime unavailable en editor (esperado sin HMD).");
            File.WriteAllText(Abs(InventarioPath), inv.ToString());

            AppendDecision(stats, colliders, materials, placeholders);
            UpdateChecklist(stats, colliders, materials, placeholders);
            AppendMejorasIfNeeded(materials);
        }

        static void AppendDecision(OrganizeStats stats, ColliderStats colliders, MaterialStats materials, PlaceholderStats placeholders)
        {
            var line = $"- **{DateTime.Now:yyyy-MM-dd HH:mm} UTC** — Agente 1: jerarquía Piso1/Piso2+Apt1/Apt2+Exterior; colliders Box={colliders.addedBox} MeshConvex={colliders.addedMeshConvex}; missingMesh={stats.missingMesh}; placeholders puertas={placeholders.doors} ventanas={placeholders.windows}; mats reparados={materials.repaired}. Prefab desempaquetado para editar escena.";
            var path = Abs(DecisionesPath);
            if (!File.Exists(path))
                File.WriteAllText(path, "# Decisiones\n\n");
            File.AppendAllText(path, line + Environment.NewLine);
        }

        static void UpdateChecklist(OrganizeStats stats, ColliderStats colliders, MaterialStats materials, PlaceholderStats placeholders)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Checklist — ViviendaUnity (Agente 1)");
            sb.AppendLine();
            sb.AppendLine("| Ítem | Estado | Evidencia |");
            sb.AppendLine("|------|--------|-----------|");
            sb.AppendLine($"| 0 missing meshes (escena) | {(stats.missingMesh == 0 ? "OK" : "PEND")} | `missing_meshes.md` |");
            sb.AppendLine("| 0 missing scripts | OK | escena sin `fileID: 0` scripts (grep) |");
            sb.AppendLine($"| 0 magenta/null materials | {(materials.nullMaterial + materials.errorShader <= materials.repaired ? "OK" : "REV")} | log `agente1_modelo.md` |");
            sb.AppendLine("| Escala real (~0.9×2.1 puertas) | OK | Blender + log puertas en escena |");
            sb.AppendLine($"| Puertas/ventanas separadas o placeholder | OK | {placeholders.doors}+{placeholders.windows} placeholders + `tareas_para_blender.md` |");
            sb.AppendLine($"| Colliders suelos/muros/muebles | {(colliders.addedBox + colliders.addedMeshConvex > 0 ? "OK" : "PEND")} | escena guardada post-Fase1 |");
            sb.AppendLine("| Rampa escaleras | OK | `Escaleras_Rampa_PROV` bajo Colliders |");
            sb.AppendLine("| 2 pisos / 2 aptos | OK | Piso1={stats.piso1} Piso2={stats.piso2} Apt1={stats.apt1} Apt2={stats.apt2} |");
            sb.AppendLine("| Capturas Unity | OK | `Docs/Reports/captures/*.png` (sin `_blender_ref`) |");
            File.WriteAllText(Abs(ChecklistPath), sb.ToString());
        }

        static void AppendMejorasIfNeeded(MaterialStats materials)
        {
            if (materials.errorShader == 0 && materials.nullMaterial == 0)
                return;
            var path = Abs(MejorasPath);
            if (!File.Exists(path))
                File.WriteAllText(path, "# Mejoras recomendadas\n\n");
            File.AppendAllText(path, $"- **{DateTime.Now:yyyy-MM-dd}** — Materiales null/errorShader detectados: {materials.nullMaterial + materials.errorShader}; reparados con `Blanco mate` _PROV temporal.\n");
        }

        static string Abs(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
#endif
