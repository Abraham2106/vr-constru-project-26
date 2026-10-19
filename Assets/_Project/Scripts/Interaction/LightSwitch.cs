using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ProyectoVR.UI;
namespace ProyectoVR {
 [DisallowMultipleComponent]
 public class LightSwitch:MonoBehaviour {
  [SerializeField] XRSimpleInteractable interactable;
  [SerializeField] Transform switchMesh;
  [SerializeField] RoomLightGroup roomLights;
  [SerializeField] AudioSource clickAudio;
  [SerializeField] AudioClip clickClip;
  [SerializeField] float onAngle=18f;
  [SerializeField] float offAngle=-18f;
  public RoomLightGroup Room=>roomLights;
  void Awake(){if(interactable==null)interactable=GetComponent<XRSimpleInteractable>();if(interactable!=null)interactable.activated.AddListener(OnActivated);SyncVisual();}
  void OnDestroy(){if(interactable!=null)interactable.activated.RemoveListener(OnActivated);}
  void OnActivated(ActivateEventArgs args){if(VrWorldMenuController.AnyMenuVisible)return;Toggle();VrHapticsUtility.PulseInteractor(args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor,.35f,.03f);}
  public void Toggle(){if(roomLights==null)return;roomLights.Toggle();SyncVisual();if(clickAudio!=null&&clickClip!=null)clickAudio.PlayOneShot(clickClip);}
  void SyncVisual(){if(switchMesh!=null)switchMesh.localRotation=Quaternion.Euler(roomLights!=null&&roomLights.LightsOn?onAngle:offAngle,0,0);}
 }
}
