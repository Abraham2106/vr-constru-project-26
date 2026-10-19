using UnityEngine;

namespace ProyectoVR.Core
{
    /// <summary>Evita normalizar o rotar con vectores degenerados (assert IsNormalized en Unity).</summary>
    public static class DirectionUtil
    {
        public const float MinSqrMagnitude = 1e-6f;

        public static bool TryGetUnitVector(Vector3 worldDirection, out Vector3 unit)
        {
            var sqr = worldDirection.sqrMagnitude;
            if (sqr < MinSqrMagnitude)
            {
                unit = default;
                return false;
            }

            unit = worldDirection / Mathf.Sqrt(sqr);
            return true;
        }

        public static bool TryGetHorizontalUnit(Vector3 worldDirection, out Vector3 unit)
        {
            worldDirection.y = 0f;
            return TryGetUnitVector(worldDirection, out unit);
        }

        /// <summary>Ángulo firmado en el plano XZ entre dos direcciones horizontales unitarias.</summary>
        public static float SignedAngleXZ(Vector3 fromUnit, Vector3 toUnit)
        {
            return Mathf.Atan2(
                fromUnit.z * toUnit.x - fromUnit.x * toUnit.z,
                fromUnit.x * toUnit.x + fromUnit.z * toUnit.z) * Mathf.Rad2Deg;
        }

        public static bool TryLookRotation(Vector3 forward, Vector3 upwards, out Quaternion rotation)
        {
            if (!TryGetUnitVector(forward, out var f))
            {
                rotation = Quaternion.identity;
                return false;
            }

            if (!TryGetUnitVector(upwards, out var u))
            {
                rotation = Quaternion.identity;
                return false;
            }

            rotation = Quaternion.LookRotation(f, u);
            return true;
        }

        public static bool TryProjectOnPlaneUnit(Vector3 vector, Vector3 planeNormal, out Vector3 unit)
        {
            var projected = Vector3.ProjectOnPlane(vector, planeNormal);
            return TryGetUnitVector(projected, out unit);
        }

        /// <summary>
        /// Equivalente seguro a la rotación de marco usada por ContinuousMoveProvider (evita FromToRotation con dir cero).
        /// </summary>
        public static bool TryGetGroundedMoveFrameRotation(
            Transform originTransform,
            Vector3 inputForwardInWorldSpace,
            Transform forwardSourceTransform,
            out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (originTransform == null)
                return false;

            var originUp = originTransform.up;
            var forward = inputForwardInWorldSpace;

            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(forward, originUp)), 1f))
            {
                if (forwardSourceTransform == null)
                    return false;
                forward = -forwardSourceTransform.up;
            }

            if (!TryGetUnitVector(originTransform.forward, out var originForwardUnit))
                return false;

            if (!TryProjectOnPlaneUnit(forward, originUp, out var projectedUnit))
                return false;

            rotation = Quaternion.FromToRotation(originForwardUnit, projectedUnit);
            return true;
        }
    }
}
