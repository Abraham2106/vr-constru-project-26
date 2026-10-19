using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using ProyectoVR;
using ProyectoVR.Core;
using ProyectoVR.UI;
using Vivienda.Tools;

public static class JourneySceneSetup
{
 const string Audio="Assets/_Project/Audio/Generated/";
 static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value){var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Bool(UnityEngine.Object target,string name,bool value){var s=new SerializedObject(target);s.FindProperty(name).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
 static Vector3 Landing(Vector3 hint){
  float[] offsets={0,.25f,-.25f,.5f,-.5f,.9f,-.9f};
  foreach(var x in offsets)foreach(var z in offsets){var p=hint+new Vector3(x,0,z);var hits=Physics.RaycastAll(p+Vector3.up*.65f,Vector3.down,1.2f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>Mathf.Abs(h.point.y-hint.y));foreach(var hit in hits){if(hit.normal.y<.95f||Mathf.Abs(hit.point.y-hint.y)>.35f)continue;var feet=hit.point+Vector3.up*.04f;if(!Physics.CheckCapsule(feet+Vector3.up*.32f,feet+Vector3.up*1.4f,.28f,~0,QueryTriggerInteraction.Ignore))return feet;}}
  throw new InvalidOperationException("No safe floor near "+hint);
 }
 static Transform Marker(string name,Vector3 position,float yaw){var go=GameObject.Find(name)??new GameObject(name);go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));return go.transform;}
 static void Label(Transform parent,string value){var go=new GameObject("Etiqueta",typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,-.14f,.035f);go.transform.localRotation=Quaternion.Euler(0,180,0);go.transform.localScale=Vector3.one*.0014f;var t=go.GetComponent<TextMeshPro>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=21;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(230,65);t.text=value;t.color=Color.white;}
 static void Room(Transform parent,int floor,string name,Vector2 center,Vector3 switchPosition,Vector3 normal,float range){
  string id="Apartamento "+(floor+1)+" - "+name;var existing=parent.Find(id);if(existing!=null)return;
  float y=floor*3;var root=new GameObject(id);root.transform.SetParent(parent,false);root.transform.position=new Vector3(center.x,y,center.y);
  var group=root.AddComponent<RoomLightGroup>();var gs=new SerializedObject(group);gs.FindProperty("roomId").stringValue=id;gs.FindProperty("lightsOn").boolValue=false;gs.ApplyModifiedPropertiesWithoutUndo();
  float ceiling=y+2.65f;var hits=Physics.RaycastAll(new Vector3(center.x,y+1.8f,center.y),Vector3.up,1.4f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider.name.ToLowerInvariant().Contains("cielo")&&h.point.y>y+2.1f).OrderBy(h=>h.distance).ToArray();if(hits.Length>0)ceiling=hits[0].point.y-.05f;
  var lightGo=new GameObject("Luz "+name);lightGo.transform.SetParent(root.transform,false);lightGo.transform.position=new Vector3(center.x,ceiling-.10f,center.y);var light=lightGo.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.88f,.72f);light.intensity=.65f;light.range=range;light.shadows=LightShadows.None;light.enabled=false;
  var fixture=GameObject.CreatePrimitive(PrimitiveType.Cylinder);fixture.name="Plafon "+name;fixture.transform.SetParent(root.transform,true);fixture.transform.position=new Vector3(center.x,ceiling,center.y);fixture.transform.localScale=new Vector3(.25f,.025f,.25f);fixture.GetComponent<Collider>().enabled=false;
  var lampMat=JourneyGeometry.Material("Plafones",new Color(.76f,.78f,.79f));lampMat.EnableKeyword("_EMISSION");fixture.GetComponent<Renderer>().sharedMaterial=lampMat;
  gs=new SerializedObject(group);var list=gs.FindProperty("controlledLights");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=light;var renderers=gs.FindProperty("controlledFixtures");renderers.arraySize=1;renderers.GetArrayElementAtIndex(0).objectReferenceValue=fixture.GetComponent<Renderer>();gs.ApplyModifiedPropertiesWithoutUndo();
  var sw=new GameObject("Interruptor "+name);sw.transform.SetParent(root.transform,true);sw.transform.SetPositionAndRotation(switchPosition+Vector3.up*y,Quaternion.LookRotation(normal,Vector3.up));
  var plate=Cube("Placa",sw.transform,Vector3.zero,new Vector3(.11f,.17f,.025f),JourneyGeometry.Material("Interruptores",new Color(.80f,.82f,.82f)));
  var rocker=Cube("Tecla",sw.transform,new Vector3(0,0,.024f),new Vector3(.067f,.103f,.025f),JourneyGeometry.Material("Teclas",new Color(.19f,.22f,.25f)));rocker.GetComponent<Collider>().enabled=false;
  var interactable=sw.AddComponent<XRSimpleInteractable>();interactable.colliders.Clear();interactable.colliders.Add(plate.GetComponent<Collider>());interactable.interactionLayers=1;interactable.distanceCalculationMode=XRBaseInteractable.DistanceCalculationMode.ColliderVolume;
  var audio=sw.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.18f;audio.minDistance=.25f;audio.maxDistance=3;
  var switchComponent=sw.AddComponent<LightSwitch>();Ref(switchComponent,"interactable",interactable);Ref(switchComponent,"switchMesh",rocker.transform);Ref(switchComponent,"roomLights",group);Ref(switchComponent,"clickAudio",audio);Ref(switchComponent,"clickClip",AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"sfx_switch_click.wav"));Label(sw.transform,name);
 }
 [MenuItem("Vivienda/Journey/Completar requisitos")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first");
  var rig=UnityEngine.Object.FindAnyObjectByType<XROrigin>();
  var menu=UnityEngine.Object.FindObjectsByType<VrWorldMenuController>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
  var module=UnityEngine.Object.FindAnyObjectByType<XRUIInputModule>();
  var host=UnityEngine.Object.FindAnyObjectByType<ViviendaToolHost>();
  var cycle=UnityEngine.Object.FindAnyObjectByType<DayNightController>();
  var registry=UnityEngine.Object.FindAnyObjectByType<TeleportDestinationRegistry>();
  var palette=AssetDatabase.LoadAssetAtPath<ArchitecturalPaintPalette>("Assets/_Project/Prefabs/Tools/ArchitecturalPaintPalette.asset");
  if(rig==null||menu==null||module==null||host==null||host.WallPaint==null||host.Measure==null||cycle==null||registry==null||palette==null||rig.GetComponent<CharacterController>()==null||rig.GetComponent<PlayerSpawnBootstrap>()==null)
   throw new InvalidOperationException("Faltan referencias VR, herramientas, menu o paleta en la escena activa.");
  var log=new StringBuilder();int doors=JourneyGeometry.Doors();int windows=JourneyGeometry.Windows();int structure=JourneyGeometry.Structure();log.AppendLine("Puertas="+doors+" ventanas="+windows+" mallas fisicas="+structure);
  if(rig.GetComponent<DesktopPreview>()==null)rig.gameObject.AddComponent<DesktopPreview>();if(rig.GetComponent<JourneyToolHint>()==null)rig.gameObject.AddComponent<JourneyToolHint>();
  foreach(var n in rig.GetComponentsInChildren<NearFarInteractor>(true)){n.enabled=true;n.gameObject.SetActive(true);n.enableUIInteraction=true;n.enableFarCasting=true;n.enableNearCasting=true;n.allowHoveredActivate=true;}
  Bool(menu,"m_StartHidden",false);Ref(menu,"m_Palette",palette);
  foreach(var follow in menu.GetComponents<VrMenuFollowHead>())follow.enabled=false;
  var click=menu.GetComponent<UiClickSound>();if(click==null){if(menu.GetComponent<AudioSource>()==null)menu.gameObject.AddComponent<AudioSource>();click=menu.gameObject.AddComponent<UiClickSound>();}Ref(click,"m_ClickClip",AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"sfx_switch_click.wav"));menu.GetComponent<AudioSource>().volume=.25f;Ref(menu,"m_ClickSound",click);
  module.enableXRInput=true;module.enableMouseInput=true;
  host.WallPaint.Palette=palette;
  var measureMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Journey/Medicion.mat");if(measureMaterial==null){JourneyGeometry.EnsureFolder("Assets/_Project/Art/Materials/Journey");measureMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));measureMaterial.SetColor("_BaseColor",new Color(1,.78f,.05f));AssetDatabase.CreateAsset(measureMaterial,"Assets/_Project/Art/Materials/Journey/Medicion.mat");}Ref(host.Measure,"lineMaterial",measureMaterial);
  foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if(r.name.ToLowerInvariant().Contains("muro")&&!r.name.ToLowerInvariant().Contains("cortina")&&r.GetComponent<PaintableWall>()==null)r.gameObject.AddComponent<PaintableWall>();
  var roomRoot=GameObject.Find("Habitaciones interactivas")??new GameObject("Habitaciones interactivas");
  for(int floor=0;floor<2;floor++){
   Room(roomRoot.transform,floor,"Sala",new Vector2(3,.7f),new Vector3(3.31f,1.2f,2.66f),Vector3.back,2.65f);
   Room(roomRoot.transform,floor,"Dormitorio 1",new Vector2(-.5f,.7f),new Vector3(.20f,1.2f,-1.02f),Vector3.forward,2.65f);
   Room(roomRoot.transform,floor,"Dormitorio 2",new Vector2(-1,-2.8f),new Vector3(.35f,1.2f,-2.48f),Vector3.left,2.35f);
   Room(roomRoot.transform,floor,"Baño",new Vector2(1.18f,-3.35f),new Vector3(1.65f,1.2f,-2.30f),Vector3.back,1.7f);
   Room(roomRoot.transform,floor,"Cocina",new Vector2(3.2f,-2.75f),new Vector3(2.02f,1.2f,-2.75f),Vector3.right,2.4f);
   Room(roomRoot.transform,floor,"Lavanderia",new Vector2(3.25f,-5.35f),new Vector3(3.3f,1.2f,-4.60f),Vector3.back,1.9f);
  }
  var cso=new SerializedObject(cycle);cso.FindProperty("exteriorNightLights").arraySize=0;cso.ApplyModifiedPropertiesWithoutUndo();
  foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.name=="Luz_Piso1"||light.name=="Luz_Piso2"||light.name=="Exterior Fill Light")light.enabled=false;
  var exteriorLights=GameObject.Find("Iluminacion accesos")??new GameObject("Iluminacion accesos");
  if(exteriorLights.transform.childCount==0)for(int floor=0;floor<2;floor++){var go=new GameObject("Acceso "+(floor+1));go.transform.SetParent(exteriorLights.transform);go.transform.SetPositionAndRotation(new Vector3(4.1f,2.4f+3*floor,3.1f),Quaternion.LookRotation(new Vector3(0,-1,.35f)));var light=go.AddComponent<Light>();light.type=LightType.Spot;light.range=3.4f;light.spotAngle=85;light.color=new Color(1,.86f,.68f);light.intensity=.8f;light.shadows=LightShadows.None;light.enabled=false;}
  cso=new SerializedObject(cycle);var nightLights=exteriorLights.GetComponentsInChildren<Light>();var array=cso.FindProperty("exteriorNightLights");array.arraySize=nightLights.Length;for(int i=0;i<nightLights.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=nightLights[i];cso.ApplyModifiedPropertiesWithoutUndo();
  if(rig.GetComponentInChildren<WindowAmbience>()==null){var go=new GameObject("Ambiente exterior");go.transform.SetParent(rig.transform,false);var audio=go.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"amb_exterior_loop.wav");audio.playOnAwake=true;audio.loop=true;audio.volume=.015f;go.AddComponent<WindowAmbience>();}
  var cc=rig.GetComponent<CharacterController>();bool wasEnabled=cc.enabled;cc.enabled=false;
  Vector3 exterior,first,second;
  try{Physics.SyncTransforms();exterior=Landing(new Vector3(.2f,0,9.8f));first=Landing(new Vector3(3,0,.7f));second=Landing(new Vector3(3,3,.7f));}finally{cc.enabled=wasEnabled;}
  var spawn=Marker("PlayerSpawn",exterior,180);var targets=new[]{Marker("TP_Exterior",exterior,180),Marker("TP_Piso1",first,180),Marker("TP_Piso2",second,180)};
  Ref(registry,"m_Exterior",targets[0]);Ref(registry,"m_Piso1",targets[1]);Ref(registry,"m_Piso2",targets[2]);
  var bootstrap=rig.GetComponent<PlayerSpawnBootstrap>();Ref(bootstrap,"m_PlayerSpawn",spawn);
  cc.enabled=false;rig.transform.SetPositionAndRotation(exterior,Quaternion.Euler(0,180,0));cc.enabled=wasEnabled;
  log.AppendLine("Habitaciones="+roomRoot.GetComponentsInChildren<RoomLightGroup>().Length+" luces nocturnas="+nightLights.Length);
  foreach(var t in targets)log.AppendLine(t.name+"="+t.position);
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
  File.WriteAllText("Temp/JourneySetup.txt",log.ToString());Debug.Log(log.ToString());
 }
}
