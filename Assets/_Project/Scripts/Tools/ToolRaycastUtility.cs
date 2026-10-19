using UnityEngine;

namespace Vivienda.Tools
{
    public static class ToolRaycastUtility
    {
        const float DefaultMaxDistance = 40f;

        public static ToolRaycastHit RaycastFrom(Transform origin, LayerMask mask, float maxDistance = DefaultMaxDistance)
        {
            if (ProyectoVR.DesktopPreview.IsActive && Camera.main != null) origin = Camera.main.transform;
            if (origin == null)
                return ToolRaycastHit.Miss;

            var ray = new Ray(origin.position, origin.forward);
            if (!Physics.Raycast(ray, out var hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
                return ToolRaycastHit.Miss;

            return new ToolRaycastHit(true, hit.point, hit.normal, hit.transform);
        }
    }
}
