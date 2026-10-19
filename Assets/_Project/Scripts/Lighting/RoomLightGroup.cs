using System.Collections.Generic;
using UnityEngine;
namespace ProyectoVR {
 [DisallowMultipleComponent]
 public class RoomLightGroup:MonoBehaviour {
  [SerializeField] string roomId="Sala";
  [SerializeField] List<Light> controlledLights=new();
  [SerializeField] List<Renderer> controlledFixtures=new();
  [SerializeField] bool lightsOn;
  public string RoomId=>roomId;
  public bool LightsOn=>lightsOn;
  public void SetLightsOn(bool on){lightsOn=on;Apply();}
  public void Toggle()=>SetLightsOn(!lightsOn);
  void Awake()=>Apply();
  void Apply(){foreach(var light in controlledLights)if(light!=null)light.enabled=lightsOn;foreach(var r in controlledFixtures)if(r!=null){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetColor("_EmissionColor",lightsOn?new Color(1,.82f,.6f)*.8f:Color.black);r.SetPropertyBlock(block);}}
  public void RegisterLight(Light light){if(light!=null&&!controlledLights.Contains(light))controlledLights.Add(light);Apply();}
 }
}
