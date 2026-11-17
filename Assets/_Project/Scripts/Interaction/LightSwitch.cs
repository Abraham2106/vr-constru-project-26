using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
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
  void Awake(){InteractionAudio.Configure(clickAudio,.4f,4f);if(interactable==null)interactable=GetComponent<XRSimpleInteractable>();if(interactable!=null)interactable.selectEntered.AddListener(OnSelected);SyncVisual();}
  void OnDestroy(){if(interactable!=null)interactable.selectEntered.RemoveListener(OnSelected);}
  void OnSelected(SelectEnterEventArgs args){if(VrWorldMenuController.AnyMenuVisible)return;Toggle();VrHapticsUtility.PulseInteractor(args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor,.35f,.03f);}
  void Update(){if(interactable==null||VrWorldMenuController.AnyMenuVisible)return;foreach(var hovered in interactable.interactorsHovering){if(hovered is XRBaseInputInteractor input && !input.hasSelection && input.activateInput.ReadWasPerformedThisFrame()){Toggle();VrHapticsUtility.PulseInteractor(input,.35f,.03f);break;}}}
  public void Toggle(){if(roomLights==null)return;roomLights.Toggle();SyncVisual();InteractionAudio.Play(clickAudio,clickClip,.4f,.04f);}
  void SyncVisual(){if(switchMesh!=null)switchMesh.localRotation=Quaternion.Euler(roomLights!=null&&roomLights.LightsOn?onAngle:offAngle,0,0);}
 }
}
