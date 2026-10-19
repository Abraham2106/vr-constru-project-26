using System;
using UnityEngine;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Puente día/noche para otros agentes. Si existe un componente con método público SetDayNight(bool),
    /// se invoca por reflexión ligera en el primer uso.
    /// </summary>
    public static class EnvironmentCycleEvents
    {
        public static event Action<bool> OnSetNightMode;

        public static void RequestNightMode(bool isNight)
        {
            OnSetNightMode?.Invoke(isNight);
            if (ProyectoVR.DayNightController.Instance != null)
                ProyectoVR.DayNightController.Instance.SetDayNight(isNight);
            else
                EnvironmentCycleBridge.TryInvokeExternal(isNight);
        }
    }

    static class EnvironmentCycleBridge
    {
        static MonoBehaviour s_External;

        public static void TryInvokeExternal(bool isNight)
        {
            if (s_External == null)
                s_External = FindDayNightBehaviour();

            if (s_External == null)
                return;

            var method = s_External.GetType().GetMethod("SetDayNight",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            method?.Invoke(s_External, new object[] { isNight });
        }

        static MonoBehaviour FindDayNightBehaviour()
        {
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null)
                    continue;
                var t = mb.GetType();
                if (t.GetMethod("SetDayNight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) != null)
                    return mb;
            }

            return null;
        }
    }
}
