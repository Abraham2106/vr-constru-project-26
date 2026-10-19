using UnityEngine;

namespace Vivienda.Tools
{
    public static class UrpColorUtility
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public static Color ReadBaseColor(Material mat)
        {
            if (mat == null)
                return Color.white;
            if (mat.HasProperty(BaseColorId))
                return mat.GetColor(BaseColorId);
            if (mat.HasProperty(ColorId))
                return mat.GetColor(ColorId);
            return Color.white;
        }

        public static void ApplyBaseColor(Renderer renderer, MaterialPropertyBlock block, Color color)
        {
            if (renderer == null)
                return;

            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(ColorId))
                block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }

        public static void ClearPropertyBlock(Renderer renderer)
        {
            if (renderer != null)
                renderer.SetPropertyBlock(null);
        }
    }
}
