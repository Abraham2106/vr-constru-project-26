using System.Collections.Generic;
using UnityEngine;

namespace ProyectoVR.Core
{
    public enum TeleportDestinationId
    {
        Exterior,
        Piso1,
        Piso2,
    }

    /// <summary>
    /// Resuelve TP_Exterior, TP_Piso1, TP_Piso2 por nombre en escena.
    /// </summary>
    public class TeleportDestinationRegistry : MonoBehaviour
    {
        [SerializeField] Transform m_Exterior;
        [SerializeField] Transform m_Piso1;
        [SerializeField] Transform m_Piso2;

        readonly Dictionary<TeleportDestinationId, Transform> m_Map = new();

        void Awake()
        {
            m_Map.Clear();
            Register(TeleportDestinationId.Exterior, m_Exterior, "TP_Exterior");
            Register(TeleportDestinationId.Piso1, m_Piso1, "TP_Piso1");
            Register(TeleportDestinationId.Piso2, m_Piso2, "TP_Piso2");
        }

        void Register(TeleportDestinationId id, Transform assigned, string fallbackName)
        {
            if (assigned == null)
            {
                var go = GameObject.Find(fallbackName);
                assigned = go != null ? go.transform : null;
            }

            if (assigned != null)
                m_Map[id] = assigned;
        }

        public bool TryGet(TeleportDestinationId id, out Transform target)
        {
            return m_Map.TryGetValue(id, out target);
        }
    }
}
