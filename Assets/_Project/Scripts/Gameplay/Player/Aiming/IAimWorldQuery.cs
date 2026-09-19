using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public interface IAimWorldQuery : IUtility
    {
        void SetIgnoredColliders(int[] ids);
        bool Raycast(Vector3 origin, Vector3 direction, float distance, int mask, out Vector3 point);
        bool IsInsideObstacle(Vector3 origin, float radius, int mask);
        void Clear();
    }
}