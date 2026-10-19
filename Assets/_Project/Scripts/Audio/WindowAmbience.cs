using UnityEngine;
namespace ProyectoVR {
 [RequireComponent(typeof(AudioSource))]
 public class WindowAmbience:MonoBehaviour {
  AudioSource source;
  void Awake(){source=GetComponent<AudioSource>();source.volume=.015f;source.loop=true;source.spatialBlend=0;if(source.clip!=null)source.Play();}
  void Update(){float open=WindowManager.OpenWindowCount;float target=Mathf.Lerp(.015f,.1f,1f-Mathf.Pow(.5f,open));source.volume=Mathf.MoveTowards(source.volume,target,Time.deltaTime*.1f);}
 }
}
