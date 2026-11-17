using UnityEngine;
namespace ProyectoVR {
 /// <summary>
 /// Ambiente exterior muy leve (viento y pasto). Se escucha solo afuera de la casa y se desvanece al entrar.
 /// Adentro, el viento solo entra por las ventanas o puertas abiertas (ver ExteriorOpening).
 /// </summary>
 [RequireComponent(typeof(AudioSource))]
 public class WindowAmbience:MonoBehaviour {
  [SerializeField] AudioClip windLoop;
  [Range(0f,1f)][SerializeField] float outdoorVolume=.06f;
  [SerializeField] float fadeSeconds=1.5f;
  AudioSource source;
  Transform listener;
  void Awake(){
   source=GetComponent<AudioSource>();
   if(windLoop!=null)source.clip=windLoop;
   source.loop=true;source.playOnAwake=false;source.spatialBlend=0f;source.volume=0f;
   ProyectoVrAudioBus.Instance?.RouteToAmbiente(source);
  }
  void Update(){
   if(source.clip==null)return;
   if(listener==null){var cam=Camera.main;if(cam==null)return;listener=cam.transform;}
   float target=IndoorDetector.IsIndoors(listener.position)?0f:outdoorVolume;
   source.volume=Mathf.MoveTowards(source.volume,target,outdoorVolume/Mathf.Max(.1f,fadeSeconds)*Time.deltaTime);
   if(source.volume>0f&&!source.isPlaying)source.Play();
   else if(source.volume<=0f&&target<=0f&&source.isPlaying)source.Stop();
  }
 }

 /// <summary>Adentro = hay techo o losa de la casa encima de la cabeza.</summary>
 public static class IndoorDetector {
  static Transform house;
  static int frame=-1;static Vector3 lastPos;static bool lastResult;
  public static bool IsIndoors(Vector3 head){
   if(frame==Time.frameCount&&(head-lastPos).sqrMagnitude<.0001f)return lastResult;
   if(house==null){var go=GameObject.Find("Vivienda");house=go!=null?go.transform:null;}
   bool inside=false;
   var hits=Physics.RaycastAll(head+Vector3.up*.2f,Vector3.up,8f,Core.WalkableLayerMask.Value,QueryTriggerInteraction.Ignore);
   foreach(var h in hits)if(house==null||h.collider.transform.IsChildOf(house)){inside=true;break;}
   frame=Time.frameCount;lastPos=head;lastResult=inside;return inside;
  }
 }
}
