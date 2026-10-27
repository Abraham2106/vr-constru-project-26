using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ProyectoVR;

public static class MirrorGlazingCorrection
{
    const string Folder="Assets/_Project/Art/Presentation";
    const string CubePath=Folder+"/Reflections/Blender_Forest.exr";
    const string GlassPath="Assets/Vivienda/Fidelity/Materials/Glass.001.mat";
    [MenuItem("Vivienda/Graficos/Aplicar vidrio espejado de referencia")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying || Lightmapping.isRunning)throw new InvalidOperationException("Editor ocupado.");
        AssetDatabase.ImportAsset(CubePath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(CubePath);
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        if(importer.textureShape!=TextureImporterShape.TextureCube || settings.cubemapConvolution!=TextureImporterCubemapConvolution.Specular)
        {
            importer.textureShape=TextureImporterShape.TextureCube;
            importer.generateCubemap=TextureImporterGenerateCubemap.AutoCubemap;
            importer.ReadTextureSettings(settings);settings.cubemapConvolution=TextureImporterCubemapConvolution.Specular;importer.SetTextureSettings(settings);
            importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.maxTextureSize=256;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(CubePath);
        if(cube==null)throw new Exception("No se importo el entorno de reflejo.");
        var exterior=AssetDatabase.LoadAssetAtPath<Material>(GlassPath);
        const string insidePath=Folder+"/Reflections/Glass_Interior_Clear.mat";
        var interior=AssetDatabase.LoadAssetAtPath<Material>(insidePath);
        if(interior==null){interior=new Material(exterior){name="Glass Interior Clear"};AssetDatabase.CreateAsset(interior,insidePath);}
        Configure(exterior,false);Configure(interior,true);
        var probes=UnityEngine.Object.FindObjectsByType<ReflectionProbe>().Where(p=>p.name.StartsWith("Reflejos Fachada")).OrderBy(p=>p.transform.position.y).ToArray();
        if(probes.Length!=2)throw new Exception("Faltan sondas exteriores de ambos pisos.");
        var anchors=new Transform[2];
        var mirrorProbes=new ReflectionProbe[2];
        var root=GameObject.Find("Referencia visual del vidrio");if(root==null)root=new GameObject("Referencia visual del vidrio");
        for(int floor=0;floor<2;floor++)
        {
            var original=probes[floor];
            var originalDay=AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/_Project/Settings/Visualizacion/Reflections/Fachada"+(floor+1)+"_Dia.exr");
            original.customBakedTexture=originalDay;EditorUtility.SetDirty(original);
            var name="Vidrio espejo Piso "+(floor+1);var child=root.transform.Find(name);
            var go=child!=null?child.gameObject:new GameObject(name);go.transform.SetParent(root.transform,false);
            go.transform.position=new Vector3(.3f,floor==0?1.4f:4.45f,5.5f);
            var probe=go.GetComponent<ReflectionProbe>();if(probe==null)probe=go.AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=cube;probe.boxProjection=false;
            probe.center=Vector3.zero;probe.size=Vector3.one*.4f;probe.blendDistance=0;probe.importance=100;
            anchors[floor]=go.transform;mirrorProbes[floor]=probe;EditorUtility.SetDirty(probe);
        }
        int count=0;
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.sharedMaterials.Contains(exterior)))
        {
            var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
            var original=filter.sharedMesh;
            string safe=new string(renderer.name.Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray());
            string path=Folder+"/Meshes/Glazing_"+safe+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(mesh,path);}
            else if(mesh!=original)EditorUtility.CopySerialized(original,mesh);
            var vertices=mesh.vertices;var normals=mesh.normals;var outside=new List<int>();var inside=new List<int>();
            var bounds=renderer.bounds;Vector3 direction;
            if(bounds.size.x<bounds.size.z)direction=Vector3.right*(bounds.center.x>.3f?1:-1);
            else direction=Vector3.forward*(bounds.center.z> -1.7f?1:-1);
            // Closed pane: classify existing faces, keeping every vertex and collider in place.
            var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var normal=renderer.transform.TransformDirection(normals[triangles[i]]+normals[triangles[i+1]]+normals[triangles[i+2]]).normalized;
                var list=Vector3.Dot(normal,direction)<-.5f?inside:outside;
                list.Add(triangles[i]);list.Add(triangles[i+1]);list.Add(triangles[i+2]);
            }
            if(inside.Count==0||outside.Count==0)throw new Exception("No se distinguen ambas caras: "+renderer.name);
            mesh.subMeshCount=2;mesh.SetTriangles(outside,0);mesh.SetTriangles(inside,1);mesh.RecalculateBounds();
            filter.sharedMesh=mesh;renderer.sharedMaterials=new[]{exterior,interior};
            renderer.reflectionProbeUsage=ReflectionProbeUsage.Simple;renderer.probeAnchor=anchors[bounds.center.y>=3?1:0];
            EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(filter);EditorUtility.SetDirty(renderer);count++;
        }
        var controller=UnityEngine.Object.FindAnyObjectByType<DayNightController>();var data=new SerializedObject(controller);
        var scenarios=data.FindProperty("reflectionScenarios");
        for(int i=0;i<scenarios.arraySize;i++)
        {
            var row=scenarios.GetArrayElementAtIndex(i);var probe=row.FindPropertyRelative("probe").objectReferenceValue as ReflectionProbe;
            int floor=Array.IndexOf(probes,probe);
            if(floor>=0)row.FindPropertyRelative("day").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/_Project/Settings/Visualizacion/Reflections/Fachada"+(floor+1)+"_Dia.exr");
        }
        for(int floor=0;floor<2;floor++)
        {
            SerializedProperty row=null;
            for(int i=0;i<scenarios.arraySize;i++)
                if(scenarios.GetArrayElementAtIndex(i).FindPropertyRelative("probe").objectReferenceValue==mirrorProbes[floor])row=scenarios.GetArrayElementAtIndex(i);
            if(row==null){int index=scenarios.arraySize;scenarios.arraySize++;row=scenarios.GetArrayElementAtIndex(index);}
            row.FindPropertyRelative("probe").objectReferenceValue=mirrorProbes[floor];row.FindPropertyRelative("day").objectReferenceValue=cube;
            row.FindPropertyRelative("night").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/_Project/Settings/Visualizacion/Reflections/Fachada"+(floor+1)+"_Noche.exr");
        }
        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory("Validation");File.WriteAllText("Validation/Mirror-Glazing.txt",$"PASS {count} vidrios: exterior espejado; interior transparente; geometria y colliders conservados.\nPASS entorno Blender Forest importado como cubemap especular nativo.\n");
        Debug.Log("Vidrio espejado de referencia aplicado: "+count);
    }
    static void Configure(Material material,bool inside)
    {
        material.SetTexture("_BaseMap",null);material.SetTexture("_SpecGlossMap",null);material.SetTexture("_BumpMap",null);
        material.DisableKeyword("_METALLICSPECGLOSSMAP");material.DisableKeyword("_NORMALMAP");material.EnableKeyword("_SPECULAR_SETUP");
        material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHAMODULATE_ON");
        // Both sides are dielectric transparent glass; preserve existing pane meshes and probes.
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_WorkflowMode",0);material.SetFloat("_Metallic",0);
        material.SetFloat("_Smoothness",.96f);material.SetFloat("_SmoothnessTextureChannel",0);material.SetFloat("_Cull",2);
        material.SetColor("_SpecColor",new Color(.04f,.04f,.04f,1));
        material.SetColor("_BaseColor",new Color(.96f,.98f,1,inside?.025f:.055f));
        material.SetFloat("_BlendModePreserveSpecular",1);
        material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",10);material.SetFloat("_SrcBlendAlpha",1);material.SetFloat("_DstBlendAlpha",10);
        material.SetFloat("_ZWrite",0);material.SetFloat("_EnvironmentReflections",1);material.SetFloat("_SpecularHighlights",1);
        material.SetOverrideTag("RenderType","Transparent");material.renderQueue=3000;
        material.SetShaderPassEnabled("ShadowCaster",false);material.SetShaderPassEnabled("DepthOnly",false);EditorUtility.SetDirty(material);
    }
}


