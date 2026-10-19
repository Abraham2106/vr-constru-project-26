using ProyectoVR.Core;
using UnityEngine;

namespace Vivienda.Tools
{
    /// <summary>Enlaza eventos del menú Agente 3 con ViviendaToolHost.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ViviendaToolHost))]
    public class ViviendaToolEventBridge : MonoBehaviour
    {
        ViviendaToolHost host;

        void Awake()
        {
            host = GetComponent<ViviendaToolHost>();
        }

        void OnEnable()
        {
            MeasureToolEvents.OnMeasureToolToggled += OnMeasureToggled;
            WallPaletteEvents.OnPaletteIndexSelected += OnPaletteSelected;
        }

        void OnDisable()
        {
            MeasureToolEvents.OnMeasureToolToggled -= OnMeasureToggled;
            WallPaletteEvents.OnPaletteIndexSelected -= OnPaletteSelected;
        }

        void OnMeasureToggled(bool enabled)
        {
            if (host == null)
                return;
            if (enabled)
                host.ActivateTool(ViviendaToolIds.Measure);
            else if (host.ActiveToolId == ViviendaToolIds.Measure)
                host.DeactivateAll();
        }

        void OnPaletteSelected(int index)
        {
            if (host == null)
                return;
            host.ActivateTool(ViviendaToolIds.WallPaint);
            host.WallPaint?.SelectColorIndex(index);
        }
    }
}
