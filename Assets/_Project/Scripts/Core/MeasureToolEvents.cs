using System;

namespace ProyectoVR.Core
{
    public static class MeasureToolEvents
    {
        public static event Action<bool> OnMeasureToolToggled;

        static bool s_Enabled;

        public static bool IsEnabled => s_Enabled;

        public static void SetEnabled(bool enabled)
        {
            if (s_Enabled == enabled)
                return;
            s_Enabled = enabled;
            OnMeasureToolToggled?.Invoke(enabled);
        }

        public static void Toggle()
        {
            SetEnabled(!s_Enabled);
        }
    }
}
