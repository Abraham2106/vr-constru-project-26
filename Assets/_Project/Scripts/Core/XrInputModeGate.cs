using UnityEngine;
using UnityEngine.InputSystem;
using ProyectoVR.UI;

namespace ProyectoVR.Core
{
    /// <summary>Suppresses locomotion while using menus and keeps desktop input separate from tracked XR.</summary>
    [DefaultExecutionOrder(-100)]
    public class XrInputModeGate : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] TeleportFadeController fade;
        InputActionMap locomotion;
        InputActionMap desktop;
        bool? desktopMode;

        void Awake()
        {
            locomotion = actions.FindActionMap("Locomotion", true);
            desktop = actions.FindActionMap("Desktop", true);
        }

        void Update()
        {
            if (desktopMode != DesktopPreview.IsActive)
            {
                desktopMode = DesktopPreview.IsActive;
                actions.bindingMask = desktopMode.Value ? InputBinding.MaskByGroup("Desktop")
                    : DesktopPreview.IsSimulatorActive ? InputBinding.MaskByGroups("XR", "Simulator")
                    : InputBinding.MaskByGroup("XR");
            }
            SetEnabled(locomotion, !VrWorldMenuController.AnyMenuVisible && (fade == null || !fade.IsFading));
            SetEnabled(desktop, DesktopPreview.IsActive);
        }

        void OnDisable()
        {
            locomotion?.Disable();
            desktop?.Disable();
        }

        static void SetEnabled(InputActionMap map, bool enabled)
        {
            if (map.enabled == enabled) return;
            if (enabled) map.Enable();
            else map.Disable();
        }
    }
}
