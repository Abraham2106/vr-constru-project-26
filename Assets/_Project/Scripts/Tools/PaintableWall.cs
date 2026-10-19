using UnityEngine;

namespace Vivienda.Tools
{
    /// <summary>Muro cuyo color base URP puede cambiarse en runtime (solo con herramienta pintura).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class PaintableWall : MonoBehaviour
    {
        [SerializeField] Renderer targetRenderer;

        Color originalColor = Color.white;
        bool capturedOriginal;
        MaterialPropertyBlock block;

        public Renderer TargetRenderer => targetRenderer != null ? targetRenderer : targetRenderer = GetComponent<Renderer>();

        void Awake()
        {
            block = new MaterialPropertyBlock();
            CaptureOriginalIfNeeded();
        }

        void CaptureOriginalIfNeeded()
        {
            if (capturedOriginal)
                return;
            var r = TargetRenderer;
            if (r == null)
                return;

            if (r.sharedMaterial != null)
                originalColor = UrpColorUtility.ReadBaseColor(r.sharedMaterial);
            capturedOriginal = true;
        }

        public void Paint(Color color)
        {
            CaptureOriginalIfNeeded();
            UrpColorUtility.ApplyBaseColor(TargetRenderer, block, color);
        }

        public void RestoreOriginal()
        {
            CaptureOriginalIfNeeded();
            UrpColorUtility.ClearPropertyBlock(TargetRenderer);
        }

        public Color GetOriginalColor() => originalColor;
    }
}
