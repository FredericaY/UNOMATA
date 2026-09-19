namespace Unomata.Gameplay
{
    public readonly struct WeaponSettings
    {
        public float Damage { get; }
        public float ShotsPerSecond { get; }
        public float Range { get; }
        public int HitMask { get; }
        public float OriginRadius { get; }
        public WeaponSettings(float damage, float shotsPerSecond, float range, int hitMask, float originRadius = 0.01f)
        { Damage = damage; ShotsPerSecond = shotsPerSecond; Range = range; HitMask = hitMask; OriginRadius = originRadius; }
        public bool TryValidate(out string error)
        {
            error = !CombatNumbers.IsNonNegative(Damage) ? "Damage must be finite and nonnegative." :
                !CombatNumbers.IsFinite(ShotsPerSecond) || ShotsPerSecond <= 0 ? "ShotsPerSecond must be finite and positive." :
                !CombatNumbers.IsFinite(Range) || Range <= 0 ? "Range must be finite and positive." :
                HitMask == 0 ? "HitMask must include environment and targets." :
                !CombatNumbers.IsFinite(OriginRadius) || OriginRadius <= 0 ? "OriginRadius must be finite and positive." : null;
            return error == null;
        }
    }
}
