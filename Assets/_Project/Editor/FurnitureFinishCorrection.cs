using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class FurnitureFinishCorrection
{
    const string Folder = "Assets/_Project/Art/Presentation/Finishes";
    [Serializable] class Row { public string name; public int material; public float tileMetres; public bool frontReveal; public float[] vertices, normals, uv; public int[] triangles; }
    [Serializable] class Manifest { public Row[] meshes; }

    [MenuItem("Vivienda/Graficos/Corregir acabados del mobiliario")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || Lightmapping.isRunning) throw new InvalidOperationException("Salir de Play y esperar al bake antes de corregir materiales.");
        JourneyGeometry.EnsureFolder(Folder + "/Meshes");
        JourneyGeometry.EnsureFolder(Folder + "/Materials");
        string[] names = { "Furniture_Walnut", "Furniture_Linen", "Furniture_Microcement", "Furniture_Rug", "Furniture_Bedding" };
        string[] baseMaps = { "walnut_veneer_Diffuse.jpg", "Linen_Cream_BaseColor.png", "Microcement_BaseColor.png", "Linen_Cream_BaseColor.png", "Linen_Cream_BaseColor.png" };
        string[] normalMaps = { "walnut_veneer_nor_gl.jpg", "rough_linen_nor_gl.jpg", "Microcement_Normal.png", "rough_linen_nor_gl.jpg", "rough_linen_nor_gl.jpg" };
        string[] packedMaps = { "walnut_veneer_MetallicSmoothness.png", "rough_linen_MetallicSmoothness.png", "Microcement_MetallicSmoothness.png", "rough_linen_MetallicSmoothness.png", "rough_linen_MetallicSmoothness.png" };
        float[] bump = { .22f, .32f, .35f, .4f, .25f };
        var materials = new Material[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string path = Folder + "/Materials/" + names[i] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap", Texture(baseMaps[i], true, false));
            material.SetTexture("_BumpMap", Texture(normalMaps[i], false, true));
            material.SetTexture("_MetallicGlossMap", Texture(packedMaps[i], false, false));
            material.SetColor("_BaseColor", i == 3 ? new Color(.42f, .4f, .36f, 1).gamma : i == 4 ? new Color(1.15f, 1.15f, 1.15f, 1).gamma : Color.white);
            material.SetFloat("_WorkflowMode", 1); material.DisableKeyword("_SPECULAR_SETUP"); material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", 1); material.SetFloat("_SmoothnessTextureChannel", 0); material.SetFloat("_BumpScale", bump[i]);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.enableInstancing = true; EditorUtility.SetDirty(material); materials[i] = material;
        }
        using var source = File.OpenRead(Folder + "/FurnitureFinishes.json.gz");
        using var archive = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(archive);
        var manifest = JsonUtility.FromJson<Manifest>(reader.ReadToEnd());
        var all = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
        var report = new List<string> { "Source: Blender presentation copy; metric face UVs; independent native lightmap UV2.", "Walnut repeat=1.8m; textiles=0.25m; microcement=0.5m; metallic=0; front reveals=3mm." };
        foreach (var row in manifest.meshes)
        {
            // Name lookup includes inactive source meshes so the correction persists in every scene state.
            var filter = all.FirstOrDefault(f => f.name == row.name);
            if (filter == null) throw new Exception("Furniture source object missing: " + row.name);
            var renderer = filter.GetComponent<MeshRenderer>();
            var originalBounds = renderer.bounds;
            int count = row.vertices.Length / 3;
            var vertices = new Vector3[count]; var normals = new Vector3[count]; var uv = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = filter.transform.InverseTransformPoint(new Vector3(row.vertices[i * 3], row.vertices[i * 3 + 1], row.vertices[i * 3 + 2]));
                normals[i] = filter.transform.InverseTransformDirection(new Vector3(row.normals[i * 3], row.normals[i * 3 + 1], row.normals[i * 3 + 2]));
                uv[i] = new Vector2(row.uv[i * 2], row.uv[i * 2 + 1]);
                if (!float.IsFinite(uv[i].x) || !float.IsFinite(uv[i].y)) throw new Exception("Invalid UV: " + row.name);
            }
            string safe = new string(row.name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            string path = Folder + "/Meshes/" + safe + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = row.name + " Metric finish" }; AssetDatabase.CreateAsset(mesh, path); } else mesh.Clear();
            mesh.indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = row.triangles;
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            UnwrapParam.SetDefaults(out var unwrap); unwrap.packMargin = .025f;
            if (!Unwrapping.GenerateSecondaryUVSet(mesh, unwrap)) throw new Exception("Lightmap UV2 failed: " + row.name);
            filter.sharedMesh = mesh; renderer.sharedMaterial = materials[row.material];
            // No new geometry or collision enters the navigation path; front leaves shrink within their old envelope.
            if (Vector3.Distance(originalBounds.center, renderer.bounds.center) > .01f || Vector3.Distance(originalBounds.size, renderer.bounds.size) > .02f)
                throw new Exception("Unexpected furniture geometry change: " + row.name);
            EditorUtility.SetDirty(mesh); EditorUtility.SetDirty(filter); EditorUtility.SetDirty(renderer);
            report.Add("PASS " + row.name + " => " + names[row.material] + "; tile=" + row.tileMetres + "m; reveal=" + row.frontReveal + "; triangles=" + row.triangles.Length / 3);
        }
        CorrectCeilings();
        Directory.CreateDirectory("Validation"); File.WriteAllLines("Validation/Furniture-Finishes.txt", report);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes();
        Debug.Log("Acabados corregidos: " + manifest.meshes.Length + " objetos; " + manifest.meshes.Count(r => r.frontReveal) + " frentes con juntas reales. Rehornear GI y reflejos.");
    }

    static Texture2D Texture(string file, bool srgb, bool normal)
    {
        string path = Folder + "/" + file;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new Exception("Texture missing: " + path);
        var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (importer.textureType != type || importer.sRGBTexture != srgb || importer.filterMode != FilterMode.Trilinear || importer.anisoLevel != 4 || !importer.mipmapEnabled || importer.maxTextureSize != (file.StartsWith("walnut") ? 2048 : 1024))
        {
            importer.textureType = type; importer.sRGBTexture = srgb; importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4; importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = file.StartsWith("walnut") ? 2048 : 1024; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    [MenuItem("Vivienda/Graficos/Corregir brillo de cielos rasos")]
    public static void CorrectCeilings()
    {
        if (EditorApplication.isPlaying || Lightmapping.isRunning) throw new InvalidOperationException("Editor ocupado.");
        JourneyGeometry.EnsureFolder(Folder + "/Materials");
        int count = 0;
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include).Where(r => r.name.Contains("Cielo Raso")))
        {
            var input = renderer.sharedMaterial;
            if (input == null || !input.HasProperty("_Smoothness") || input.GetFloat("_Smoothness") <= .1f) continue;
            string path = Folder + "/Materials/Ceiling_" + (renderer.name.Contains("686576") ? "Acoustic_Matte" : "Dark_Matte") + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(input); AssetDatabase.CreateAsset(material, path); }
            material.SetFloat("_Smoothness", .08f); material.SetFloat("_BumpScale", .25f);
            // Keep the original albedo, UVs and specular workflow; only remove the polished ceiling finish.
            renderer.sharedMaterial = material; EditorUtility.SetDirty(material); EditorUtility.SetDirty(renderer); count++;
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes();
        Debug.Log("Cielos rasos con acabado mate corregido: " + count);
    }

    [MenuItem("Vivienda/Graficos/Auditar acabados del mobiliario")]
    public static void Audit()
    {
        using var source = File.OpenRead(Folder + "/FurnitureFinishes.json.gz");
        using var archive = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(archive);
        var expected = JsonUtility.FromJson<Manifest>(reader.ReadToEnd());
        var all = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
        var report = new List<string>(); int errors = 0;
        foreach (var row in expected.meshes)
        {
            var filter = all.FirstOrDefault(f => f.name == row.name);
            if (filter == null) { errors++; report.Add("FAIL missing " + row.name); continue; }
            var mesh = filter.sharedMesh; var material = filter.GetComponent<Renderer>().sharedMaterial;
            bool valid = AssetDatabase.GetAssetPath(mesh).StartsWith(Folder + "/Meshes/") && AssetDatabase.GetAssetPath(material).StartsWith(Folder + "/Materials/");
            valid &= mesh.uv.Length == mesh.vertexCount && mesh.uv2.Length == mesh.vertexCount;
            var vertices = mesh.vertices; var uv = mesh.uv; var triangles = mesh.triangles;
            float worst = 0; int measured = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                float area = Vector3.Cross(filter.transform.TransformVector(vertices[b] - vertices[a]), filter.transform.TransformVector(vertices[c] - vertices[a])).magnitude;
                if (area < .004f) continue; // Tiny bevel strips use interpolated UVs; assess the visible planar surfaces.
                var u = uv[b] - uv[a]; var v = uv[c] - uv[a]; float uvArea = Mathf.Abs(u.x * v.y - u.y * v.x);
                float error = uvArea <= .00000001f ? 100 : Mathf.Abs(area / uvArea / (row.tileMetres * row.tileMetres) - 1);
                worst = Mathf.Max(worst, error); measured++;
            }
            valid &= measured > 0 && worst < .035f;
            foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
            {
                var texture = material.GetTexture(property);
                var importer = texture != null ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter : null;
                valid &= importer != null && importer.mipmapEnabled && importer.filterMode == FilterMode.Trilinear && importer.anisoLevel == 4;
                if (importer != null) valid &= property == "_BaseMap" ? importer.sRGBTexture : property == "_BumpMap" ? importer.textureType == TextureImporterType.NormalMap : !importer.sRGBTexture;
            }
            valid &= material.shader.name == "Universal Render Pipeline/Lit" && material.GetFloat("_Metallic") == 0 && material.IsKeywordEnabled("_NORMALMAP") && material.IsKeywordEnabled("_METALLICSPECGLOSSMAP");
            if (!valid) errors++;
            report.Add((valid ? "PASS " : "FAIL ") + row.name + ": tile=" + row.tileMetres + "m; surfaces=" + measured + "; max metric UV error=" + worst.ToString("P2"));
        }
        var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
        int missingSlots = renderers.Count(r => r.sharedMaterials.Any(m => m == null));
        errors += missingSlots;
        report.Add("Scene renderers with missing material slots=" + missingSlots);
        report.Add("Furniture audited=" + expected.meshes.Length + "; errors=" + errors);
        File.WriteAllLines("Validation/Furniture-Finish-Audit.txt", report);
        if (errors != 0) Debug.LogError("Furniture finish audit: " + errors + " problems. See Validation/Furniture-Finish-Audit.txt");
        else Debug.Log("Furniture finish audit passed: " + expected.meshes.Length + " pieces; PBR imports, metric UVs and independent lightmap UV2.");
    }
}
