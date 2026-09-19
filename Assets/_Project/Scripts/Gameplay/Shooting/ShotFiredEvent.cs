using System;
using UnityEngine;
namespace Unomata.Gameplay
{
    public enum ShotHitKind { Miss, Surface, Enemy }
    public readonly struct ShotFiredEvent
    {
        public ShotId Id { get; }
        public Guid AimContext { get; }
        public int Frame { get; }
        public double Time { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public Vector3 EndPoint { get; }
        public Vector3 Normal { get; }
        public ShotHitKind HitKind { get; }
        public Guid EnemyId { get; }
        public EnemyDamageResult Damage { get; }
        public ShotFiredEvent(ShotId id, Guid aimContext, int frame, double time, Vector3 origin,
            Vector3 direction, Vector3 endPoint, Vector3 normal, ShotHitKind kind, Guid enemyId, EnemyDamageResult damage)
        {
            Id = id; AimContext = aimContext; Frame = frame; Time = time; Origin = origin;
            Direction = direction; EndPoint = endPoint; Normal = normal; HitKind = kind; EnemyId = enemyId; Damage = damage;
        }
    }
}
