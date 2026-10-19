using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using ProyectoVR.UI;

namespace ProyectoVR
{
    [RequireComponent(typeof(XROrigin))]
    [DefaultExecutionOrder(-50)]
    public class DesktopPreview : MonoBehaviour
    {
        public static bool IsActive { get; private set; }
        XROrigin origin;
        float pitch;

        void Start()
        {
            origin = GetComponent<XROrigin>();
#if UNITY_EDITOR
            var head = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.Head);
            var name = (head.name ?? "").ToLowerInvariant();
            bool simulated = name.Contains("simulat") || name.Contains("mock");
            bool realHeadset = head.isValid && !simulated && (head.characteristics & InputDeviceCharacteristics.HeadMounted) != 0;
            IsActive = !realHeadset && (simulated || !XRSettings.isDeviceActive);
            if (IsActive && origin.Camera != null)
            {
                foreach (var simulator in FindObjectsByType<XRDeviceSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    simulator.gameObject.SetActive(false);
                var driver = origin.Camera.GetComponent<TrackedPoseDriver>();
                if (driver != null) driver.enabled = false;
                var cameraTransform = origin.Camera.transform;
                if (cameraTransform.parent != origin.transform)
                    cameraTransform.parent.localPosition = new Vector3(0, 1.65f, 0);
                cameraTransform.localPosition = cameraTransform.parent == origin.transform ? new Vector3(0, 1.65f, 0) : Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
            }
#else
            IsActive = false;
#endif
        }

        void Update()
        {
            if (!IsActive || origin == null || origin.Camera == null || Mouse.current == null) return;
            bool look = Mouse.current.rightButton.isPressed && !VrWorldMenuController.AnyMenuVisible;
            Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !look;
            if (!look) return;
            var delta = Mouse.current.delta.ReadValue() * .08f;
            transform.Rotate(0, delta.x, 0, UnityEngine.Space.World);
            pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
            origin.Camera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            IsActive = false;
        }
    }
}

