using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FacadeFinishCorrection
{
    const string Root = "Assets/_Project/Art/Presentation";
    const string WoodPath = "Assets/Vivienda/Fidelity/Materials/Brown Polished Oak Wood.mat";
    [Serializable] class Row { public string name; public float[] vertices, normals, uv; }
    [Serializable] class Manifest { public Row[] meshes; }
    static Vector3 V(float[] values, int i) => new Vector3(values[i*3], values[i*3+1], values[i*3+2]);
    static Vector3Int Key(Vector3 p) => Vector3Int.RoundToInt(p * 10000);

    [MenuItem("Vivienda/Graficos/Trasladar acabados de fachada desde Blender")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || Lightmapping.isRunning) throw new InvalidOperationException("Editor ocupado.");
        using var file = File.OpenRead(Root + "/Source/FacadeFinishes.json.gz");
        using var archive = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(archive);
        var manifest = JsonUtility.FromJson<Manifest>(reader.ReadToEnd());
        var wood = AssetDatabase.LoadAssetAtPath<Material>(WoodPath);
        if (wood == null) throw new Exception("Falta material de madera.");
        var report = new List<string>();
        foreach (var row in manifest.meshes)
        {
            var go = GameObject.Find(row.name);
            if (go == null) throw new Exception("Falta fachada: " + row.name);
            var filter = go.GetComponent<MeshFilter>(); var renderer = go.GetComponent<MeshRenderer>();
            var original = filter.sharedMesh;
            var lookup = new Dictionary<Vector3Int, List<int>>();
            for (int i=0; i<row.vertices.Length/3; i++)
            {
                var key = Key(V(row.vertices,i));
                if (!lookup.TryGetValue(key,out var list)) lookup[key] = list = new List<int>();
                list.Add(i);
            }
            var indices = new HashSet<int>();
            for (int slot=0;slot<renderer.sharedMaterials.Length;slot++)
                if (renderer.sharedMaterials[slot] == wood)
                    foreach (int index in original.GetTriangles(slot)) indices.Add(index);
            var vertices=original.vertices; var normals=original.normals; var uv=original.uv;
            int unmatched=0;
            foreach (int i in indices)
            {
                var position=go.transform.TransformPoint(vertices[i]);
                if (!lookup.TryGetValue(Key(position),out var candidates))
                {
                    // Export/import float rounding can cross a quantization boundary.
                    candidates=Enumerable.Range(0,row.vertices.Length/3).Where(c=>(V(row.vertices,c)-position).sqrMagnitude < .0002f*.0002f).ToList();
                    if(candidates.Count==0) { unmatched++; continue; }
                }
                var normal=go.transform.TransformDirection(normals[i]).normalized;
                int match=candidates.OrderByDescending(c=>Vector3.Dot(normal,V(row.normals,c))).First();
                uv[i]=new Vector2(row.uv[match*2],row.uv[match*2+1]);
            }
            if (unmatched>0) throw new Exception(row.name+": vertices sin correspondencia="+unmatched);
            string safe=new string(row.name.Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray());
            string path=Root+"/Meshes/Facade_"+safe+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null) { mesh=UnityEngine.Object.Instantiate(original); AssetDatabase.CreateAsset(mesh,path); }
            else if(mesh!=original) EditorUtility.CopySerialized(original,mesh);
            // Only albedo UV and tangents change; retain geometry, lightmap UV2 and renderer references.
            mesh.uv=uv;mesh.RecalculateTangents();filter.sharedMesh=mesh;
            EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(filter);
            report.Add("PASS "+row.name+": "+indices.Count+" UV de madera trasladadas; UV2 conservadas.");
        }
        wood.SetColor("_BaseColor", Color.white);
        wood.SetFloat("_BumpScale",.3f);EditorUtility.SetDirty(wood);
        int glassCount=0;
        foreach (var material in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null && (m.name=="Glass.001" || m.name=="Glass" || m.name=="AR3DMat Architectural Clear Glass")).Distinct())
        {
            material.SetTexture("_BaseMap",null);material.SetTexture("_SpecGlossMap",null);
            material.DisableKeyword("_METALLICSPECGLOSSMAP");material.EnableKeyword("_SPECULAR_SETUP");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHAMODULATE_ON");
            material.SetFloat("_WorkflowMode",0);material.SetFloat("_Surface",1);material.SetFloat("_Blend",1);
            material.SetFloat("_BlendModePreserveSpecular",1);material.SetFloat("_Metallic",0);
            material.SetFloat("_Smoothness",.985f);material.SetFloat("_SmoothnessTextureChannel",0);
            // Reflective architectural coating: dielectric reflectance approximates Blender's IOR 3.7 coat.
            material.SetColor("_SpecColor",new Color(.35f,.35f,.35f,1));
            material.SetColor("_BaseColor",new Color(.12f,.14f,.15f,.65f));
            material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",10);
            material.SetFloat("_SrcBlendAlpha",1);material.SetFloat("_DstBlendAlpha",10);
            material.SetFloat("_ZWrite",0);material.SetFloat("_EnvironmentReflections",1);material.SetFloat("_SpecularHighlights",1);
            material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
            material.SetShaderPassEnabled("ShadowCaster",false);material.SetShaderPassEnabled("DepthOnly",false);
            EditorUtility.SetDirty(material);glassCount++;
        }
        foreach (var probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>())
            if (probe.name.StartsWith("Reflejos Fachada")) { probe.boxProjection=false; probe.center=new Vector3(0,0,-.5f); probe.size=new Vector3(8.8f,probe.size.y,7); EditorUtility.SetDirty(probe); }
        report.Add("PASS vidrio reflectante URP Lit: "+glassCount+" materiales; madera marron, normal .3.");
        Directory.CreateDirectory("Validation");File.WriteAllLines("Validation/Facade-Finishes.txt",report);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Debug.Log(string.Join("\n",report));
    }
}
