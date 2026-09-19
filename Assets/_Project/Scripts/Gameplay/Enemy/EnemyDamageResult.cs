using System;
using UnityEngine;
namespace Unomata.Gameplay
{
    public readonly struct EnemyDamageResult
    {
        public bool Accepted { get; }
        public Guid EnemyId { get; }
        public ShotId Shot { get; }
        public float RawDamage { get; }
        public float BaseDamageReduction { get; }
        public float HackFactor { get; }
        public float RemainingReduction { get; }
        public float ResolvedDamage { get; }
        public float AppliedDamage { get; }
        public float PreviousHp { get; }
        public float Hp { get; }
        public Vector3 Position { get; }
        public bool Killed => Accepted && PreviousHp > 0 && Hp <= 0;
        public EnemyDamageResult(EnemySnapshot previous, ShotId shot, float rawDamage,
            DamageCalculation calculation, float appliedDamage, float hp, Vector3 position)
        {
            Accepted = true; EnemyId = previous.Id; Shot = shot; RawDamage = rawDamage;
            BaseDamageReduction = previous.BaseDamageReduction; HackFactor = previous.HackFactor;
            RemainingReduction = calculation.RemainingReduction; ResolvedDamage = calculation.ResolvedDamage;
            AppliedDamage = appliedDamage; PreviousHp = previous.Hp; Hp = hp; Position = position;
        }
    }
}
