using System;

namespace ProyectoVR.Core
{
    /// <summary>Single request channel consumed by the scene's lighting controller.</summary>
    public static class EnvironmentCycleEvents
    {
        public static event Action<bool> OnSetNightMode;
        public static void RequestNightMode(bool isNight) => OnSetNightMode?.Invoke(isNight);
    }
}
