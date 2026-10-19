using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProyectoVR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class JourneyEntryRepair
{
 static Vector3 Divide(Vector3 a,Vector3 b)=>new Vector3(a.x/Mathf.Abs(b.x),a.y/Mathf.Abs(b.y),a.z/Mathf.Abs(b.z));
 static void PanelCollider(MeshFilter mf){
  foreach(var c in mf.GetComponents<Collider>())c.enabled=false;
  var box=mf.GetComponent<BoxCollider>();if(box==null)box=mf.gameObject.AddComponent<BoxCollider>();
  box.center=mf.sharedMesh.bounds.center;box.size=Vector3.Max(mf.sharedMesh.bounds.size,Divide(Vector3.one*.012f,mf.transform.lossyScale));box.isTrigger=false;box.enabled=true;
 }
 static void Bar(Transform parent,string name,Vector3 position,Vector3 size,Material material){
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=parent.gameObject.layer;
  go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(position,Quaternion.identity);go.transform.localScale=Divide(size,parent.lossyScale);go.GetComponent<Renderer>().sharedMaterial=material;
 }
 [MenuItem("Vivienda/Journey/Reparar accesos")]
 public static void Repair(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Salir de Play antes de reparar accesos.");
  int rails=0;
  foreach(var window in UnityEngine.Object.FindObjectsByType<WindowHinge>()){
   var pane=window.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name.ToLowerInvariant().Contains("muro cortina")&&r.bounds.center.z>3.2f);if(pane==null)continue;
   // Detach the user's original mesh before disabling the generated hierarchy.
   pane.transform.SetParent(window.transform.parent,true);
   var handle=pane.transform.Find("Manija Journey");if(handle!=null)handle.gameObject.SetActive(false);
   pane.gameObject.SetActive(true);pane.enabled=true;
   foreach(var c in pane.GetComponents<Collider>())c.enabled=false;
   var collider=pane.GetComponent<MeshCollider>();if(collider==null)collider=pane.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=pane.GetComponent<MeshFilter>().sharedMesh;collider.convex=false;collider.enabled=true;
   foreach(var interactable in window.GetComponentsInChildren<XRBaseInteractable>(true))interactable.enabled=false;
   window.enabled=false;window.gameObject.SetActive(false);rails++;
  }
  int count=0;
  foreach(var door in UnityEngine.Object.FindObjectsByType<DoorHinge>()){
   var mf=door.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name.Contains("Simple-A"));if(mf==null)continue;
   var serialized=new SerializedObject(door);var pivot=serialized.FindProperty("hingePivot").objectReferenceValue as Transform;var leaf=serialized.FindProperty("doorLeaf").objectReferenceValue as Transform;
   if(pivot==null||leaf==null)throw new InvalidOperationException("Acceso sin referencias de bisagra: "+door.name);
   var panel=leaf.Find("Panel opaco móvil");
   if(panel==null){
    panel=door.transform.Find("Marco fijo");if(panel==null||panel.GetComponent<MeshFilter>()==null)throw new InvalidOperationException("Falta panel secundario de acceso: "+door.name);
    panel.name="Panel opaco móvil";panel.SetParent(leaf,true);
   }
   PanelCollider(mf);PanelCollider(panel.GetComponent<MeshFilter>());panel.gameObject.layer=mf.gameObject.layer;
   if(door.transform.Find("Marco fijo")==null){
    var b=mf.GetComponent<Renderer>().bounds;b.Encapsulate(panel.GetComponent<Renderer>().bounds);
    var frame=new GameObject("Marco fijo");frame.layer=mf.gameObject.layer;frame.transform.SetParent(door.transform,true);
    bool wideX=b.size.x>b.size.z;float jamb=.04f;float depth=Mathf.Max(.08f,wideX?b.size.z:b.size.x);var material=JourneyGeometry.Material("Marcos acceso",new Color(.11f,.12f,.13f),.3f);
    if(wideX){
     Bar(frame.transform,"Jamba izquierda",new Vector3(b.min.x+jamb*.5f,b.center.y,b.center.z),new Vector3(jamb,b.size.y,depth),material);
     Bar(frame.transform,"Jamba derecha",new Vector3(b.max.x-jamb*.5f,b.center.y,b.center.z),new Vector3(jamb,b.size.y,depth),material);
     Bar(frame.transform,"Dintel",new Vector3(b.center.x,b.max.y-jamb*.5f,b.center.z),new Vector3(b.size.x,jamb,depth),material);
    }else{
     Bar(frame.transform,"Jamba izquierda",new Vector3(b.center.x,b.center.y,b.min.z+jamb*.5f),new Vector3(depth,b.size.y,jamb),material);
     Bar(frame.transform,"Jamba derecha",new Vector3(b.center.x,b.center.y,b.max.z-jamb*.5f),new Vector3(depth,b.size.y,jamb),material);
     Bar(frame.transform,"Dintel",new Vector3(b.center.x,b.max.y-jamb*.5f,b.center.z),new Vector3(depth,jamb,b.size.z),material);
    }
   }
   JourneyGeometry.Bind(door,pivot,leaf);count++;
  }
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();Debug.Log("Accesos reparados="+count+"; ambos paneles moviles; marcos huecos sin umbral. Barandillas restauradas="+rails);
 }
}
