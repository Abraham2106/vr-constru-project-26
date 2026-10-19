using System;

namespace ProyectoVR.Core
{
    public static class WallPaletteEvents
    {
        public static event Action<int> OnPaletteIndexSelected;

        public static void SelectPaletteIndex(int index)
        {
            OnPaletteIndexSelected?.Invoke(index);
        }
    }
}
