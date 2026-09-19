using System.Collections.Generic;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class UnityAimWorldQuery : IAimWorldQuery
    {
        private readonly HashSet<int> _ignored = new HashSet<int>();
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private readonly Collider[] _overlaps = new Collider[64];

        public void SetIgnoredColliders(int[] ids)
        {
            _ignored.Clear();
            if (ids != null) foreach (var id in ids) _ignored.Add(id);
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float distance, int mask, out Vector3 point)
        {
            point = Vector3.zero;
            if (distance <= 0 || direction.sqrMagnitude < 1e-12f) return false;
            var count = Physics.RaycastNonAlloc(origin, direction.normalized, _hits, distance, mask, QueryTriggerInteraction.Ignore);
            // Dense overlap is an exceptional case; do not silently drop the nearest non-player hit.
            var hits = count == _hits.Length
                ? Physics.RaycastAll(origin, direction.normalized, distance, mask, QueryTriggerInteraction.Ignore)
                : _hits;
            if (hits != _hits) count = hits.Length;
            bool found = false;
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || _ignored.Contains(hit.collider.GetInstanceID()) || hit.distance >= closest) continue;
                closest = hit.distance; point = hit.point; found = true;
            }
            return found;
        }

        public bool IsInsideObstacle(Vector3 origin, float radius, int mask)
        {
            var count = Physics.OverlapSphereNonAlloc(origin, Mathf.Max(radius, 0.001f), _overlaps, mask, QueryTriggerInteraction.Ignore);
            var colliders = count == _overlaps.Length
                ? Physics.OverlapSphere(origin, Mathf.Max(radius, 0.001f), mask, QueryTriggerInteraction.Ignore)
                : _overlaps;
            if (colliders != _overlaps) count = colliders.Length;
            for (int i = 0; i < count; i++)
            {
                var c = colliders[i];
                if (c == null || _ignored.Contains(c.GetInstanceID())) continue;
                if ((c.ClosestPoint(origin) - origin).sqrMagnitude <= 0.00000001f) return true;
            }
            return false;
        }

        public void Clear()
        {
            _ignored.Clear();
            System.Array.Clear(_hits, 0, _hits.Length);
            System.Array.Clear(_overlaps, 0, _overlaps.Length);
        }
    }
}