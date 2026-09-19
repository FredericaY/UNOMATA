using System;
using QFramework;

namespace Unomata.Gameplay
{
    // Per-view subscription lifetime. Only value snapshots cross the business boundary.
    internal sealed class EnemyViewBinding : IDisposable
    {
        private readonly EnemyController _source;
        private readonly IArchitecture _architecture;
        private readonly EnemyModel _model;
        private readonly Action<EnemySnapshot> _rebound, _changed;
        private readonly Action<EnemyDamageResult> _damaged, _died;
        private readonly IUnRegister[] _subscriptions;
        public EnemySnapshot State { get; private set; }
        public bool IsContextAlive => _model != null &&
            ReferenceEquals(_architecture.GetModel<EnemyModel>(), _model);

        public EnemyViewBinding(EnemyController source, Action<EnemySnapshot> rebound,
            Action<EnemySnapshot> changed = null, Action<EnemyDamageResult> damaged = null,
            Action<EnemyDamageResult> died = null)
        {
            _source = source;
            _rebound = rebound; _changed = changed; _damaged = damaged; _died = died;
            _architecture = GameApp.Interface;
            _model = _architecture.GetModel<EnemyModel>();
            _subscriptions = new[]
            {
                _architecture.RegisterEvent<EnemyRegisteredEvent>(Registered),
                _architecture.RegisterEvent<EnemyUnregisteredEvent>(Unregistered),
                _architecture.RegisterEvent<EnemyDamagedEvent>(Damaged),
                _architecture.RegisterEvent<EnemyDiedEvent>(Died),
                _architecture.RegisterEvent<EnemyHackFactorChangedEvent>(FactorChanged)
            };
            State = Read();
            _rebound(State);
        }
        private EnemySnapshot Read() => IsContextAlive && _source != null && _source.isActiveAndEnabled
            ? _architecture.SendQuery(new EnemySnapshotQuery(_source.Id)) : default;
        private bool Matches(Guid id) => IsContextAlive && _source != null &&
            _source.isActiveAndEnabled && id != Guid.Empty && _source.Id == id;
        private void Registered(EnemyRegisteredEvent fact)
        {
            if (!Matches(fact.State.Id)) return;
            State = Read(); _rebound(State);
        }
        private void Unregistered(EnemyUnregisteredEvent fact)
        {
            if (fact.EnemyId != State.Id) return;
            State = default; _rebound(State);
        }
        private void Damaged(EnemyDamagedEvent fact)
        {
            if (!Matches(fact.Result.EnemyId) || !fact.Result.Accepted) return;
            State = Read();
            if (!State.Exists) return;
            _changed?.Invoke(State); _damaged?.Invoke(fact.Result);
        }
        private void Died(EnemyDiedEvent fact)
        {
            if (!Matches(fact.Result.EnemyId) || !fact.Result.Killed) return;
            State = Read();
            if (!State.Exists || State.IsAlive) return;
            _changed?.Invoke(State); _died?.Invoke(fact.Result);
        }
        private void FactorChanged(EnemyHackFactorChangedEvent fact)
        {
            if (!Matches(fact.EnemyId)) return;
            State = Read(); _changed?.Invoke(State);
        }
        public void Dispose()
        {
            foreach (var subscription in _subscriptions) subscription.UnRegister();
            State = default;
        }
    }
}
