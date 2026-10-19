using System.Collections.Generic;
using UnityEngine;

namespace Vivienda.Tools
{
    public class WallPaintTool : MonoBehaviour, IViviendaTool
    {
        [SerializeField] ArchitecturalPaintPalette palette;
        [SerializeField] int selectedColorIndex;

        readonly HashSet<PaintableWall> painted = new();
        bool active;

        public string ToolId => ViviendaToolIds.WallPaint;
        public bool IsActive => active;
        public int SelectedColorIndex => selectedColorIndex;

        public ArchitecturalPaintPalette Palette
        {
            get => palette;
            set => palette = value;
        }

        public void SetActive(bool value) => active = value;

        public void SelectColorIndex(int index)
        {
            if (palette == null || palette.Count == 0)
                return;
            selectedColorIndex = ((index % palette.Count) + palette.Count) % palette.Count;
        }

        public void CycleColor(int delta = 1) => SelectColorIndex(selectedColorIndex + delta);

        public void OnTriggerDown(ToolRaycastHit hit)
        {
            if (!active || !hit.HasHit || hit.Target == null)
                return;

            var wall = hit.Target.GetComponentInParent<PaintableWall>();
            if (wall == null)
                return;

            var color = palette != null ? palette.GetColor(selectedColorIndex) : Color.cyan;
            wall.Paint(color);
            painted.Add(wall);
        }

        public void OnSecondaryDown() => CycleColor(1);

        public void RestoreAllPainted()
        {
            foreach (var wall in painted)
            {
                if (wall != null)
                    wall.RestoreOriginal();
            }
            painted.Clear();
        }
    }
}
