using System.Collections.Generic;
using UnityEngine;
namespace Unomata.Gameplay
{
    public sealed class UnityShotWorldQuery : IShotWorldQuery
    {
        private readonly HashSet<int> _ignored = new HashSet<int>();
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private readonly Collider[] _overlaps = new Collider[64];
        public void SetIgnoredColliders(int[] colliderIds)
        {
            _ignored.Clear();
            if (colliderIds != null) foreach (int id in colliderIds) _ignored.Add(id);
        }
        public bool IsInsideObstacle(Vector3 origin, float radius, int mask)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, radius, _overlaps, mask, QueryTriggerInteraction.Ignore);
            var overlaps = count == _overlaps.Length ? Physics.OverlapSphere(origin, radius, mask, QueryTriggerInteraction.Ignore) : _overlaps;
            if (overlaps != _overlaps) count = overlaps.Length;
            for (int i = 0; i < count; i++)
            {
                var collider = overlaps[i];
                if (collider == null || _ignored.Contains(collider.GetInstanceID())) continue;
                if ((collider.ClosestPoint(origin) - origin).sqrMagnitude <= 0.00000001f) return true;
            }
            return false;
        }
        public ShotHit Raycast(Vector3 origin, Vector3 direction, float range, int mask)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, range, mask, QueryTriggerInteraction.Ignore);
            var hits = count == _hits.Length ? Physics.RaycastAll(origin, direction, range, mask, QueryTriggerInteraction.Ignore) : _hits;
            if (hits != _hits) count = hits.Length;
            float nearest = float.PositiveInfinity;
            var result = default(ShotHit);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || _ignored.Contains(hit.collider.GetInstanceID()) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                result = new ShotHit(hit.point, hit.normal, hit.distance, hit.collider.GetInstanceID());
            }
            return result;
        }
        public void Clear()
        {
            _ignored.Clear();
            System.Array.Clear(_hits, 0, _hits.Length);
            System.Array.Clear(_overlaps, 0, _overlaps.Length);
        }
    }
}
