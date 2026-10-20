using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ProyectoVR;

[InitializeOnLoad]
public static class RealismPreparation
{
    const string Folder = "Assets/_Project/Art/Presentation";
    const string LightingFolder = "Assets/_Project/Settings/Visualizacion/Lightmaps";
    const string Flag = "Vivienda.RealismBake";
    [Serializable] class MaterialRow { public string name; public float[] color; public float roughness; }
    [Serializable] class TriangleGroup { public int[] triangles; }
    [Serializable] class MeshRow { public string name, kind; public int material; public float[] vertices, normals, uv; public int[] triangles; public TriangleGroup[] submeshes; public string[] materialNames; }
    [Serializable] class Manifest { public MaterialRow[] materials; public MeshRow[] meshes; }
    static RealismPreparation()
    {
        Lightmapping.bakeCompleted += BakeCompleted; EditorApplication.update += ContinueBake;
        EditorSceneManager.sceneOpened += (_, _) => RestorePreview();
        EditorApplication.delayCall += RestorePreview;
    }

    static void RestorePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning || SessionState.GetInt(Flag, 0) != 0) return;
        var scenario = UnityEngine.Object.FindAnyObjectByType<BakedDayNightLighting>();
        if (scenario != null && scenario.day.color.Length > 0) scenario.ApplyDay();
    }

    static void ContinueBake()
    {
        if (!SessionState.GetBool(Flag + ".Pending", false) || Lightmapping.isRunning || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        SessionState.SetBool(Flag + ".Pending", false);
        if (!Lightmapping.BakeAsync()) { SessionState.SetInt(Flag, 0); Debug.LogError("Night bake did not start."); }
        else Debug.Log("Bake NOCHE iniciado.");
    }

    [MenuItem("Vivienda/Graficos/Preparar presentacion arquitectonica")]
    public static void Prepare()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play.");
        JourneyGeometry.EnsureFolder(Folder + "/Meshes");
        JourneyGeometry.EnsureFolder(Folder + "/Materials");
        JourneyGeometry.EnsureFolder(LightingFolder + "/Dia");
        JourneyGeometry.EnsureFolder(LightingFolder + "/Noche");
        using var compressed = File.OpenRead(Folder + "/Source/PresentationMeshes.json.gz");
        using var archive = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new StreamReader(archive);
        var manifest = JsonUtility.FromJson<Manifest>(reader.ReadToEnd());
        var materials = manifest.materials.Select(row =>
        {
            if (row.name == "Brown Polished Oak Wood") return AssetDatabase.LoadAssetAtPath<Material>("Assets/Vivienda/Fidelity/Materials/Brown Polished Oak Wood.mat");
            var path = Folder + "/Materials/" + row.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            // Blender values are linear; Unity material properties are authored as sRGB colors.
            material.SetColor("_BaseColor", new Color(row.color[0], row.color[1], row.color[2], 1).gamma);
            material.SetFloat("_Smoothness", 1 - row.roughness);
            material.SetFloat("_Metallic", 0);
            if (row.name.Contains("Leaves")) material.SetFloat("_Cull", 0);
            material.enableInstancing = true; EditorUtility.SetDirty(material);
            return material;
        }).ToArray();
        var root = GameObject.Find("Presentacion arquitectonica");
        if (root == null) root = new GameObject("Presentacion arquitectonica");
        int architecture = 0, decoration = 0;
        foreach (var row in manifest.meshes)
        {
            var target = row.kind == "architecture" ? GameObject.Find(row.name) : root.transform.Find(row.name)?.gameObject;
            if (row.kind == "architecture" && (target == null || target.GetComponentInParent<DoorHinge>() || target.GetComponentInParent<WindowHinge>())) continue;
            if (target == null) { target = new GameObject(row.name); target.transform.SetParent(root.transform, false); }
            var filter = target.GetComponent<MeshFilter>();
            if (filter == null) filter = target.AddComponent<MeshFilter>();
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = target.AddComponent<MeshRenderer>();
            var vertices = new Vector3[row.vertices.Length / 3]; var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = target.transform.InverseTransformPoint(new Vector3(row.vertices[i * 3], row.vertices[i * 3 + 1], row.vertices[i * 3 + 2]));
                normals[i] = target.transform.InverseTransformDirection(new Vector3(row.normals[i * 3], row.normals[i * 3 + 1], row.normals[i * 3 + 2]));
                uv[i] = new Vector2(row.uv[i * 2], row.uv[i * 2 + 1]);
            }
            string safe = new string(row.name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var path = Folder + "/Meshes/" + safe + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = row.name }; AssetDatabase.CreateAsset(mesh, path); } else mesh.Clear();
            mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = row.triangles; mesh.RecalculateBounds(); mesh.RecalculateTangents();
            filter.sharedMesh = mesh;
            if (row.kind == "decor") { renderer.sharedMaterial = materials[row.material]; decoration++; }
            else architecture++;
            if (row.kind == "decor" && (row.name.Contains("SofaBase") || row.name.Contains("BedBase") || row.name.Contains("Console") || row.name.Contains("CoffeeTop") || row.name.Contains("Bedside") || row.name.StartsWith("Planter")))
            {
                var box = target.GetComponent<BoxCollider>(); if (box == null) box = target.AddComponent<BoxCollider>();
                box.center = mesh.bounds.center; box.size = mesh.bounds.size;
            }
            EditorUtility.SetDirty(mesh); EditorUtility.SetDirty(filter); EditorUtility.SetDirty(renderer);
        }
        ApplySourcePartitions(); ConfigureLighting();
        // Metric furniture UVs and finishes must survive a full presentation rebuild.
        FurnitureFinishCorrection.Apply();
        FacadeFinishCorrection.Apply();
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveOpenScenes();
        Debug.Log($"Presentacion: {architecture} mallas con biseles; {decoration} piezas de mobiliario/contexto. Iluminacion preparada.");
    }

    static void ConfigureLighting()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingFolder + "/Arquitectura.asset");
        if (settings == null) { settings = new LightingSettings(); AssetDatabase.CreateAsset(settings, LightingFolder + "/Arquitectura.asset"); }
        settings.bakedGI = true; settings.realtimeGI = false;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
        LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
        settings.lightmapResolution = 14; settings.lightmapMaxSize = 1024; settings.lightmapPadding = 4;
        settings.directSampleCount = 32; settings.indirectSampleCount = 128; settings.environmentSampleCount = 128;
        settings.maxBounces = 4; settings.ao = true; settings.aoMaxDistance = .25f;
        settings.aoExponentIndirect = .7f; settings.aoExponentDirect = 0;
        settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
        Lightmapping.lightingSettings = settings; EditorUtility.SetDirty(settings);
        var roots = new[] { GameObject.Find("Vivienda"), GameObject.Find("Presentacion arquitectonica") };
        var receivers = roots.SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true)).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.GetComponentInParent<DoorHinge>() == null && r.GetComponentInParent<WindowHinge>() == null).ToArray();
        foreach (var renderer in receivers)
        {
            var filter = renderer.GetComponent<MeshFilter>(); if (filter == null || filter.sharedMesh == null) continue;
            // Preserve source albedo/atlas UVs. Native unwrap creates an independent lightmap UV channel.
            string safe = new string(renderer.name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var path = Folder + "/Meshes/Lightmap_" + safe + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); AssetDatabase.CreateAsset(mesh, path); }
            else if (mesh != filter.sharedMesh) EditorUtility.CopySerialized(filter.sharedMesh, mesh);
            mesh.name = renderer.name + " Lightmap UV";
            UnwrapParam.SetDefaults(out var unwrap); unwrap.packMargin = .025f;
            if (!Unwrapping.GenerateSecondaryUVSet(mesh, unwrap)) throw new Exception("UV2 failed: " + renderer.name);
            EditorUtility.SetDirty(mesh);
            filter.sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            renderer.receiveGI = ReceiveGI.Lightmaps;
            renderer.scaleInLightmap = renderer.name.Contains("Zacate") ? .08f : 1;
            EditorUtility.SetDirty(filter); EditorUtility.SetDirty(renderer);
        }
        var controller = UnityEngine.Object.FindAnyObjectByType<DayNightController>();
        var scenarios = controller.GetComponent<BakedDayNightLighting>(); if (scenarios == null) scenarios = controller.gameObject.AddComponent<BakedDayNightLighting>();
        scenarios.controller = controller; scenarios.receivers = receivers;
        var probeRoot = GameObject.Find("Sondas de luz arquitectonica"); if (probeRoot == null) probeRoot = new GameObject("Sondas de luz arquitectonica");
        var group = probeRoot.GetComponent<LightProbeGroup>(); if (group == null) group = probeRoot.AddComponent<LightProbeGroup>();
        var positions = new List<Vector3>();
        foreach (float floor in new[] { 0f, 3f })
            foreach (float x in new[] { -1.7f, -.8f, .9f, 2.8f, 4.1f })
                foreach (float z in new[] { -5.5f, -3.4f, -1.7f, .1f, 1.9f })
                    foreach (float height in new[] { .4f, 1.55f, 2.5f })
                    {
                        var p = new Vector3(x, floor + height, z);
                        if (!Physics.CheckSphere(p, .1f, ~0, QueryTriggerInteraction.Ignore)) positions.Add(p);
                    }
        group.probePositions = positions.ToArray(); EditorUtility.SetDirty(group); EditorUtility.SetDirty(scenarios);
        foreach (var light in UnityEngine.Object.FindObjectsByType<Light>())
            light.lightmapBakeType = light == RenderSettings.sun ? LightmapBakeType.Mixed : LightmapBakeType.Realtime;
    }

    static Manifest ReadSource(string file)
    {
        using var compressed = File.OpenRead(Folder + "/Source/" + file);
        using var archive = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new StreamReader(archive);
        return JsonUtility.FromJson<Manifest>(reader.ReadToEnd());
    }

    [MenuItem("Vivienda/Graficos/Restaurar materiales por cara desde Blender")]
    public static void ApplySourcePartitions()
    {
        if (Lightmapping.isRunning || EditorApplication.isPlaying) throw new InvalidOperationException("Editor ocupado.");
        var report = new List<string>();
        foreach (var row in ReadSource("WallMaterialPartitions.json.gz").meshes)
        {
            var target = GameObject.Find(row.name); if (target == null) throw new Exception("Wall missing: " + row.name);
            var filter = target.GetComponent<MeshFilter>(); var renderer = target.GetComponent<MeshRenderer>();
            var vertices = new Vector3[row.vertices.Length / 3]; var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = target.transform.InverseTransformPoint(new Vector3(row.vertices[i * 3], row.vertices[i * 3 + 1], row.vertices[i * 3 + 2]));
                normals[i] = target.transform.InverseTransformDirection(new Vector3(row.normals[i * 3], row.normals[i * 3 + 1], row.normals[i * 3 + 2]));
                uv[i] = new Vector2(row.uv[i * 2], row.uv[i * 2 + 1]);
            }
            var materials = row.materialNames.Select(path => AssetDatabase.LoadAssetAtPath<Material>(path) ?? throw new Exception("Material missing: " + path)).ToArray();
            var safe = new string(row.name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var pathMesh = Folder + "/Meshes/Partitions_" + safe + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathMesh);
            if (mesh == null) { mesh = new Mesh { name = row.name }; AssetDatabase.CreateAsset(mesh, pathMesh); } else mesh.Clear();
            mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.subMeshCount = row.submeshes.Length;
            for (int i = 0; i < row.submeshes.Length; i++) mesh.SetTriangles(row.submeshes[i].triangles, i, false);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            UnwrapParam.SetDefaults(out var unwrap); unwrap.packMargin = .025f;
            if (!Unwrapping.GenerateSecondaryUVSet(mesh, unwrap)) throw new Exception("Wall UV2 failed: " + row.name);
            filter.sharedMesh = mesh; renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(mesh); EditorUtility.SetDirty(filter); EditorUtility.SetDirty(renderer);
            report.Add(row.name + ": " + string.Join(", ", row.materialNames.Select(Path.GetFileNameWithoutExtension)));
        }
        PolishFurnitureNormals();
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes();
        File.WriteAllLines("Validation/Wall-Material-Partitions.txt", report);
        Debug.Log("Materiales por cara restaurados desde Blender: " + report.Count + " muros; normales de mobiliario suavizadas.");
    }

    [MenuItem("Vivienda/Graficos/Suavizar normales de mobiliario")]
    public static void PolishFurnitureNormals()
    {
        if (Lightmapping.isRunning) throw new InvalidOperationException("Bake running.");
        foreach (var row in ReadSource("PresentationMeshes.json.gz").meshes.Where(row => row.kind == "decor" && (row.name.StartsWith("P1_") || row.name.StartsWith("P2_"))))
        {
            var target = GameObject.Find(row.name); if (target == null) continue;
            var mesh = target.GetComponent<MeshFilter>().sharedMesh;
            // Native lightmap unwrapping can split/reorder vertices; match the unchanged world positions.
            var normalsByPosition = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < row.vertices.Length / 3; i++)
            {
                var key = Vector3Int.RoundToInt(new Vector3(row.vertices[i * 3], row.vertices[i * 3 + 1], row.vertices[i * 3 + 2]) * 10000);
                normalsByPosition.TryGetValue(key, out var sum);
                normalsByPosition[key] = sum + new Vector3(row.normals[i * 3], row.normals[i * 3 + 1], row.normals[i * 3 + 2]);
            }
            mesh.normals = mesh.vertices.Select(vertex =>
            {
                var key = Vector3Int.RoundToInt(target.transform.TransformPoint(vertex) * 10000);
                if (!normalsByPosition.TryGetValue(key, out var normal)) throw new Exception("Furniture position mismatch: " + row.name);
                return target.transform.InverseTransformDirection(normal.normalized);
            }).ToArray();
            mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh);
        }
        var ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Environment/ExteriorGround_Day.mat");
        foreach (string property in new[] { "_BaseMap", "_BumpMap" }) ground.SetTextureScale(property, Vector2.one * .06096f);
        EditorUtility.SetDirty(ground); AssetDatabase.SaveAssets();
    }


    [MenuItem("Vivienda/Graficos/Completar vegetacion y materiales")]
    public static void FinishMaterials()
    {
        if (Lightmapping.isRunning || EditorApplication.isPlaying) throw new InvalidOperationException("Editor ocupado.");
        const string vegetation = Folder + "/Vegetation";
        var materialList = new List<Material>();
        foreach (string kind in new[] { "pot", "leaves" })
        {
            var path = vegetation + "/" + kind + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            foreach (string suffix in new[] { "diff.jpg", "nor_gl.jpg", "MetallicSmoothness.png" })
            {
                var texturePath = vegetation + "/Textures/" + kind + "_" + suffix;
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType = suffix.StartsWith("nor") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = suffix.StartsWith("diff"); importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4; importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
                material.SetTexture(suffix.StartsWith("diff") ? "_BaseMap" : suffix.StartsWith("nor") ? "_BumpMap" : "_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", 1);
            material.SetFloat("_Cull", kind == "leaves" ? 0 : 2);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.enableInstancing = true; EditorUtility.SetDirty(material); materialList.Add(material);
        }
        var old = GameObject.Find("Presentacion arquitectonica");
        foreach (Transform child in old.transform)
            if (child.name.StartsWith("Plant") || child.name.StartsWith("Soil")) child.gameObject.SetActive(false);
        var root = GameObject.Find("Vegetacion de contexto"); if (root == null) root = new GameObject("Vegetacion de contexto");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(vegetation + "/PottedPlant02.fbx");
        if (model == null) throw new Exception("Plant FBX missing.");
        int index = 0;
        foreach (var location in new[] { new Vector3(-3, 0, 4.25f), new Vector3(-3, 0, 1.9f), new Vector3(5.6f, 0, 2.6f) })
        {
            var existing = root.transform.Find("PottedPlant02_" + index);
            var plant = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            plant.name = "PottedPlant02_" + index; plant.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, index * 37, 0)); plant.transform.localScale = Vector3.one;
            var renderers = plant.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            plant.transform.localScale *= 1.15f / bounds.size.y;
            bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            plant.transform.position = location + new Vector3(-bounds.center.x, -.045f - bounds.min.y, -bounds.center.z);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name.Contains("leaves") ? materialList[1] : materialList[0]).ToArray();
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.ReflectionProbeStatic);
            }
            index++;
        }
        var ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Environment/ExteriorGround_Day.mat");
        ground.SetColor("_BaseColor", new Color(.14f, .14f, .14f, 1).gamma);
        ground.SetTexture("_MetallicGlossMap", null); ground.DisableKeyword("_METALLICSPECGLOSSMAP"); ground.SetFloat("_Metallic", 0); ground.SetFloat("_Smoothness", .08f); EditorUtility.SetDirty(ground);
        // Extend only the visual context; the original plot and its walkable collision remain unchanged.
        var context = GameObject.Find("Terreno de contexto"); if (context == null) context = new GameObject("Terreno de contexto");
        var filter = context.GetComponent<MeshFilter>(); if (filter == null) filter = context.AddComponent<MeshFilter>();
        var rendererGround = context.GetComponent<MeshRenderer>(); if (rendererGround == null) rendererGround = context.AddComponent<MeshRenderer>();
        var mesh = new Mesh { name = "Context terrain" };
        mesh.vertices = new[] { new Vector3(-100, -.066f, -100), new Vector3(-100, -.066f, 100), new Vector3(100, -.066f, 100), new Vector3(100, -.066f, -100) };
        mesh.uv = mesh.vertices.Select(v => new Vector2(v.x, v.z) * 16.4042f).ToArray(); mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var groundPath = Folder + "/Meshes/ContextTerrain.asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(groundPath);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, groundPath); saved = mesh; } else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); }
        filter.sharedMesh = saved; rendererGround.sharedMaterial = ground;
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(context.scene); EditorSceneManager.SaveOpenScenes();
        Debug.Log("Contexto terminado: 3 plantas Poly Haven CC0, materiales PBR nativos 1K, cesped dielectrico calibrado y terreno extendido.");
    }

    [MenuItem("Vivienda/Graficos/Hornear iluminacion arquitectonica dia y noche")]
    public static void BeginBake()
    {
        if (EditorApplication.isPlaying || Lightmapping.isRunning) throw new InvalidOperationException("Editor ocupado.");
        SetEnvironment(false); SessionState.SetInt(Flag, 1); SessionState.SetBool(Flag + ".Pending", false);
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveOpenScenes();
        if (!Lightmapping.BakeAsync()) { SessionState.SetInt(Flag, 0); throw new Exception("Bake did not start."); }
        Debug.Log("Bake arquitectonico DIA iniciado.");
    }

    static void BakeCompleted()
    {
        int stage = SessionState.GetInt(Flag, 0); if (stage == 0) return;
        try
        {
            var component = UnityEngine.Object.FindAnyObjectByType<BakedDayNightLighting>();
            var maps = LightmapSettings.lightmaps;
            if (maps.Length == 0) throw new Exception("Bake finished without lightmaps.");
            var scenario = new BakedDayNightLighting.Scenario { color = new Texture2D[maps.Length], direction = new Texture2D[maps.Length],
                indices = component.receivers.Select(r => r.lightmapIndex).ToArray(), scaleOffsets = component.receivers.Select(r => r.lightmapScaleOffset).ToArray() };
            for (int i = 0; i < maps.Length; i++)
            {
                scenario.color[i] = CopyMap(maps[i].lightmapColor, stage, i, "Color");
                scenario.direction[i] = CopyMap(maps[i].lightmapDir, stage, i, "Direction");
            }
            var probes = LightmapSettings.lightProbes?.bakedProbes;
            if (probes != null)
            {
                scenario.probeCoefficients = new float[probes.Length * 27];
                for (int i = 0; i < probes.Length; i++) for (int channel = 0; channel < 3; channel++) for (int c = 0; c < 9; c++) scenario.probeCoefficients[i * 27 + channel * 9 + c] = probes[i][channel, c];
            }
            if (stage == 1)
            {
                component.day = scenario; EditorUtility.SetDirty(component); AssetDatabase.SaveAssets(); EditorSceneManager.SaveOpenScenes();
                SessionState.SetInt(Flag, 2); SetEnvironment(true);
                SessionState.SetBool(Flag + ".Pending", true);
                Debug.Log("Bake DIA guardado; esperando a que el editor libere el horneado anterior.");
            }
            else
            {
                component.night = scenario; component.ApplyDay(); SetEnvironment(false);
                SessionState.SetInt(Flag, 0); EditorUtility.SetDirty(component);
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(component.gameObject.scene); EditorSceneManager.SaveOpenScenes();
                Directory.CreateDirectory("Validation");
                File.WriteAllText("Validation/Architectural-GI.txt", $"PASS native Progressive CPU bake\nDay lightmaps={component.day.color.Length}\nNight lightmaps={component.night.color.Length}\nReceivers={component.receivers.Length}\nLight probes={scenario.probeCoefficients.Length / 27}\nDirectional lightmaps; 14 texels/m; 4 bounces; AO indirect 0.25m.\nRoom switches remain realtime.\n");
                Debug.Log("BAKE ARQUITECTONICO COMPLETADO: escenarios dia/noche guardados.");
            }
        }
        catch (Exception error) { SessionState.SetInt(Flag, 0); Debug.LogException(error); }
    }

    static Texture2D CopyMap(Texture2D source, int stage, int index, string suffix)
    {
        if (source == null) return null;
        var path = AssetDatabase.GetAssetPath(source);
        var destination = LightingFolder + (stage == 1 ? "/Dia/" : "/Noche/") + index + "_" + suffix + Path.GetExtension(path);
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(destination)) AssetDatabase.DeleteAsset(destination);
        if (!AssetDatabase.CopyAsset(path, destination)) throw new Exception("Cannot copy " + path);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
    }

    static void SetEnvironment(bool night)
    {
        var controller = UnityEngine.Object.FindAnyObjectByType<DayNightController>();
        var data = new SerializedObject(controller); var snapshot = data.FindProperty(night ? "night" : "day");
        var sun = (Light)data.FindProperty("directionalLight").objectReferenceValue;
        sun.color = snapshot.FindPropertyRelative("sunColor").colorValue;
        sun.intensity = snapshot.FindPropertyRelative("sunIntensity").floatValue;
        sun.transform.rotation = Quaternion.Euler(snapshot.FindPropertyRelative("sunEuler").vector3Value);
        sun.enabled = true;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = snapshot.FindPropertyRelative("ambientSky").colorValue;
        RenderSettings.ambientEquatorColor = snapshot.FindPropertyRelative("ambientEquator").colorValue;
        RenderSettings.ambientGroundColor = snapshot.FindPropertyRelative("ambientGround").colorValue;
        RenderSettings.ambientIntensity = snapshot.FindPropertyRelative("ambientIntensity").floatValue;
        var sky = (Material)data.FindProperty("skyboxMaterial").objectReferenceValue;
        sky.SetFloat("_Exposure", snapshot.FindPropertyRelative("skyboxExposure").floatValue);
        sky.SetColor("_SkyTint", snapshot.FindPropertyRelative("skyboxTint").colorValue);
        var access = data.FindProperty("exteriorNightLights");
        for (int i = 0; i < access.arraySize; i++) if (access.GetArrayElementAtIndex(i).objectReferenceValue is Light light) light.enabled = night;
        EditorUtility.SetDirty(sun); EditorUtility.SetDirty(sky);
    }
}
