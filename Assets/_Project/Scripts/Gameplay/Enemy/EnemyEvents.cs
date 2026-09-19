using System;
namespace Unomata.Gameplay
{
    public readonly struct EnemyRegisteredEvent
    {
        public EnemySnapshot State { get; }
        public EnemyRegisteredEvent(EnemySnapshot state) { State = state; }
    }
    public readonly struct EnemyUnregisteredEvent
    {
        public Guid EnemyId { get; }
        public EnemyUnregisteredEvent(Guid enemyId) { EnemyId = enemyId; }
    }
    public readonly struct EnemyDamagedEvent
    {
        public EnemyDamageResult Result { get; }
        public EnemyDamagedEvent(EnemyDamageResult result) { Result = result; }
    }
    public readonly struct EnemyDiedEvent
    {
        public EnemyDamageResult Result { get; }
        public EnemyDiedEvent(EnemyDamageResult result) { Result = result; }
    }
    public readonly struct EnemyHackFactorChangedEvent
    {
        public Guid EnemyId { get; }
        public float Factor { get; }
        public EnemyHackFactorChangedEvent(Guid id, float factor) { EnemyId = id; Factor = factor; }
    }
}
