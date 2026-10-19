using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RestoreBakedMaterials
{
    [MenuItem("Vivienda/Materiales/Restaurar canales PBR de Blender")]
    public static void Repair()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Salir de Play antes de restaurar materiales.");

        var report = new StringBuilder();
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Vivienda/Fidelity/Materials" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material.shader.name != "Universal Render Pipeline/Lit" || material.GetTexture("_SpecGlossMap") == null)
                continue;

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_SpecGlossMap"))) as TextureImporter;
            if (importer == null || importer.sRGBTexture)
                throw new InvalidOperationException("SpecGloss debe importarse lineal: " + path);

            Undo.RecordObject(material, "Restaurar mapas PBR horneados");
            material.SetFloat("_WorkflowMode", 0);
            material.SetFloat("_Smoothness", 1);
            material.SetFloat("_SmoothnessTextureChannel", 0);
            material.DisableKeyword("_SPECGLOSSMAP");
            material.DisableKeyword("_METALLICGLOSSMAP");
            material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            material.EnableKeyword("_SPECULAR_SETUP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (material.GetTexture("_BumpMap") != null)
                material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            report.AppendLine("PASS " + material.name + ": URP Specular; RGB=F0; alpha=smoothness; mapa lineal.");
            count++;
        }

        var grassMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Vivienda/Fidelity/Grass/Grass01_MetallicSmoothness.png");
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Art/Materials/Environment" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (!material.name.StartsWith("ExteriorGround"))
                continue;

            Undo.RecordObject(material, "Restaurar canales y escala del cesped");
            material.SetFloat("_WorkflowMode", 1);
            material.SetTexture("_MetallicGlossMap", grassMap);
            material.SetFloat("_Smoothness", 1);
            material.SetFloat("_SmoothnessTextureChannel", 0);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_SPECULAR_SETUP");
            material.DisableKeyword("_METALLICGLOSSMAP");
            material.enableInstancing = true;

            var surface = UnityEngine.Object.FindObjectsByType<MeshRenderer>()
                .FirstOrDefault(renderer => renderer.sharedMaterials.Contains(material));
            if (surface != null)
            {
                float uvPerMeter = GetUVPerMeter(surface.GetComponent<MeshFilter>());
                if (uvPerMeter <= 0)
                    throw new InvalidOperationException("No se pudo medir el UV del cesped.");
                // One tile per meter, retaining the baked UV rotation and normal-map basis.
                var tiling = Vector2.one / uvPerMeter;
                foreach (var property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap" })
                {
                    material.SetTextureScale(property, tiling);
                    material.SetTextureOffset(property, Vector2.zero);
                }
                report.AppendLine("PASS " + material.name + ": tile de 1m; UV/m=" + uvPerMeter.ToString("F3") + "; tiling=" + tiling.ToString("F5"));
            }
            foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
            {
                var texture = material.GetTexture(property);
                if (texture == null) continue;
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (importer == null) continue;
                if (importer.anisoLevel == 4 && importer.filterMode == FilterMode.Trilinear && importer.mipmapEnabled) continue;
                importer.anisoLevel = 4;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            EditorUtility.SetDirty(material);
            report.AppendLine("PASS " + material.name + ": metallic/smoothness horneado; mipmaps, filtro trilineal y anisotropia 4x.");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        report.AppendLine("Materiales especulares reparados=" + count);
        report.AppendLine("Bakes existentes conservados; no se añade iluminacion a los albedos.");
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/BlenderPBR-Restoration.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    static float GetUVPerMeter(MeshFilter surface)
    {
        var mesh = surface.sharedMesh;
        var vertices = mesh.vertices.Select(surface.transform.TransformPoint).ToArray();
        var uv = mesh.uv;
        var triangles = mesh.triangles;
        float top = vertices.Max(vertex => vertex.y);
        var ratios = new List<float>();
        for (int index = 0; index < triangles.Length; index += 3)
        {
            int a = triangles[index], b = triangles[index + 1], c = triangles[index + 2];
            if (Mathf.Abs(vertices[a].y - top) > .005f || Mathf.Abs(vertices[b].y - top) > .005f || Mathf.Abs(vertices[c].y - top) > .005f)
                continue;
            foreach (var edge in new[] { (a, b), (b, c), (c, a) })
            {
                float meters = Vector3.Distance(vertices[edge.Item1], vertices[edge.Item2]);
                if (meters > .1f)
                    ratios.Add(Vector2.Distance(uv[edge.Item1], uv[edge.Item2]) / meters);
            }
        }
        return ratios.Count > 0 ? ratios.Average() : 0;
    }
}
