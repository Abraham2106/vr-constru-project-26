using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace ProyectoVR
{
    [DisallowMultipleComponent]
    public class DesktopDoorInteraction : MonoBehaviour
    {
        [SerializeField] InputActionReference interactAction;
        Camera view;
        DoorHinge door;
        WindowHinge window;
        LightSwitch lightSwitch;

        void Awake() { view = GetComponent<XROrigin>()?.Camera; }

        void Update()
        {
            door = null; window = null; lightSwitch = null;
            if(ProyectoVR.UI.VrWorldMenuController.AnyMenuVisible)return;
            if (!DesktopPreview.IsActive || view == null || interactAction == null) return;
            if (!Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)) return;
            door = hit.collider.GetComponentInParent<DoorHinge>();
            window = hit.collider.GetComponentInParent<WindowHinge>();
            lightSwitch=hit.collider.GetComponentInParent<LightSwitch>();
            if (!interactAction.action.WasPressedThisFrame()) return;
            if (door != null) door.Toggle();
            else if (window != null) window.Toggle();
            else if(lightSwitch!=null)lightSwitch.Toggle();
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (door == null && window == null && lightSwitch==null) return;
            GUI.Box(new Rect(Screen.width / 2f - 165f, Screen.height - 76f, 330f, 30f),
                door != null ? "E : Abrir / cerrar puerta" : window!=null?"E : Abrir / cerrar ventana":"E : Encender / apagar luz");
        }
#endif
    }
}
