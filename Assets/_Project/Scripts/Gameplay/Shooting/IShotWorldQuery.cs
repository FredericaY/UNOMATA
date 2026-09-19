using UnityEngine;
using QFramework;
namespace Unomata.Gameplay
{
    public interface IShotWorldQuery : IUtility
    {
        void SetIgnoredColliders(int[] colliderIds);
        bool IsInsideObstacle(Vector3 origin, float radius, int mask);
        ShotHit Raycast(Vector3 origin, Vector3 direction, float range, int mask);
        void Clear();
    }
}
