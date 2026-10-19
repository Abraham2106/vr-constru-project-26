using UnityEngine;
using UnityEngine.InputSystem;
namespace ProyectoVR.UI {
 public class VrMenuToggleInput:MonoBehaviour {
  [SerializeField] VrWorldMenuController m_Menu;
  [SerializeField] InputActionAsset m_MenuActions;
  [SerializeField] string m_ActionMapName="XR Menu";
  [SerializeField] string m_ToggleActionName="ToggleMenu";
  InputAction toggle;
  void OnEnable(){toggle=new InputAction("Abrir menu VR",InputActionType.Button);toggle.AddBinding("<XRController>{LeftHand}/secondaryButton");toggle.AddBinding("<XRController>{LeftHand}/menuButton");toggle.AddBinding("<Keyboard>/m");toggle.AddBinding("<Keyboard>/escape");toggle.performed+=OnToggle;toggle.Enable();}
  void OnDisable(){toggle?.Dispose();toggle=null;}
  void OnToggle(InputAction.CallbackContext context){if(m_Menu==null)m_Menu=FindAnyObjectByType<VrWorldMenuController>();m_Menu?.ToggleVisible();}
 }
}
