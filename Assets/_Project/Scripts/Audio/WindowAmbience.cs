using UnityEngine;
namespace ProyectoVR {
 /// <summary>
 /// Ambiente exterior: un fondo muy bajo siempre, que sube solo cerca de la ventana abierta mas cercana.
 /// No se suma por ventana: dejar varias abiertas no lo hace mas fuerte, y al alejarse vuelve al fondo.
 /// </summary>
 [RequireComponent(typeof(AudioSource))]
 public class WindowAmbience:MonoBehaviour {
  [SerializeField] float baseVolume=.012f;
  [SerializeField] float nearWindowVolume=.07f;
  [Tooltip("Distancia a la que se oye el volumen maximo.")]
  [SerializeField] float fullDistance=1.5f;
  [Tooltip("Distancia a la que la ventana ya no aporta nada.")]
  [SerializeField] float silentDistance=6f;
  [SerializeField] float fadeSpeed=.08f;
  AudioSource source;
  Transform listener;
  void Awake(){source=GetComponent<AudioSource>();source.volume=baseVolume;source.loop=true;source.spatialBlend=0;ProyectoVrAudioBus.Instance?.RouteToAmbiente(source);if(source.clip!=null)source.Play();}
  void Update(){
   if(listener==null){var cam=Camera.main;listener=cam!=null?cam.transform:transform;}
   float d=WindowManager.NearestOpenDistance(listener.position);
   float near=1f-Mathf.InverseLerp(fullDistance,silentDistance,d);
   float target=Mathf.Lerp(baseVolume,nearWindowVolume,near*near);
   source.volume=Mathf.MoveTowards(source.volume,target,Time.deltaTime*fadeSpeed);
  }
 }
}
