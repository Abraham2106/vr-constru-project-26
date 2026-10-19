using ProyectoVR.Core;
using UnityEngine;

namespace ProyectoVR.UI
{
    /// <summary>
    /// Mantiene el menú ~1 m frente al usuario cuando está visible.
    /// </summary>
    public class VrMenuFollowHead : MonoBehaviour
    {
        [SerializeField] Transform m_Head;
        [SerializeField] float m_Distance = 1f;
        [SerializeField] float m_HeightOffset = -0.05f;
        [SerializeField] VrWorldMenuController m_Menu;

        void LateUpdate()
        {
            if (m_Head == null || m_Menu == null || !m_Menu.IsVisible)
                return;

            if (!DirectionUtil.TryGetHorizontalUnit(m_Head.forward, out var forward)
                && !DirectionUtil.TryGetUnitVector(m_Head.forward, out forward))
                return;

            var targetPos = m_Head.position + forward * m_Distance;
            targetPos.y = m_Head.position.y + m_HeightOffset;
            transform.position = targetPos;
            if (!DirectionUtil.TryLookRotation(forward, Vector3.up, out var look))
                return;
            transform.rotation = look;
        }
    }
}
