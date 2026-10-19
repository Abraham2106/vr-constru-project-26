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
        public enum PreviewMode { Desktop, XRSimulator, Headset }
        [SerializeField] PreviewMode mode;
        [SerializeField] InputActionReference lookAction;
        [SerializeField] InputActionReference lookHoldAction;
        XROrigin origin;
        float pitch;
        float yaw;

        void Start()
        {
            origin = GetComponent<XROrigin>();
#if UNITY_EDITOR || UNITY_STANDALONE
            IsActive = mode == PreviewMode.Desktop;
            if (IsActive && origin.Camera != null)
            {
                foreach (var simulator in FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Include))
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
            if (!IsActive || origin == null || origin.Camera == null || lookAction == null || lookHoldAction == null) return;
            bool look = lookHoldAction.action.IsPressed() && !VrWorldMenuController.AnyMenuVisible;
            Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !look;
            if (!look) return;
            var delta = lookAction.action.ReadValue<Vector2>() * .08f;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
            origin.Camera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            IsActive = false;
        }
    }
}
