using UnityEngine;

namespace Unomata.Gameplay
{
    /// <summary>Exact intersection of a pivoted barrel ray and a world target, including muzzle offset.</summary>
    public static class AimGeometry
    {
        public static bool TrySolve(Vector3 pivot, Vector3 target, Vector3 muzzleLocalPosition,
            Quaternion muzzleLocalRotation, Vector3 up, float minimumTravel, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (!Finite(pivot) || !Finite(target) || !Finite(muzzleLocalPosition) ||
                !Finite(muzzleLocalRotation) || !Finite(up) || !Finite(minimumTravel) || minimumTravel < 0)
                return false;
            var delta = target - pivot;
            float distanceSquared = delta.sqrMagnitude;
            if (distanceSquared < 0.000001f || up.sqrMagnitude < 0.000001f) return false;
            var local = Quaternion.Inverse(muzzleLocalRotation.normalized) * muzzleLocalPosition;
            float discriminant = distanceSquared - local.x * local.x - local.y * local.y;
            if (discriminant <= 0) return false;
            float travel = Mathf.Sqrt(discriminant) - local.z;
            if (travel < minimumTravel) return false;
            var localIntersection = local + Vector3.forward * travel;
            var direction = delta.normalized;
            var stableUp = Vector3.ProjectOnPlane(up, direction);
            if (stableUp.sqrMagnitude < 0.000001f)
                stableUp = Vector3.ProjectOnPlane(Mathf.Abs(direction.x) < 0.9f ? Vector3.right : Vector3.forward, direction);
            rotation = Quaternion.LookRotation(direction, stableUp.normalized)
                * PreciseFromTo(localIntersection.normalized, Vector3.forward)
                * Quaternion.Inverse(muzzleLocalRotation.normalized);
            return Finite(rotation);
        }

        private static Quaternion PreciseFromTo(Vector3 from, Vector3 to)
        {
            // FromToRotation treats very small angles as identity; that loses far-target parallax.
            var axis = Vector3.Cross(from, to);
            float sine = axis.magnitude;
            float cosine = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);
            if (sine < 1e-8f) return cosine >= 0 ? Quaternion.identity : Quaternion.AngleAxis(180, Vector3.up);
            return Quaternion.AngleAxis(Mathf.Atan2(sine, cosine) * Mathf.Rad2Deg, axis / sine);
        }
        public static float AngleDegrees(Vector3 a, Vector3 b)
        {
            if (!Finite(a) || !Finite(b) || a.sqrMagnitude < 1e-12f || b.sqrMagnitude < 1e-12f) return 180f;
            a.Normalize(); b.Normalize();
            return Mathf.Atan2(Vector3.Cross(a, b).magnitude, Mathf.Clamp(Vector3.Dot(a, b), -1, 1)) * Mathf.Rad2Deg;
        }

        public static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        public static bool Finite(Quaternion q) => Finite(q.x) && Finite(q.y) && Finite(q.z) && Finite(q.w) &&
            q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w > 0.000001f;
    }
}