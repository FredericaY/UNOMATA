using System.Globalization;

namespace Unomata.Gameplay
{
    public readonly struct EnemyStatusData
    {
        public bool IsAlive { get; }
        public float HpFraction { get; }
        public float RemainingReduction { get; }
        public float Vulnerability { get; }
        public EnemyStatusData(EnemySnapshot snapshot)
        {
            IsAlive = snapshot.IsAlive;
            HpFraction = snapshot.Exists ? snapshot.Hp / snapshot.MaxHp : 0;
            DamageCalculation.TryCalculate(0, snapshot.BaseDamageReduction, snapshot.HackFactor, out var result);
            RemainingReduction = result.RemainingReduction;
            Vulnerability = result.Bonus;
        }
        public string Label
        {
            get
            {
                double percent = (double)(Vulnerability > 0 ? Vulnerability : RemainingReduction) * 100;
                string value = percent.ToString(percent >= 1000000 ? "0.#E+0" : "0.#", CultureInfo.InvariantCulture);
                return (Vulnerability > 0 ? "VULNERABLE +" : "RESIST ") + value + "%";
            }
        }
    }
}
