using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
namespace Unomata.Gameplay
{
    public sealed class EnemySystem : AbstractSystem
    {
        private EnemyModel _model;
        protected override void OnInit() { _model = this.GetModel<EnemyModel>(); }

        public bool Register(Guid id, EnemySettings settings, int[] colliderIds)
        {
            if (!settings.TryValidate(out var error)) return Reject(error);
            if (id == Guid.Empty || _model.Records.ContainsKey(id)) return Reject("Enemy identity is empty or already registered.");
            if (colliderIds == null || colliderIds.Length == 0) return Reject("Enemy needs a collider identity.");
            var unique = new HashSet<int>();
            foreach (int colliderId in colliderIds)
                if (colliderId == 0 || !unique.Add(colliderId) || _model.ColliderOwners.ContainsKey(colliderId))
                    return Reject("Enemy collider identity is zero, duplicated or already owned.");
            var record = new EnemyModel.Record
            {
                State = new EnemySnapshot(id, settings.MaxHp, settings.MaxHp, settings.BaseDamageReduction, settings.HackFactor),
                ColliderIds = (int[])colliderIds.Clone()
            };
            _model.Records.Add(id, record);
            foreach (int colliderId in record.ColliderIds) _model.ColliderOwners.Add(colliderId, id);
            this.SendEvent(new EnemyRegisteredEvent(record.State));
            return true;
        }

        public void Unregister(Guid id)
        {
            if (!_model.Records.TryGetValue(id, out var record)) return;
            foreach (int colliderId in record.ColliderIds) _model.ColliderOwners.Remove(colliderId);
            _model.Records.Remove(id);
            this.SendEvent(new EnemyUnregisteredEvent(id));
        }

        public EnemySnapshot Read(Guid id) => _model.Records.TryGetValue(id, out var record) ? record.State : default;
        public EnemySnapshot ReadCollider(int colliderId) => _model.ColliderOwners.TryGetValue(colliderId, out var id) ? Read(id) : default;
        public EnemySnapshot[] ReadAll()
        {
            var states = new EnemySnapshot[_model.Records.Count];
            int index = 0;
            foreach (var record in _model.Records.Values) states[index++] = record.State;
            return states;
        }

        public bool SetHackFactor(Guid id, float factor)
        {
            if (!CombatNumbers.IsNonNegative(factor)) return Reject("HackFactor must be finite and nonnegative.");
            if (!_model.Records.TryGetValue(id, out var record) || !record.State.IsAlive) return false;
            var old = record.State;
            if (old.HackFactor == factor) return true;
            record.State = new EnemySnapshot(id, old.MaxHp, old.Hp, old.BaseDamageReduction, factor);
            this.SendEvent(new EnemyHackFactorChangedEvent(id, factor));
            return true;
        }

        public EnemyDamageResult ApplyDamage(Guid id, ShotId shot, float rawDamage, Vector3 position)
        {
            if (!shot.IsValid || !CombatNumbers.IsFinite(position)) { Reject("Invalid shot identity or hit position."); return default; }
            if (!_model.ActiveShotContexts.Contains(shot.Context) || !_model.Records.TryGetValue(id, out var record) || !record.State.IsAlive) return default;
            if (record.ConsumedShots.TryGetValue(shot.Context, out long sequence) && sequence >= shot.Sequence) return default;
            var before = record.State;
            if (!DamageCalculation.TryCalculate(rawDamage, before.BaseDamageReduction, before.HackFactor, out var calculation))
            { Reject("Invalid damage parameters or calculation overflow."); return default; }
            float applied = Mathf.Min(before.Hp, calculation.ResolvedDamage);
            float hp = Mathf.Max(0, before.Hp - applied);
            var result = new EnemyDamageResult(before, shot, rawDamage, calculation, applied, hp, position);
            record.ConsumedShots[shot.Context] = shot.Sequence;
            record.State = new EnemySnapshot(id, before.MaxHp, hp, before.BaseDamageReduction, before.HackFactor);
            _model.LastDamage = result;
            // State and watermark are committed before subscribers may reenter.
            this.SendEvent(new EnemyDamagedEvent(result));
            if (result.Killed) this.SendEvent(new EnemyDiedEvent(result));
            return result;
        }

        public void BeginShotContext(Guid context)
        {
            if (context != Guid.Empty) _model.ActiveShotContexts.Add(context);
        }
        public void ReleaseShotContext(Guid context)
        {
            _model.ActiveShotContexts.Remove(context);
            foreach (var record in _model.Records.Values) record.ConsumedShots.Remove(context);
        }
        private static bool Reject(string message) { Debug.LogWarning("[EnemySystem] " + message); return false; }
    }
}
