using UnityEngine;
namespace Unomata.Gameplay
{
    public readonly struct ShotHit
    {
        public bool HasHit { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
        public int ColliderId { get; }
        public ShotHit(Vector3 point, Vector3 normal, float distance, int colliderId)
        { HasHit = true; Point = point; Normal = normal; Distance = distance; ColliderId = colliderId; }
    }
}
