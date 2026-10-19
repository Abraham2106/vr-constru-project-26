using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.XR.CoreUtils;
using ProyectoVR;

[InitializeOnLoad]
public static class ViviendaRuntimeCheck
{
 const string CheckKey="Vivienda.RuntimeCheck";
 static readonly StringBuilder Report=new StringBuilder();
 static double started;
 static int errors;
 static Vector3 start;
 static bool moving;
 static ViviendaRuntimeCheck() { EditorApplication.playModeStateChanged+=State; }
 [MenuItem("Vivienda/Check runtime")]
 public static void Begin() {
  SessionState.SetBool(CheckKey,true); Report.Clear(); errors=0; moving=false;
  Application.logMessageReceived+=Log;
  EditorApplication.delayCall+=()=>EditorApplication.EnterPlaymode();
 }
 static void Log(string message,string trace,LogType type) {
  if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception) {errors++; if(errors<4)Report.AppendLine(message+"\n"+trace);}
 }
 static void State(PlayModeStateChange state) {
  if(!SessionState.GetBool(CheckKey,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode) {
   Report.Clear(); errors=0; Application.logMessageReceived-=Log; Application.logMessageReceived+=Log;
   started=EditorApplication.timeSinceStartup;
   var cc=UnityEngine.Object.FindFirstObjectByType<CharacterController>(); start=cc.transform.position;
   Report.AppendLine("Play entered; start="+start);
   foreach(var b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>()) if(b!=null&&(b.GetType().Name.Contains("MoveProvider")||b.GetType().Name.Contains("GravityProvider")||b.GetType().Name=="SimpleCharacterLocomotion"))Report.AppendLine(b.GetType().Name+" enabled="+b.enabled);
   EditorApplication.update+=Tick;
  }
  if(state==PlayModeStateChange.EnteredEditMode) {
   EditorApplication.update-=Tick; Application.logMessageReceived-=Log;
   Report.AppendLine("Errors during Play="+errors);
   File.WriteAllText("Temp/ViviendaRuntimeCheck.txt",Report.ToString());
   SessionState.SetBool(CheckKey,false);
  }
 }
 static void Tick() {
  if(!EditorApplication.isPlaying)return;
  var elapsed=EditorApplication.timeSinceStartup-started;
  if(Time.time>1&&!moving) {
   moving=true;
   var mover=UnityEngine.Object.FindAnyObjectByType<SimpleCharacterLocomotion>();
   var before=mover.transform.position;
   for(int i=0;i<144;i++)mover.Step(Vector2.up,1f/72f);
   Report.AppendLine("Walk at 1.4m/s for 2s: "+(mover.transform.position-before));
   for(int i=0;i<72;i++)mover.Step(Vector2.zero,1f/72f);
   var body=mover.GetComponent<CharacterController>();
   Report.AppendLine("Floor grounded="+body.isGrounded+" position="+body.transform.position);
  }
  if(!moving&&elapsed<40)return;
  if(elapsed<4)return;
  if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
  var cc=UnityEngine.Object.FindFirstObjectByType<CharacterController>();
  Report.AppendLine("End="+cc.transform.position+" grounded="+cc.isGrounded+" displacement="+(cc.transform.position-start));
  File.WriteAllText("Temp/ViviendaRuntimeCheck.txt",Report.ToString()+"\nErrors during Play="+errors);
  EditorApplication.update-=Tick;
  EditorApplication.ExitPlaymode();
 }
}
