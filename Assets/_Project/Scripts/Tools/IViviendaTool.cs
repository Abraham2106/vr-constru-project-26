namespace Vivienda.Tools
{
    /// <summary>Contrato para herramientas activadas desde el menú XR (Agente 3).</summary>
    public interface IViviendaTool
    {
        string ToolId { get; }
        bool IsActive { get; }
        void SetActive(bool active);
        void OnTriggerDown(ToolRaycastHit hit);
        void OnSecondaryDown();
    }

    public readonly struct ToolRaycastHit
    {
        public readonly bool HasHit;
        public readonly UnityEngine.Vector3 Point;
        public readonly UnityEngine.Vector3 Normal;
        public readonly UnityEngine.Transform Target;

        public ToolRaycastHit(bool hasHit, UnityEngine.Vector3 point, UnityEngine.Vector3 normal, UnityEngine.Transform target)
        {
            HasHit = hasHit;
            Point = point;
            Normal = normal;
            Target = target;
        }

        public static ToolRaycastHit Miss => new(false, default, default, null);
    }
}
