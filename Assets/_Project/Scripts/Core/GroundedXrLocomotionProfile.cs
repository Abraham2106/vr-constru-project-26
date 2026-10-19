using Unity.XR.CoreUtils;
using UnityEngine;

namespace ProyectoVR.Core
{
    /// <summary>Configures the physical capsule; XRI owns movement, gravity and teleportation.</summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(XROrigin), typeof(CharacterController))]
    public class GroundedXrLocomotionProfile : MonoBehaviour
    {
        [SerializeField] XROrigin m_Origin;

        void Awake()
        {
            if (m_Origin == null) m_Origin = GetComponent<XROrigin>();
            var controller = m_Origin.Origin.GetComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = .3f;
            controller.skinWidth = .02f;
            controller.stepOffset = .4f;
            controller.slopeLimit = 45;
            controller.center = Vector3.up * .85f;
            controller.minMoveDistance = 0;
            m_Origin.CameraYOffset = 1.65f;
        }
    }
}
