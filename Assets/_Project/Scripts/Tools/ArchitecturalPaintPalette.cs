using UnityEngine;

namespace Vivienda.Tools
{
    [CreateAssetMenu(fileName = "ArchitecturalPaintPalette", menuName = "Vivienda/Paleta pintura arquitectónica")]
    public class ArchitecturalPaintPalette : ScriptableObject
    {
        [SerializeField] Color[] colors =
        {
            new(0.96f, 0.94f, 0.90f), // Blanco roto
            new(0.88f, 0.87f, 0.85f), // Gris perla
            new(0.82f, 0.76f, 0.66f), // Beige arena
            new(0.78f, 0.55f, 0.44f), // Terracota suave
            new(0.62f, 0.70f, 0.58f), // Verde salvia
            new(0.55f, 0.65f, 0.72f), // Azul polvo
            new(0.28f, 0.30f, 0.32f), // Antracita
            new(0.93f, 0.89f, 0.78f), // Crema vainilla
            new(0.85f, 0.72f, 0.72f), // Rosa empolvado
            new(0.72f, 0.58f, 0.38f), // Ocre dorado
            new(0.45f, 0.48f, 0.52f), // Gris urbano
            new(0.98f, 0.98f, 0.97f), // Blanco nieve
        };

        public int Count => colors != null ? colors.Length : 0;

        public Color GetColor(int index)
        {
            if (colors == null || colors.Length == 0)
                return Color.white;
            index = ((index % colors.Length) + colors.Length) % colors.Length;
            return colors[index];
        }

        public string GetDisplayName(int index)
        {
            string[] names =
            {
                "Blanco roto", "Gris perla", "Beige arena", "Terracota", "Verde salvia", "Azul polvo",
                "Antracita", "Crema vainilla", "Rosa empolvado", "Ocre dorado", "Gris urbano", "Blanco nieve"
            };
            index = ((index % names.Length) + names.Length) % names.Length;
            return names[index];
        }
    }
}
