using Unity.XR.CoreUtils;
using UnityEngine;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Teletransporte instantáneo del XR Origin con fade (menú).
    /// </summary>
    public class XrLocomotionTeleportService : MonoBehaviour
    {
        [SerializeField] XROrigin m_Origin;
        [SerializeField] TeleportFadeController m_Fade;
        [SerializeField] TeleportDestinationRegistry m_Registry;

        void Awake()
        {
            if (m_Origin == null)
                m_Origin = GetComponentInParent<XROrigin>();
            if (m_Fade == null)
                m_Fade = GetComponentInChildren<TeleportFadeController>(true);
            if (m_Registry == null)
                m_Registry = FindAnyObjectByType<TeleportDestinationRegistry>();
        }

        public void TeleportTo(TeleportDestinationId destination)
        {
            if (m_Registry == null || m_Origin == null)
                return;
            if (!m_Registry.TryGet(destination, out var target) || target == null)
                return;

            void Move()
            {
                var controller = m_Origin.Origin.GetComponent<CharacterController>();
                bool wasEnabled = controller != null && controller.enabled;
                if (wasEnabled) controller.enabled = false;
                try
                {
                    m_Origin.transform.rotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
                    var cam = m_Origin.Camera;
                    var offset = cam != null ? m_Origin.transform.position - cam.transform.position : Vector3.zero;
                    offset.y = 0f;
                    m_Origin.transform.position = target.position + offset;
                    Physics.SyncTransforms();
                }
                finally
                {
                    if (wasEnabled) controller.enabled = true;
                }
            }

            if (m_Fade != null)
                m_Fade.RunFadeTeleport(Move);
            else
                Move();
        }
    }
}
