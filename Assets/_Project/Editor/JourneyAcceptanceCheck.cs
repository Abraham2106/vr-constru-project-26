using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ProyectoVR;
using ProyectoVR.UI;
using Vivienda.Tools;

[InitializeOnLoad]
public static class JourneyAcceptanceCheck {
 const string Flag="Journey.Acceptance";
 static StringBuilder report=new();static int phase,errors,failed;static float next;
 static DoorHinge[] doors;static WindowHinge[] windows;static XRInteractionManager manager;static XRBaseInteractor hand;static bool captured;
 static VrWorldMenuController menu;static ViviendaToolHost host;static Vector3[] frames;static Transform[] frameObjects;static float ambientBefore;
 static JourneyAcceptanceCheck(){EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Vivienda/Journey/Comprobar experiencia")]
 public static void Begin(){SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
 static void Check(bool ok,string message){report.AppendLine((ok?"PASS ":"FAIL ")+message);if(!ok)failed++;}
 static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;if(errors<4)report.AppendLine(message+"\n"+trace);}}
 static void State(PlayModeStateChange state){if(!SessionState.GetBool(Flag,false))return;if(state==PlayModeStateChange.EnteredPlayMode){report.Clear();errors=failed=phase=0;captured=false;next=2;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SessionState.SetBool(Flag,false);}}
 static void Select(Component c){var target=c.GetComponent<XRSimpleInteractable>();manager.SelectEnter((IXRSelectInteractor)hand,(IXRSelectInteractable)target);manager.SelectExit((IXRSelectInteractor)hand,(IXRSelectInteractable)target);}
 static void Click(string name){menu.SetVisible(true);var b=menu.GetComponentsInChildren<Button>().First(x=>x.name==name);ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);}
 static bool Mapped(Component c){var s=c.GetComponent<XRSimpleInteractable>();return s!=null&&s.colliders.Any(x=>x!=null&&x.enabled&&manager.TryGetInteractableForCollider(x,out var t)&&(t as UnityEngine.Object)==s);}
 static void Tick(){if(!EditorApplication.isPlaying||Time.time<next)return;try{
 if(phase==0){
  menu=UnityEngine.Object.FindAnyObjectByType<VrWorldMenuController>();host=ViviendaToolHost.Instance;
  doors=UnityEngine.Object.FindObjectsByType<DoorHinge>().Where(d=>d.enabled).ToArray();windows=UnityEngine.Object.FindObjectsByType<WindowHinge>().Where(w=>w.enabled).ToArray();manager=UnityEngine.Object.FindAnyObjectByType<XRInteractionManager>();hand=UnityEngine.Object.FindAnyObjectByType<NearFarInteractor>();if(hand==null){hand=new GameObject("Journey test ray").AddComponent<XRRayInteractor>();hand.transform.position=Vector3.one*100;}
  Check(DesktopPreview.IsActive,"Simulador escritorio activo");Check(menu.IsVisible,"Menu visible al iniciar");Check(menu.ColorButtonCount==12,"Paleta con 12 colores");
  var cam=Camera.main;var canvas=menu.GetComponentsInChildren<Canvas>().First(c=>c.name=="Menu Journey");var screen=RectTransformUtility.WorldToScreenPoint(cam,canvas.transform.position);var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);Check(hits.Any(h=>h.gameObject.GetComponentInParent<Canvas>()==canvas),"Menu recibe raycast UI del raton");
  if(!captured){Directory.CreateDirectory("Captures");ScreenCapture.CaptureScreenshot("Captures/JourneyMenu.png");captured=true;next=Time.time+.5f;return;}
  Check(doors.Length==11&&doors.All(Mapped),"11 puertas registradas en XR");Check(windows.Length>2&&windows.All(Mapped),"Ventanas reales registradas XR: "+windows.Length);
  frameObjects=UnityEngine.Object.FindObjectsByType<Transform>().Where(t=>t.name.StartsWith("Marco")&&t.GetComponent<Renderer>()!=null).ToArray();frames=frameObjects.Select(t=>t.position).ToArray();
  Click("Medir");Check(!menu.IsVisible&&host.Measure.IsActive,"Boton medir activa herramienta y cierra menu");
  host.Measure.OnTriggerDown(new ToolRaycastHit(true,Vector3.zero,Vector3.up,null));host.Measure.OnTriggerDown(new ToolRaycastHit(true,Vector3.right,Vector3.up,null));var segment=UnityEngine.Object.FindAnyObjectByType<MeasureSegment>();Check(segment!=null&&Mathf.Abs(segment.DistanceMeters-1)<.001f&&segment.transform.parent.parent==null,"Medida de 1m anclada al mundo");host.Measure.ClearAll();
  menu.SetVisible(true);menu.OpenWallPalette();var color=menu.GetComponentsInChildren<Button>().First(b=>b.name=="Color_3");ExecuteEvents.Execute(color.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);Check(!menu.IsVisible&&host.WallPaint.IsActive&&host.WallPaint.SelectedColorIndex==3,"Paleta selecciona color y activa pintura");var wall=UnityEngine.Object.FindAnyObjectByType<PaintableWall>();host.WallPaint.OnTriggerDown(new ToolRaycastHit(true,wall.transform.position,Vector3.up,wall.transform));host.WallPaint.RestoreAllPainted();host.DeactivateAll();
  var switches=UnityEngine.Object.FindObjectsByType<LightSwitch>();Check(switches.Length==12,"12 interruptores por habitacion");bool independent=true;foreach(var s in switches){s.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());independent&=s.Room.LightsOn&&switches.Count(x=>x.Room.LightsOn)==1;s.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());independent&=!s.Room.LightsOn;}Check(independent,"Gatillo en interruptor solo cambia su habitacion");
  var ambience=UnityEngine.Object.FindAnyObjectByType<WindowAmbience>();ambientBefore=ambience.GetComponent<AudioSource>().volume;
  foreach(var d in doors)Select(d);foreach(var w in windows)Select(w);next=Time.time+1.6f;phase++;return;
 }
 if(phase==1){Check(doors.All(d=>Mathf.Abs(d.CurrentAngle)>80),"Grip a distancia abre todas las puertas");Check(windows.All(w=>w.IsOpen),"Grip a distancia abre todas las ventanas");Check(frameObjects.Select((f,i)=>Vector3.Distance(f.position,frames[i])<.001f).All(x=>x),"Marcos permanecen fijos");Check(UnityEngine.Object.FindAnyObjectByType<WindowAmbience>().GetComponent<AudioSource>().volume>ambientBefore+.01f,"Ventanas aumentan ambiente exterior");foreach(var d in doors)Select(d);foreach(var w in windows)Select(w);Click("Noche");next=Time.time+3.7f;phase++;return;}
 if(phase==2){Check(doors.All(d=>Mathf.Abs(d.CurrentAngle)<.1f),"Grip cierra puertas");Check(windows.All(w=>!w.IsOpen),"Grip cierra ventanas");Check(UnityEngine.Object.FindObjectsByType<Light>().Count(l=>l.enabled)==3,"Noche: sol y dos luces exteriores; habitaciones apagadas");Click("Dia");Click("Piso2");next=Time.time+3.7f;phase++;return;}
 if(phase==3){Check(Camera.main.transform.position.y>4,"Teletransporte al segundo piso");Click("Piso1");next=Time.time+.8f;phase++;return;}
 if(phase==4){Check(Camera.main.transform.position.y>1&&Camera.main.transform.position.y<2.5f,"Teletransporte al primer piso");Click("Exterior");next=Time.time+.8f;phase++;return;}
 Check(Camera.main.transform.position.z>8,"Teletransporte al exterior frente entrada");Check(errors==0,"Errores durante Play: "+errors);menu.SetVisible(true);
 }catch(Exception ex){Check(false,ex.ToString());}
 report.AppendLine("Fallos="+failed);File.WriteAllText("Temp/JourneyAcceptance.txt",report.ToString());EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}

