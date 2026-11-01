using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Rebuilds only the residential garden from existing, attributed project assets.</summary>
public static class ResidentialGarden
{
    const string RootPath = "Assets/_Project/Art/ResidentialGarden";
    const string GardenAssets = "Assets/_Project/Art/ThirdParty/JardinCliente";

    [MenuItem("Vivienda/Jardin/Aplicar jardin residencial")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Vivienda_Prueba.unity")
            throw new InvalidOperationException("Abrir Vivienda_Prueba fuera de Play.");
        System.IO.Directory.CreateDirectory(RootPath);
        AssetDatabase.Refresh();
        var previous = GameObject.Find("Jardin_Residencial");
        if (previous) UnityEngine.Object.DestroyImmediate(previous);
        var root = new GameObject("Jardin_Residencial").transform;
        var fences = Group("Cercas_listones", root);
        var plants = Group("Macizos_y_setos", root);
        var crowns = Group("Copas_frondosas", root);
        var edges = Group("Bordes_jardineras", root);
        var oldFences = GameObject.Find("Vecindario_Placeholder").transform.Find("Vallas_linderos");
        var metal = Material("Cerca_Antracita", new Color(.105f,.12f,.115f), .18f);
        var gravel = Material("Borde_Grava", new Color(.5f,.49f,.43f), 0);
        var bed = Material("Tierra_jardin", new Color(.095f,.075f,.045f), 0);
        var fenceMesh = MakeFence();
        foreach (Transform old in oldFences)
        {
            var r = old.GetComponent<Renderer>();
            var go = MeshObject("Cerca_listones", fences, fenceMesh, metal);
            go.transform.position = new Vector3(r.bounds.center.x, -.045f, r.bounds.center.z);
        }
        oldFences.gameObject.SetActive(false);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(cube);
        foreach (float x in new[]{-5.9f,6.25f})
        {
            Box("Banda_grava", edges, cubeMesh, gravel, new Vector3(x,-.028f,2.1f), new Vector3(1.5f,.025f,15.9f));
            Box("Macizo_tierra", edges, cubeMesh, bed, new Vector3(x,-.009f,2.1f), new Vector3(1.32f,.025f,15.7f));
        }
        var hedgeModel = AssetDatabase.LoadAssetAtPath<GameObject>(GardenAssets+"/Models/hedge.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
        var hedgeMat = AssetDatabase.LoadAssetAtPath<Material>(GardenAssets+"/Materials/Hedge.mat");
        var fernSource = GameObject.Find("Jardin_Cliente").GetComponentsInChildren<MeshFilter>().First(x=>x.name.StartsWith("Helecho_"));
        var fernMat = AssetDatabase.LoadAssetAtPath<Material>(GardenAssets+"/Materials/Fern.mat");
        var lib = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/RubberduckVegetation/VegetationLibrary.fbx");
        var bushSource = lib.GetComponentsInChildren<MeshFilter>().First(x=>x.name=="bush_lod_1");
        var leaves = AssetDatabase.LoadAssetAtPath<Material>(GardenAssets+"/Materials/Leaves.mat");
        var bark = AssetDatabase.LoadAssetAtPath<Material>(GardenAssets+"/Materials/Bark.mat");
        var random = new System.Random(2411);
        int count=0;
        foreach(float x in new[]{-6.45f,6.65f})
        for(int i=0;i<11;i++)
        {
            var go=MeshObject("Seto_bajo_"+count++,plants,hedgeModel,hedgeMat);
            go.transform.position=new Vector3(x,-.04f,-5.1f+i*1.4f);
            go.transform.rotation=Quaternion.Euler(0,90,0);
            go.transform.localScale=new Vector3(.94f,.52f,1.12f);
        }
        foreach(float x in new[]{-5.65f,5.72f})
        for(int i=0;i<14;i++)
        {
            float z=-4.8f+i*1.05f;
            var fern=MeshObject("Helecho_macizo_"+i,plants,fernSource.sharedMesh,fernMat);
            fern.transform.position=new Vector3(x+(float)(random.NextDouble()-.5)*.3f,.02f,z);
            fern.transform.rotation=Quaternion.Euler(-90,(float)random.NextDouble()*360,0);
            // Source fern is approximately 14 cm. Size as a garden plant, not a miniature.
            fern.transform.localScale=Vector3.one*(5.5f+(float)random.NextDouble()*1.8f);
            if(i%2==0)
            {
                var shrub=MeshObject("Arbusto_"+i,plants,bushSource.sharedMesh,leaves);
                shrub.GetComponent<MeshRenderer>().sharedMaterials=new[]{bark,leaves};
                shrub.transform.position=new Vector3(x+(x<0?-.25f:.25f),-.04f,z+.5f);
                shrub.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                shrub.transform.localScale=new Vector3(.75f,.55f,.75f);
            }
        }
        // Front clusters frame the entrance without covering the stair or the driveway.
        foreach(float side in new[]{-1f,1f})
        for(int i=0;i<4;i++)
        {
            float x=side<0?-4.6f-i*.48f:5.3f+i*.38f;
            var shrub=MeshObject("Grupo_frontal_"+side+"_"+i,plants,bushSource.sharedMesh,leaves);
            shrub.GetComponent<MeshRenderer>().sharedMaterials=new[]{bark,leaves};
            shrub.transform.position=new Vector3(x,-.04f,8.1f+(i%2)*.45f);
            shrub.transform.rotation=Quaternion.Euler(0,i*73,0);
            shrub.transform.localScale=new Vector3(.8f,.5f+(i%2)*.15f,.8f);
        }
        // Add leaves only: never duplicate tree trunks or their branches.
        var trees=GameObject.Find("Jardin_Cliente").transform.Find("Arboles_Ligeros");
        var originalMesh=trees.GetChild(2).GetComponent<MeshFilter>().sharedMesh;
        var canopy=UnityEngine.Object.Instantiate(originalMesh);
        canopy.name="Copa_solo_hojas";
        var triangles=canopy.GetTriangles(0);canopy.subMeshCount=1;canopy.SetTriangles(triangles,0);canopy.RecalculateBounds();
        canopy=SaveMesh(canopy,"Copa_solo_hojas");
        foreach(Transform tree in trees)
        for(int layer=0;layer<3;layer++)
        {
            var crown=MeshObject("Copa_"+tree.name+"_"+layer,crowns,canopy,leaves);
            crown.transform.position=tree.position+new Vector3(0,layer*.12f,0);
            crown.transform.rotation=tree.rotation*Quaternion.Euler(0,65+layer*115,0);
            crown.transform.localScale=Vector3.Scale(tree.localScale,new Vector3(1.05f,1,1.05f));
        }
        var ground=GameObject.Find("Vivienda").GetComponentsInChildren<MeshRenderer>().First(x=>x.name=="Suelo Zacate [693790]");
        string lawnPath=RootPath+"/Cesped_Residencial.mat";
        var lawn=AssetDatabase.LoadAssetAtPath<Material>(lawnPath);
        if(!lawn){lawn=new Material(ground.sharedMaterial);AssetDatabase.CreateAsset(lawn,lawnPath);}
        lawn.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Vivienda/58_grass-8_diffuse_jpg.jpg"));
        lawn.SetTexture("_BumpMap",null);lawn.SetTexture("_MetallicGlossMap",null);lawn.SetTexture("_EmissionMap",null);
        lawn.DisableKeyword("_NORMALMAP");lawn.DisableKeyword("_METALLICSPECGLOSSMAP");lawn.DisableKeyword("_EMISSION");
        lawn.SetColor("_EmissionColor",Color.black);
        lawn.SetColor("_BaseColor",new Color(.72f,.76f,.67f,1));
        lawn.SetFloat("_Smoothness",0);lawn.SetFloat("_Metallic",0);
        lawn.SetTextureScale("_BaseMap",new Vector2(.04f,.04f));
        ground.sharedMaterial=lawn;
        EditorUtility.SetDirty(lawn);
        AddGrass(root);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("Jardin residencial aplicado: 8 cercas combinadas, 22 setos, 28 helechos, 14 arbustos, 12 capas de copa; casa intacta.");
    }
    static Transform Group(string n,Transform parent){var t=new GameObject(n).transform;t.SetParent(parent,false);return t;}
    static void AddGrass(Transform root)
    {
        string path=GardenAssets+"/Models/grass.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var sources=model.GetComponentsInChildren<MeshFilter>();
        string materialPath=RootPath+"/Pasto_3D.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(GardenAssets+"/Materials/Grass.mat"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetColor("_BaseColor",new Color(.62f,.72f,.49f,1));material.enableInstancing=true;
        EditorUtility.SetDirty(material);
        var group=Group("Pasto_3D_por_sectores",root);
        var sectors=new Dictionary<string,List<CombineInstance>>();
        var random=new System.Random(731);
        for(float x=-6.9f;x<6.9f;x+=.30f)
        for(float z=-6.8f;z<12f;z+=.30f)
        {
            // Footprint and generous margin around the patio, stair and all access routes.
            if(x> -2.8f && x<5.2f && z<10.1f)continue;
            // Keep grass out of the two planted soil beds.
            if((x> -6.65f&&x< -5.15f)||(x>5.5f&&x<7f))continue;
            float px=x+(float)(random.NextDouble()-.5)*.07f;
            float pz=z+(float)(random.NextDouble()-.5)*.07f;
            string key=Mathf.FloorToInt((x+7)/3)+"_"+Mathf.FloorToInt((z+7)/3);
            if(!sectors.ContainsKey(key))sectors[key]=new List<CombineInstance>();
            float size=240f+(float)random.NextDouble()*15f;
            var matrix=Matrix4x4.TRS(new Vector3(px,-.045f,pz),Quaternion.Euler(0,(float)random.NextDouble()*360,0),new Vector3(size,115f,size));
            var source=sources[random.Next(sources.Length)];
            sectors[key].Add(new CombineInstance{mesh=source.sharedMesh,transform=matrix});
        }
        foreach(var pair in sectors)
        {
            var mesh=new Mesh{name="Pasto_sector_"+pair.Key,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            mesh=SaveMesh(mesh,mesh.name);
            var go=MeshObject(mesh.name,group,mesh,material);
            // Small blades receive sunlight but do not add hundreds of shadow casters.
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
    }
    static Material Material(string name,Color color,float smoothness)
    {
        string path=RootPath+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material mat)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.lightProbeUsage=LightProbeUsage.BlendProbes;return go;
    }
    static void Box(string n,Transform p,Mesh mesh,Material mat,Vector3 position,Vector3 scale){var go=MeshObject(n,p,mesh,mat);go.transform.position=position;go.transform.localScale=scale;}
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        string path=RootPath+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing)
        {
            existing.Clear();existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;
            existing.normals=mesh.normals;existing.uv=mesh.uv;existing.tangents=mesh.tangents;
            existing.subMeshCount=mesh.subMeshCount;
            for(int i=0;i<mesh.subMeshCount;i++)existing.SetTriangles(mesh.GetTriangles(i),i);
            existing.bounds=mesh.bounds;existing.UploadMeshData(false);EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(mesh);return existing;
        }
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh MakeFence()
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=cube.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(cube);
        var parts=new List<CombineInstance>();
        for(int bay=0;bay<9;bay++)
        {
            float z=-8+bay*2;
            parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(0,.7f,z),Quaternion.identity,new Vector3(.12f,1.4f,.12f))});
            if(bay==8)continue;
            for(int row=0;row<10;row++)parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(0,.1f+row*.135f,z+1),Quaternion.identity,new Vector3(.065f,.105f,1.9f))});
        }
        var result=new Mesh{name="Cerca_listones_16m"};result.CombineMeshes(parts.ToArray(),true,true);return SaveMesh(result,"Cerca_listones_16m");
    }
}
