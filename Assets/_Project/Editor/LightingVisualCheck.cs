using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using ProyectoVR;using ProyectoVR.UI;
[InitializeOnLoad]public static class LightingVisualCheck {
const string Flag="LightingVisualCheck";static int stage,errors,emissiveFixtures;static string dayReflectionCheck;static float next;
static LightingVisualCheck(){EditorApplication.playModeStateChanged+=State;}
[MenuItem("Vivienda/Journey/Comparar iluminacion")]public static void Begin(){SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();}
static void Log(string m,string t,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
static void State(PlayModeStateChange s){if(!SessionState.GetBool(Flag,false))return;if(s==PlayModeStateChange.EnteredPlayMode){stage=errors=0;next=2;EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;}}
static void Shot(string n){ScreenCapture.CaptureScreenshot("Captures/"+n+".png");}
static int CountEmissiveFixtures(){int count=0;foreach(var room in Object.FindObjectsByType<RoomLightGroup>().Where(r=>r.LightsOn)){var fixtures=new SerializedObject(room).FindProperty("controlledFixtures");for(int i=0;i<fixtures.arraySize;i++){var renderer=fixtures.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;if(renderer==null)continue;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);if(renderer.sharedMaterials.Any(m=>m!=null&&m.IsKeywordEnabled("_EMISSION"))&&block.GetColor("_EmissionColor").maxColorComponent>.1f)count++;}}return count;}
static void Tick(){if(!EditorApplication.isPlaying||Time.time<next)return;var menu=VrWorldMenuController.Instance;switch(stage++){
case 0:Directory.CreateDirectory("Captures");menu.SetVisible(false);next=Time.time+.5f;break;
case 1:Shot("Luz_Dia_Exterior");next=Time.time+.5f;break;
case 2:menu.TeleportPiso1();next=Time.time+1;break;
case 3:dayReflectionCheck=string.Join(", ",Object.FindObjectsByType<ReflectionProbe>().Select(p=>p.customBakedTexture!=null?p.customBakedTexture.name:"NULL"));Shot("Luz_Dia_Interior");next=Time.time+.5f;break;
case 4:foreach(var room in Object.FindObjectsByType<RoomLightGroup>())if(room.name.Contains("Sala")||room.name.Contains("Cocina"))room.SetLightsOn(true);DayNightController.Instance.StartNight();next=Time.time+3.7f;break;
case 5:emissiveFixtures=CountEmissiveFixtures();Shot("Luz_Noche_Interior");next=Time.time+.5f;break;
case 6:menu.TeleportExterior();next=Time.time+1;break;
case 7:Shot("Luz_Noche_Exterior");next=Time.time+.5f;break;
default:var nightMaps=string.Join(", ",Object.FindObjectsByType<ReflectionProbe>().Select(p=>p.customBakedTexture!=null?p.customBakedTexture.name:"NULL"));var profileReport="Errores Play="+errors+"\nReflejos dia="+dayReflectionCheck+"\nReflejos noche="+nightMaps+"\nPlafones emisivos encendidos="+emissiveFixtures+"\nCamara="+Camera.main.nearClipPlane+".."+Camera.main.farClipPlane+"m\nDia/noche exterior e interior capturados.\n";File.WriteAllText("Temp/LightingVisualCheck.txt",profileReport);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();break;}}
}
