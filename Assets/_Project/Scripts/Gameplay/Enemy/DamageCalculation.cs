using System;
namespace Unomata.Gameplay
{
    public readonly struct DamageCalculation
    {
        public float RemainingReduction { get; }
        public float Bonus { get; }
        public float ResolvedDamage { get; }
        private DamageCalculation(float remaining, float bonus, float damage)
        { RemainingReduction = remaining; Bonus = bonus; ResolvedDamage = damage; }
        public static bool TryCalculate(float rawDamage, float reduction, float factor, out DamageCalculation result)
        {
            result = default;
            if (!CombatNumbers.IsNonNegative(rawDamage) || !CombatNumbers.IsNonNegative(factor) ||
                !CombatNumbers.IsFinite(reduction) || reduction < 0 || reduction > 1) return false;
            double remaining = reduction * (1d - Math.Min(factor, 1d));
            double bonus = Math.Max((double)factor - 1d, 0d);
            double damage = rawDamage * (1d - remaining) * (1d + bonus);
            if (double.IsNaN(damage) || double.IsInfinity(damage) || damage > float.MaxValue) return false;
            result = new DamageCalculation((float)remaining, (float)bonus, (float)damage);
            return true;
        }
    }
}
