using System.Collections.Generic;
using UnityEngine;

namespace ProyectoVR
{
    /// <summary>
    /// Sube el ambiente exterior en el AudioMixer cuando hay ventanas abiertas.
    /// </summary>
    public static class WindowManager
    {
        static readonly HashSet<WindowHinge> Windows = new();

        public static int OpenWindowCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Windows.Clear();OpenWindowCount=0;}

        public static void RegisterWindow(WindowHinge window)
        {
            if (window != null)
                Windows.Add(window);
            Recalculate();
        }

        public static void UnregisterWindow(WindowHinge window)
        {
            if (window != null)
                Windows.Remove(window);
            Recalculate();
        }

        public static void NotifyWindowStateChanged() => Recalculate();

        /// <summary>Distancia a la ventana abierta mas cercana (infinito si no hay ninguna abierta).</summary>
        public static float NearestOpenDistance(Vector3 position)
        {
            float best = float.PositiveInfinity;
            foreach (var w in Windows)
                if (w != null && w.isActiveAndEnabled && w.IsOpen)
                    best = Mathf.Min(best, Vector3.Distance(position, w.transform.position));
            return best;
        }

        static void Recalculate()
        {
            var open = 0;
            Windows.RemoveWhere(w=>w==null);
            foreach (var w in Windows)
            {
                if (w.isActiveAndEnabled && w.IsOpen)
                    open++;
            }

            OpenWindowCount = open;
            var bus = ProyectoVrAudioBus.Instance;
            if (bus == null)
                return;

            var normalized = open == 0 ? 0f : 1f - Mathf.Pow(0.5f, open);
            bus.SetExteriorAmbience(normalized);
        }
    }
}
