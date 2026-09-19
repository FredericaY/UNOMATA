using UnityEngine;
namespace Unomata.Gameplay
{
    public static class CombatNumbers
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        public static bool IsNonNegative(float value) => IsFinite(value) && value >= 0f;
    }
}
