using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace ProyectoVR.Core
{
    /// <summary>
    /// CharacterController + locomoción simple; proveedores XRI de movimiento/gravedad desactivados.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GroundedXrLocomotionProfile : MonoBehaviour
    {
        const float BodyHeight = 1.7f;
        const float BodyRadius = 0.3f;
        const float SkinWidth = 0.02f;
        const float StepOffset = 0.4f;
        const float SlopeLimit = 45f;
        const float EyeHeight = 1.65f;

        [SerializeField] XROrigin m_Origin;

        void Awake()
        {
            if (m_Origin == null)
                m_Origin = GetComponent<XROrigin>();

            ApplyCharacterController();
            DisableXriMoveAndGravity();
            EnsureSimpleLocomotion();
            DisableSimulatorFreeFlight();
        }

        void Start()
        {
            DisableXriMoveAndGravity();
            EnsureSimpleLocomotion();
            DisableSimulatorFreeFlight();
        }

        void LateUpdate()
        {
            DisableXriMoveAndGravity();
        }

        void DisableXriMoveAndGravity()
        {
            foreach (var move in GetComponentsInChildren<ContinuousMoveProvider>(true))
                move.enabled = false;

            foreach (var move in GetComponentsInChildren<DynamicMoveProvider>(true))
                move.enabled = false;

            foreach (var gravity in GetComponentsInChildren<GravityProvider>(true))
                gravity.enabled = false;

            foreach (var grounded in GetComponentsInChildren<GroundedDynamicMoveProvider>(true))
            {
                grounded.enabled = false;
                if (Application.isPlaying)
                    Destroy(grounded);
                else
                    DestroyImmediate(grounded);
            }
        }

        void EnsureSimpleLocomotion()
        {
            if (m_Origin?.Origin == null)
                return;

            var body = m_Origin.Origin.gameObject;
            if (body.GetComponent<SimpleCharacterLocomotion>() == null)
                body.AddComponent<SimpleCharacterLocomotion>();
        }

        void ApplyCharacterController()
        {
            if (m_Origin?.Origin == null)
                return;

            var cc = m_Origin.Origin.GetComponent<CharacterController>();
            if (cc == null)
                cc = m_Origin.Origin.gameObject.AddComponent<CharacterController>();

            cc.height = BodyHeight;
            cc.radius = BodyRadius;
            cc.skinWidth = SkinWidth;
            cc.stepOffset = StepOffset;
            cc.slopeLimit = SlopeLimit;
            cc.center = new Vector3(0f, BodyHeight * 0.5f, 0f);
            cc.minMoveDistance = 0.001f;

            m_Origin.CameraYOffset = EyeHeight;
        }

        void DisableSimulatorFreeFlight()
        {
            var simulators = FindObjectsByType<XRDeviceSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sim in simulators)
            {
                sim.keyboardXTranslateSpeed = 0f;
                sim.keyboardYTranslateSpeed = 0f;
                sim.keyboardZTranslateSpeed = 0f;
                sim.keyboardBodyTranslateMultiplier = 0f;
                sim.mouseTransformationMode = XRDeviceSimulator.TransformationMode.Rotate;
            }
        }
    }
}
