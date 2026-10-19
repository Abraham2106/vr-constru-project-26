using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace ProyectoVR.Core
{
    /// <summary>Connects named menu destinations to XRI's native teleport queue.</summary>
    public class XrLocomotionTeleportService : MonoBehaviour
    {
        [SerializeField] TeleportationProvider m_Provider;
        [SerializeField] TeleportFadeController m_Fade;
        [SerializeField] TeleportDestinationRegistry m_Registry;

        void Awake()
        {
            if (m_Provider == null) m_Provider = GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()?.GetComponentInChildren<TeleportationProvider>(true);
            if (m_Fade == null) m_Fade = GetComponentInChildren<TeleportFadeController>(true);
            if (m_Registry == null) m_Registry = FindAnyObjectByType<TeleportDestinationRegistry>();
        }

        public void TeleportTo(TeleportDestinationId destination)
        {
            if (m_Provider == null || !m_Provider.isActiveAndEnabled || m_Registry == null || !m_Registry.TryGet(destination, out var target)) return;
            void Queue() => m_Provider.QueueTeleportRequest(new TeleportRequest
            {
                destinationPosition = target.position,
                destinationRotation = target.rotation,
                matchOrientation = MatchOrientation.TargetUpAndForward,
                requestTime = Time.time
            });
            if (m_Fade != null) m_Fade.RunFadeTeleport(Queue);
            else Queue();
        }
    }
}
