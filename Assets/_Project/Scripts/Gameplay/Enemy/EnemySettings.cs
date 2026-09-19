namespace Unomata.Gameplay
{
    public readonly struct EnemySettings
    {
        public float MaxHp { get; }
        public float BaseDamageReduction { get; }
        public float HackFactor { get; }
        public EnemySettings(float maxHp, float reduction, float factor)
        { MaxHp = maxHp; BaseDamageReduction = reduction; HackFactor = factor; }
        public bool TryValidate(out string error)
        {
            error = !CombatNumbers.IsFinite(MaxHp) || MaxHp <= 0 ? "MaxHp must be finite and positive." :
                !CombatNumbers.IsFinite(BaseDamageReduction) || BaseDamageReduction < 0 || BaseDamageReduction > 1 ? "BaseDamageReduction must be in [0,1]." :
                !CombatNumbers.IsNonNegative(HackFactor) ? "HackFactor must be finite and nonnegative." : null;
            return error == null;
        }
    }
}
