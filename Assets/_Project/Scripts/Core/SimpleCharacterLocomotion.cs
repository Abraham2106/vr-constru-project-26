using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProyectoVR
{
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public class SimpleCharacterLocomotion : MonoBehaviour
    {
        [SerializeField] float m_WalkSpeed = 1.4f;
        [SerializeField] float m_SprintSpeed = 2.5f;
        [SerializeField] Transform m_ViewDirection;
        CharacterController m_Controller;
        float m_VerticalVelocity;
        public float WalkSpeed { get => m_WalkSpeed; set => m_WalkSpeed = value; }

        void Awake()
        {
            m_Controller = GetComponent<CharacterController>();
            if (m_ViewDirection == null)
                m_ViewDirection = GetComponent<XROrigin>()?.Camera?.transform;
        }

        void Update()
        {
            var input = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            }
            var hand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (hand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out var stick) && stick.sqrMagnitude > input.sqrMagnitude)
                input = stick;
            Step(input, Time.deltaTime, keyboard != null && keyboard.leftShiftKey.isPressed);
        }

        public void Step(Vector2 input, float deltaTime, bool sprint = false)
        {
            if (m_Controller == null) m_Controller = GetComponent<CharacterController>();
            if (!m_Controller.enabled || !float.IsFinite(deltaTime) || deltaTime <= 0f) return;
            if (!float.IsFinite(input.x) || !float.IsFinite(input.y)) input = Vector2.zero;
            input = Vector2.ClampMagnitude(input, 1f);
            var view = m_ViewDirection != null ? m_ViewDirection : transform;
            var forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.000001f)
                forward = Vector3.Cross(view.right, Vector3.up);
            if (forward.sqrMagnitude < 0.000001f) forward = transform.forward;
            forward /= Mathf.Sqrt(forward.sqrMagnitude);
            var right = new Vector3(forward.z, 0f, -forward.x);
            if (m_Controller.isGrounded && m_VerticalVelocity < 0f) m_VerticalVelocity = -2f;
            m_VerticalVelocity = Mathf.Max(m_VerticalVelocity + Physics.gravity.y * deltaTime, -30f);
            var velocity = (forward * input.y + right * input.x) * (sprint ? m_SprintSpeed : m_WalkSpeed);
            velocity.y = m_VerticalVelocity;
            var flags = m_Controller.Move(velocity * deltaTime);
            if ((flags & CollisionFlags.Below) != 0) m_VerticalVelocity = -2f;
            if ((flags & CollisionFlags.Above) != 0 && m_VerticalVelocity > 0f) m_VerticalVelocity = 0f;
        }
    }
}
