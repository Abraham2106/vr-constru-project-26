using ProyectoVR;
using UnityEngine;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Enlaza el menú XR con <see cref="DayNightController"/> cuando está en escena.
    /// </summary>
    public class DayNightEnvironmentBridge : MonoBehaviour
    {
        void OnEnable()
        {
            EnvironmentCycleEvents.OnSetNightMode += Apply;
        }

        void OnDisable()
        {
            EnvironmentCycleEvents.OnSetNightMode -= Apply;
        }

        static void Apply(bool isNight)
        {
            if (DayNightController.Instance != null)
                DayNightController.Instance.SetDayNight(isNight);
        }
    }
}
