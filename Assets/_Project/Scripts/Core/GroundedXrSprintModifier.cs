using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Velocidades para caminar, acelerar y acercarse con precisión en escritorio.
    /// </summary>
    public class GroundedXrSprintModifier : MonoBehaviour
    {
        const float WalkSpeed = 2.0f;
        const float SprintSpeed = 3.0f;
        const float PrecisionSpeed = 0.65f;
        const float StickFullSpeed = 2.4f;
        const float StickMinFactor = 0.3f;

        ContinuousMoveProvider[] m_Movers;
        public bool PrecisionMode { get; set; }

        void Awake()
        {
            m_Movers = GetComponentsInChildren<ContinuousMoveProvider>(true);
        }

        void Update()
        {
            if (m_Movers == null || m_Movers.Length == 0)
                return;

            var keyboard = Keyboard.current;
            bool desktop = DesktopPreview.IsActive;
            var speed = PrecisionMode || desktop && keyboard != null && keyboard.leftCtrlKey.isPressed
                ? PrecisionSpeed
                : desktop && keyboard != null && keyboard.leftShiftKey.isPressed ? SprintSpeed
                : desktop ? WalkSpeed : StickFullSpeed;

            foreach (var move in m_Movers)
            {
                // Con joystick: poca inclinacion = paso lento y preciso; a fondo = recorrido rapido.
                // El proveedor ya multiplica por la inclinacion; este factor lo vuelve progresivo.
                float curve = 1f;
                if (!desktop && !PrecisionMode)
                {
                    float tilt = Mathf.Max(move.leftHandMoveInput.ReadValue().magnitude, move.rightHandMoveInput.ReadValue().magnitude);
                    curve = Mathf.Lerp(StickMinFactor, 1f, Mathf.Clamp01(tilt));
                }
                move.moveSpeed = speed * curve;
            }
        }
    }

}
