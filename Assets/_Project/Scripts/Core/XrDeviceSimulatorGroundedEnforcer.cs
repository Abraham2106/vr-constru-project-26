using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Mantiene el XR Device Simulator en modo mirada (sin vuelo WASD/Q/E en el espacio).
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class XrDeviceSimulatorGroundedEnforcer : MonoBehaviour
    {
        void Awake()
        {
            Apply();
        }

        void Update()
        {
            Apply();
        }

        void Apply()
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
