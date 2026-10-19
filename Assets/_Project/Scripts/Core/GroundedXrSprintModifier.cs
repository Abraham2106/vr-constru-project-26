using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Sprint opcional (Left Shift) sobre la velocidad de caminata configurada en el rig.
    /// </summary>
    public class GroundedXrSprintModifier : MonoBehaviour
    {
        const float WalkSpeed = 1.4f;
        const float SprintSpeed = 2.5f;

        ContinuousMoveProvider[] m_Movers;

        void Awake()
        {
            m_Movers = GetComponentsInChildren<ContinuousMoveProvider>(true);
        }

        void Update()
        {
            if (m_Movers == null || m_Movers.Length == 0)
                return;

            var speed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed
                ? SprintSpeed
                : WalkSpeed;

            foreach (var move in m_Movers)
                move.moveSpeed = speed;
        }
    }

}
