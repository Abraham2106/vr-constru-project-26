namespace Vivienda.Tools
{
    /// <summary>Hooks estáticos para el menú XR (Agente 3). Sin dependencia de UI concreta.</summary>
    public static class ViviendaMenuHooks
    {
        public static void SelectMeasureTool() => ViviendaToolHost.Instance?.ActivateTool(ViviendaToolIds.Measure);

        public static void SelectWallPaintTool() => ViviendaToolHost.Instance?.ActivateTool(ViviendaToolIds.WallPaint);

        public static void DeselectAllTools() => ViviendaToolHost.Instance?.DeactivateAll();

        public static void MeasureUndoLast() => ViviendaToolHost.Instance?.Measure?.UndoLast();

        public static void MeasureClearAll() => ViviendaToolHost.Instance?.Measure?.ClearAll();

        public static void PaintCycleColor() => ViviendaToolHost.Instance?.WallPaint?.CycleColor(1);

        public static void PaintRestoreWalls() => ViviendaToolHost.Instance?.WallPaint?.RestoreAllPainted();
    }
}
