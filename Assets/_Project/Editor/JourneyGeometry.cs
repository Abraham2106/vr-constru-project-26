using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ProyectoVR;

public static class JourneyGeometry
{
 const string Folder="Assets/_Project/Art/Models/Journey";
 public static void EnsureFolder(string path){var p="Assets";foreach(var part in path.Split('/').Skip(1)){var next=p+"/"+part;if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(p,part);p=next;}}
 static int Find(int[] parent,int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
 static string Safe(string s)=>new string(s.Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray());
 static Mesh Part(Mesh source,HashSet<int> selected,bool inside,string path){
  var mesh=UnityEngine.Object.Instantiate(source);mesh.name=(inside?"JourneyLeaf_":"JourneyFrame_")+source.name;
  var vertices=source.vertices;var used=new HashSet<int>();
  for(int s=0;s<source.subMeshCount;s++){var input=source.GetTriangles(s);var list=new List<int>();for(int i=0;i<input.Length;i+=3)if(selected.Contains(input[i])==inside){list.Add(input[i]);list.Add(input[i+1]);list.Add(input[i+2]);used.Add(input[i]);used.Add(input[i+1]);used.Add(input[i+2]);}mesh.SetTriangles(list,s,false);}
  var first=used.First();var bounds=new Bounds(vertices[first],Vector3.zero);foreach(int i in used)bounds.Encapsulate(vertices[i]);mesh.bounds=bounds;
  var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);return existing;}
  AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 static Vector3 Divide(Vector3 a,Vector3 b)=>new Vector3(a.x/b.x,a.y/b.y,a.z/b.z);
 public static void LeafCollider(MeshFilter mf){
  foreach(var c in mf.GetComponents<Collider>())c.enabled=false;
  var box=mf.GetComponent<BoxCollider>();if(box==null)box=mf.gameObject.AddComponent<BoxCollider>();
  var s=mf.transform.lossyScale;var min=new Vector3(.012f/Mathf.Max(.000001f,Mathf.Abs(s.x)),.012f/Mathf.Max(.000001f,Mathf.Abs(s.y)),.012f/Mathf.Max(.000001f,Mathf.Abs(s.z)));
  box.center=mf.sharedMesh.bounds.center;box.size=Vector3.Max(mf.sharedMesh.bounds.size,min);box.isTrigger=false;box.enabled=true;
 }
 public static Material Material(string name,Color color,float metal=0){
  const string folder="Assets/_Project/Art/Materials/Journey";EnsureFolder(folder);var path=folder+"/"+name+".mat";
  var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",.35f);EditorUtility.SetDirty(mat);return mat;
 }
 public static void Bind(Component hinge,Transform pivot,Transform leaf){
  var simple=hinge.GetComponent<XRSimpleInteractable>()??hinge.gameObject.AddComponent<XRSimpleInteractable>();simple.enabled=true;simple.interactionLayers=1;simple.distanceCalculationMode=XRBaseInteractable.DistanceCalculationMode.ColliderVolume;
  foreach(var old in hinge.GetComponentsInChildren<XRGrabInteractable>(true))old.enabled=false;
  simple.colliders.Clear();foreach(var c in leaf.GetComponentsInChildren<Collider>(true))if(c.enabled&&!c.isTrigger)simple.colliders.Add(c);
  var so=new SerializedObject(hinge);so.FindProperty("hingePivot").objectReferenceValue=pivot;var prop=so.FindProperty("doorLeaf");if(prop!=null)prop.objectReferenceValue=leaf;so.FindProperty("handleGrab").objectReferenceValue=simple;
  bool bedroom=hinge is DoorHinge&&hinge.name.StartsWith("Puerta_")&&(hinge.name.Contains("_02_")||hinge.name.Contains("_03_")||hinge.name.Contains("_07_")||hinge.name.Contains("_08_"));prop=so.FindProperty("hingeOnPositiveY");if(prop!=null)prop.boolValue=!bedroom;prop=so.FindProperty("snapSpeed");if(prop!=null)prop.floatValue=200f;prop=so.FindProperty("maxAngle");if(prop!=null)prop.floatValue=bedroom?90f:105f;so.ApplyModifiedPropertiesWithoutUndo();
 }
 public static void Handle(Transform leaf,Bounds bounds,bool wideX){
  // The old grab proxies are no longer targets; leaving their colliders alive
  // makes the ray hit an invisible handle instead of the moving leaf.
  foreach(var t in leaf.GetComponentsInChildren<Transform>(true))if(t.name=="HandleInteractable"){
   foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;
   foreach(var grab in t.GetComponentsInChildren<XRGrabInteractable>(true))grab.enabled=false;
  }
  var existing=leaf.Find("Manija Journey");if(existing!=null)return;
  foreach(var r in leaf.GetComponentsInChildren<Renderer>(true))if(r.name.Contains("Manija_VIS"))r.enabled=false;
  var handle=GameObject.CreatePrimitive(PrimitiveType.Cube);handle.name="Manija Journey";handle.transform.SetParent(leaf,true);
  var point=bounds.center;if(wideX)point.x=bounds.max.x-.10f;else point.z=bounds.max.z-.10f;point.y=bounds.min.y+Mathf.Min(1.05f,bounds.size.y*.6f);
  if(wideX)point.z=bounds.center.z-.055f;else point.x=bounds.center.x-.055f;
  handle.transform.SetPositionAndRotation(point,wideX?Quaternion.identity:Quaternion.Euler(0,90,0));handle.transform.localScale=Divide(new Vector3(.13f,.025f,.045f),leaf.lossyScale);
  handle.GetComponent<Renderer>().sharedMaterial=Material("Herrajes",new Color(.48f,.51f,.53f),.7f);
 }
 public static int Doors(){
  EnsureFolder(Folder);
  foreach(var window in UnityEngine.Object.FindObjectsByType<WindowHinge>(FindObjectsSortMode.None))if(window.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.name.Contains("Simple-A"))){window.enabled=false;if(window.GetComponent<DoorHinge>()==null)window.gameObject.AddComponent<DoorHinge>();}
  int count=0;
  foreach(var door in UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None)){
   var mf=door.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name.Contains("door-trustile")||f.name.Contains("Simple-A"));if(mf==null)continue;
   var meshPath=AssetDatabase.GetAssetPath(mf.sharedMesh).Replace('\\','/');
   bool alreadySplit=meshPath.StartsWith(Folder+"/",StringComparison.Ordinal)&&meshPath.EndsWith("_Leaf.asset",StringComparison.OrdinalIgnoreCase);
   if(alreadySplit||mf.sharedMesh.name.StartsWith("JourneyLeaf_")){
    var stored=new SerializedObject(door);var storedPivot=stored.FindProperty("hingePivot").objectReferenceValue as Transform;var storedLeaf=stored.FindProperty("doorLeaf").objectReferenceValue as Transform;
    if(storedPivot==null){var old=door.GetComponent<WindowHinge>();if(old!=null)storedPivot=new SerializedObject(old).FindProperty("hingePivot").objectReferenceValue as Transform;}
    if(storedPivot==null)throw new InvalidOperationException("Separated door has no hinge pivot: "+door.name);
    if(storedLeaf==null){storedLeaf=mf.transform;while(storedLeaf.parent!=null&&storedLeaf.parent!=storedPivot)storedLeaf=storedLeaf.parent;}
    LeafCollider(mf);Handle(storedLeaf,mf.GetComponent<Renderer>().bounds,mf.GetComponent<Renderer>().bounds.size.x>mf.GetComponent<Renderer>().bounds.size.z);Bind(door,storedPivot,storedLeaf);count++;continue;
   }
   var source=mf.sharedMesh;var vertices=source.vertices;var parent=Enumerable.Range(0,vertices.Length).ToArray();var weld=new Dictionary<Vector3Int,int>();
   for(int i=0;i<vertices.Length;i++){var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*10000),Mathf.RoundToInt(v.y*10000),Mathf.RoundToInt(v.z*10000));if(weld.TryGetValue(key,out var j))parent[Find(parent,i)]=Find(parent,j);else weld[key]=i;}
   var triangles=source.triangles;for(int i=0;i<triangles.Length;i+=3){parent[Find(parent,triangles[i+1])]=Find(parent,triangles[i]);parent[Find(parent,triangles[i+2])]=Find(parent,triangles[i]);}
   var groups=Enumerable.Range(0,vertices.Length).GroupBy(i=>Find(parent,i)).Select(g=>g.ToArray()).ToArray();
   var all=mf.GetComponent<Renderer>().bounds;bool wideX=all.size.x>all.size.z;float width=wideX?all.size.x:all.size.z;
   var options=groups.Select(g=>{var b=new Bounds(mf.transform.TransformPoint(vertices[g[0]]),Vector3.zero);foreach(int i in g)b.Encapsulate(mf.transform.TransformPoint(vertices[i]));return new {indices=g,bounds=b};}).Where(g=>(wideX?g.bounds.size.x:g.bounds.size.z)>width*.55f&&g.bounds.size.y>all.size.y*.65f).OrderBy(g=>wideX?g.bounds.size.z:g.bounds.size.x).ToArray();
   if(options.Length<2)throw new InvalidOperationException("Cannot separate frame/leaf: "+door.name);
   var selected=new HashSet<int>(options[0].indices);var name=Safe(door.name);var panel=Part(source,selected,true,Folder+"/"+name+"_Leaf.asset");var frameMesh=Part(source,selected,false,Folder+"/"+name+"_Frame.asset");
   var so=new SerializedObject(door);Transform pivot=so.FindProperty("hingePivot").objectReferenceValue as Transform;
   if(pivot==null){var old=door.GetComponent<WindowHinge>();if(old!=null)pivot=new SerializedObject(old).FindProperty("hingePivot").objectReferenceValue as Transform;}
   if(pivot==null)pivot=mf.transform.parent.parent;
   var leaf=mf.transform.parent;while(leaf.parent!=null&&leaf.parent!=pivot)leaf=leaf.parent;
   var fixedFrame=new GameObject("Marco fijo");fixedFrame.transform.SetParent(door.transform,false);fixedFrame.transform.SetPositionAndRotation(mf.transform.position,mf.transform.rotation);fixedFrame.transform.localScale=Divide(mf.transform.lossyScale,door.transform.lossyScale);
   fixedFrame.AddComponent<MeshFilter>().sharedMesh=frameMesh;fixedFrame.AddComponent<MeshRenderer>().sharedMaterials=mf.GetComponent<Renderer>().sharedMaterials;fixedFrame.AddComponent<MeshCollider>().sharedMesh=frameMesh;
   mf.sharedMesh=panel;
   LeafCollider(mf);
   var position=leaf.position;var rotation=leaf.rotation;
   var hinge=options[0].bounds.center;if(wideX)hinge.x=options[0].bounds.min.x;else hinge.z=options[0].bounds.min.z;
   pivot.SetPositionAndRotation(hinge,Quaternion.identity);leaf.SetPositionAndRotation(position,rotation);
   Handle(leaf,options[0].bounds,wideX);Bind(door,pivot,leaf);count++;
   var audio=door.GetComponent<AudioSource>()??door.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.loop=false;audio.spatialBlend=1;audio.volume=.15f;audio.maxDistance=3;
   so=new SerializedObject(door);so.FindProperty("latchAudio").objectReferenceValue=audio;so.FindProperty("latchClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Generated/sfx_latch.wav");so.ApplyModifiedPropertiesWithoutUndo();
  }
  return count;
 }
 static void Bar(Transform root,string name,Vector3 position,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,true);go.transform.position=position;go.transform.localScale=Divide(scale,root.lossyScale);go.GetComponent<Renderer>().sharedMaterial=material;}
 public static int Windows(){
  var material=Material("Marco ventanas",new Color(.055f,.065f,.075f),.45f);int count=0;
  var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.ToLowerInvariant().Contains("muro cortina")).ToArray();
  foreach(var r in renderers){var b=r.bounds;float floor=b.center.y>=3?3:0;float level=b.center.y-floor;float width=Mathf.Max(b.size.x,b.size.z);bool wideX=b.size.x>b.size.z;
   if(b.center.z>3.2f)continue;
   if(level<.7f||level>2.05f||b.size.y<.6f||b.size.y>2.25f||width<.55f||width>2.3f||Mathf.Min(b.size.x,b.size.z)>.15f)continue;
   var old=r.GetComponentInParent<WindowHinge>();if(old!=null&&old.enabled){var stored=new SerializedObject(old);var storedPivot=stored.FindProperty("hingePivot").objectReferenceValue as Transform;if(storedPivot!=null){LeafCollider(r.GetComponent<MeshFilter>());Handle(r.transform,b,wideX);Bind(old,storedPivot,r.transform);}count++;continue;}
   var root=new GameObject("Ventana VR "+(++count));root.transform.SetParent(r.transform.parent,true);
   var pivot=new GameObject("Bisagra").transform;pivot.SetParent(root.transform,true);var p=b.center;if(wideX)p.x=b.min.x;else p.z=b.min.z;pivot.SetPositionAndRotation(p,Quaternion.identity);
   r.transform.SetParent(pivot,true);foreach(var c in r.GetComponents<Collider>())c.enabled=false;
   var mf=r.GetComponent<MeshFilter>();LeafCollider(mf);
   var frame=new GameObject("Marco fijo").transform;frame.SetParent(root.transform,true);
   if(wideX){Bar(frame,"Jamba izquierda",new Vector3(b.min.x-.025f,b.center.y,b.center.z),new Vector3(.05f,b.size.y+.1f,.065f),material);Bar(frame,"Jamba derecha",new Vector3(b.max.x+.025f,b.center.y,b.center.z),new Vector3(.05f,b.size.y+.1f,.065f),material);Bar(frame,"Dintel",new Vector3(b.center.x,b.max.y+.025f,b.center.z),new Vector3(b.size.x,.05f,.065f),material);Bar(frame,"Alfeizar",new Vector3(b.center.x,b.min.y-.025f,b.center.z),new Vector3(b.size.x,.05f,.065f),material);}
   else{Bar(frame,"Jamba izquierda",new Vector3(b.center.x,b.center.y,b.min.z-.025f),new Vector3(.065f,b.size.y+.1f,.05f),material);Bar(frame,"Jamba derecha",new Vector3(b.center.x,b.center.y,b.max.z+.025f),new Vector3(.065f,b.size.y+.1f,.05f),material);Bar(frame,"Dintel",new Vector3(b.center.x,b.max.y+.025f,b.center.z),new Vector3(.065f,.05f,b.size.z),material);Bar(frame,"Alfeizar",new Vector3(b.center.x,b.min.y-.025f,b.center.z),new Vector3(.065f,.05f,b.size.z),material);}
   Handle(r.transform,b,wideX);var window=root.AddComponent<WindowHinge>();Bind(window,pivot,r.transform);
   var audio=root.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.1f;audio.maxDistance=3;
   var so=new SerializedObject(window);so.FindProperty("openCloseAudio").objectReferenceValue=audio;so.FindProperty("openClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Generated/sfx_window_open.wav");so.FindProperty("closeClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Generated/sfx_window_close.wav");so.ApplyModifiedPropertiesWithoutUndo();
  }
  return count;
 }
 public static int Structure(){int count=0;
  foreach(var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
   var name=mf.name.ToLowerInvariant();if(!(name.StartsWith("muro")||name.StartsWith("suelo")))continue;
   var door=mf.GetComponentInParent<DoorHinge>();var window=mf.GetComponentInParent<WindowHinge>();if(door!=null&&door.enabled||window!=null&&window.enabled)continue;
   foreach(var b in mf.GetComponents<BoxCollider>())b.enabled=false;
   var mc=mf.GetComponent<MeshCollider>();if(mc==null)mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;mc.convex=false;mc.enabled=true;count++;
  }
  Physics.SyncTransforms();return count;
 }
}


