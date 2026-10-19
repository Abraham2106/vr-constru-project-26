using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ProyectoVR;

[InitializeOnLoad]
public static class ViviendaInteractionCheck
{
 const string Flag="Vivienda.InteractionCheck";
 static readonly StringBuilder report=new StringBuilder();
 static int phase,errors;
 static float next;
 static DoorHinge[] doors;
 static WindowHinge[] windows;
 static XRInteractionManager manager;
 static NearFarInteractor hand;
 static ViviendaInteractionCheck(){EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Vivienda/Comprobar interacciones y luces")]
 public static void Begin(){SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
 static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;if(errors<4)report.AppendLine(message+"\n"+trace);}}
 static void State(PlayModeStateChange state){
  if(!SessionState.GetBool(Flag,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){report.Clear();errors=0;phase=0;next=1;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;}
  if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SessionState.SetBool(Flag,false);}
 }
 static XRSimpleInteractable Simple(Component c)=>c.GetComponent<XRSimpleInteractable>();
 static void Select(XRSimpleInteractable target){manager.SelectEnter((IXRSelectInteractor)hand,(IXRSelectInteractable)target);manager.SelectExit((IXRSelectInteractor)hand,(IXRSelectInteractable)target);}
 static int Lights()=>UnityEngine.Object.FindObjectsByType<Light>().Count(l=>l.enabled);
 static bool RayHits(XRSimpleInteractable target){
  var colliders=target.colliders.Where(c=>c!=null&&c.enabled&&!c.isTrigger).ToArray();if(colliders.Length==0)return false;
  var bounds=colliders[0].bounds;foreach(var c in colliders.Skip(1))bounds.Encapsulate(c.bounds);
  var normal=bounds.size.x<bounds.size.z?Vector3.right:Vector3.forward;
  foreach(float sign in new[]{-1f,1f})if(Physics.Raycast(bounds.center+normal*sign*(Mathf.Min(bounds.size.x,bounds.size.z)/2f+0.2f),-normal*sign,out var hit,1f,~0,QueryTriggerInteraction.Ignore)&&manager.TryGetInteractableForCollider(hit.collider,out var mapped)&&(mapped as UnityEngine.Object)==target)return true;
  return false;
 }
 static void Tick(){
  if(!EditorApplication.isPlaying||Time.time<next)return;
  try{
   if(phase==0){
    doors=UnityEngine.Object.FindObjectsByType<DoorHinge>();windows=UnityEngine.Object.FindObjectsByType<WindowHinge>();manager=UnityEngine.Object.FindAnyObjectByType<XRInteractionManager>();
    var hands=UnityEngine.Object.FindObjectsByType<NearFarInteractor>();hand=hands.First(h=>h.transform.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()!=null);
    report.AppendLine("Near/far activos="+hands.Count(h=>h.isActiveAndEnabled));
    int mapped=doors.Count(d=>Simple(d)!=null&&Simple(d).colliders.Any(c=>manager.TryGetInteractableForCollider(c,out var mappedTarget)&&(mappedTarget as UnityEngine.Object)==Simple(d)));
    report.AppendLine("Puertas registradas XR="+mapped+"/"+doors.Length+" rayos fisicos="+doors.Count(d=>RayHits(Simple(d)))+"/"+doors.Length);
    report.AppendLine("Ventanas con objetivo XR="+windows.Count(w=>Simple(w)!=null)+"/"+windows.Length);
    report.AppendLine("Luces dia="+Lights());
    foreach(var d in doors)Select(Simple(d));foreach(var w in windows)Select(Simple(w));next=Time.time+1.2f;phase++;return;
   }
   if(phase==1){
    report.AppendLine("Puertas abiertas por seleccion XR="+doors.Count(d=>d.CurrentAngle>90f)+"/"+doors.Length);
    report.AppendLine("Ventanas abiertas="+windows.Count(w=>w.IsOpen)+"/"+windows.Length);
    foreach(var d in doors)Select(Simple(d));foreach(var w in windows)Select(Simple(w));DayNightController.Instance.StartNight();next=Time.time+3.6f;phase++;return;
   }
   if(phase==2){
    report.AppendLine("Puertas cerradas por seleccion XR="+doors.Count(d=>d.CurrentAngle<0.1f)+"/"+doors.Length);
    report.AppendLine("Ventanas cerradas="+windows.Count(w=>!w.IsOpen)+"/"+windows.Length);
    report.AppendLine("Luces noche="+Lights());DayNightController.Instance.StartDay();next=Time.time+3.6f;phase++;return;
   }
   report.AppendLine("Luces al volver al dia="+Lights());report.AppendLine("Errores Play="+errors);
  }catch(Exception e){report.AppendLine("CHECK FAILED: "+e);}
  File.WriteAllText("Temp/ViviendaInteractionCheck.txt",report.ToString());EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
 }
}
