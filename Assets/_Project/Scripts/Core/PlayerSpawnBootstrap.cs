using Unity.XR.CoreUtils;
using UnityEngine;

namespace ProyectoVR.Core
{
    public class PlayerSpawnBootstrap : MonoBehaviour
    {
        [SerializeField] XROrigin m_Origin;
        [SerializeField] Transform m_PlayerSpawn;
        [SerializeField] float m_RaycastHeight = .6f;
        [SerializeField] float m_RaycastDistance = 2f;

        public static Vector3 LastSpawnWorldPosition { get; private set; }

        void Start()
        {
            if (m_Origin == null) m_Origin = GetComponent<XROrigin>();
            if (m_PlayerSpawn == null)
            {
                var spawn = GameObject.Find("PlayerSpawn");
                if (spawn != null) m_PlayerSpawn = spawn.transform;
            }
            if (m_Origin == null || m_PlayerSpawn == null) return;

            // Legacy serialized heights were above the roof. Only search the intended floor.
            var feet = ResolveGroundPosition(m_PlayerSpawn.position, Mathf.Clamp(m_RaycastHeight, .05f, .6f), Mathf.Clamp(m_RaycastDistance, .2f, 2f));
            LastSpawnWorldPosition = feet;
            var body = m_Origin.Origin != null ? m_Origin.Origin : m_Origin.gameObject;
            var controller = body.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled) controller.enabled = false;
            m_Origin.transform.rotation = Quaternion.Euler(0, m_PlayerSpawn.eulerAngles.y, 0);
            var camera = m_Origin.Camera;
            var headOffset = camera != null ? m_Origin.transform.position - camera.transform.position : Vector3.zero;
            headOffset.y = 0;
            m_Origin.transform.position = feet + headOffset;
            Physics.SyncTransforms();
            if (wasEnabled) controller.enabled = true;
        }

        static Vector3 ResolveGroundPosition(Vector3 hint, float height, float distance)
        {
            float closest = float.PositiveInfinity;
            Vector3 feet = hint;
            foreach (var hit in Physics.RaycastAll(hint + Vector3.up * height, Vector3.down, distance, WalkableLayerMask.Value, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y < .7f || Mathf.Abs(hit.point.y - hint.y) > .45f || hit.distance >= closest) continue;
                feet = new Vector3(hint.x, hit.point.y, hint.z);
                closest = hit.distance;
            }
            return feet;
        }
    }
}
