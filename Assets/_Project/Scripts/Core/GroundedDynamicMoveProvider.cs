using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace ProyectoVR.Core
{
    /// <summary>
    /// DynamicMoveProvider con guardas antes de Quaternion.FromToRotation (IsNormalized en XRI).
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Grounded Dynamic Move Provider")]
    [DefaultExecutionOrder(100)]
    public class GroundedDynamicMoveProvider : DynamicMoveProvider
    {
        protected override Vector3 ComputeDesiredMove(Vector2 input)
        {
            if (!HasSafeGroundedForward())
                return Vector3.zero;

            return base.ComputeDesiredMove(input);
        }

        bool HasSafeGroundedForward()
        {
            var xrOrigin = mediator.xrOrigin;
            if (xrOrigin?.Origin == null)
                return false;

            var forwardSourceTransform = forwardSource != null
                ? forwardSource
                : xrOrigin.Camera != null ? xrOrigin.Camera.transform : null;
            if (forwardSourceTransform == null)
                return false;

            var originTransform = xrOrigin.Origin.transform;
            if (!DirectionUtil.TryGetUnitVector(originTransform.forward, out _))
                return false;

            var inputForwardInWorldSpace = forwardSourceTransform.forward;
            return DirectionUtil.TryGetGroundedMoveFrameRotation(
                originTransform,
                inputForwardInWorldSpace,
                forwardSourceTransform,
                out _);
        }
    }
}
