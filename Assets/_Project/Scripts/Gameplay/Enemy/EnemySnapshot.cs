using System;
namespace Unomata.Gameplay
{
    public readonly struct EnemySnapshot
    {
        public Guid Id { get; }
        public bool Exists => Id != Guid.Empty;
        public float MaxHp { get; }
        public float Hp { get; }
        public float BaseDamageReduction { get; }
        public float HackFactor { get; }
        public bool IsAlive => Exists && Hp > 0;
        public EnemySnapshot(Guid id, float maxHp, float hp, float reduction, float factor)
        { Id = id; MaxHp = maxHp; Hp = hp; BaseDamageReduction = reduction; HackFactor = factor; }
    }
}
