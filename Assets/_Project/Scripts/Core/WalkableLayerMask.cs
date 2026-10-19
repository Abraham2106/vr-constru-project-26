namespace ProyectoVR.Core
{
    /// <summary>Capas con colliders caminables (Default + Estructura + Teleport).</summary>
    public static class WalkableLayerMask
    {
        public const int Value = (1 << 0) | (1 << 6) | (1 << 8);
    }
}
