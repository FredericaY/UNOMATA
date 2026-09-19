using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyController : MonoBehaviour, IController
    {
        [SerializeField] private EnemyProfile _profile;
        [SerializeField] private Collider[] _colliders;
        private IArchitecture _architecture;
        private EnemyModel _model;
        private IUnRegister _deathSubscription, _unregisterSubscription;
        private Guid _id;

        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;
        public Guid Id => _id;
        public EnemyProfile Profile => _profile;
        public EnemySnapshot Snapshot => ContextAlive ? _architecture.SendQuery(new EnemySnapshotQuery(_id)) : default;
        private bool ContextAlive => _architecture != null && _model != null &&
            ReferenceEquals(_architecture.GetModel<EnemyModel>(), _model);

        private void OnEnable()
        {
            if (_colliders == null || _colliders.Length == 0) _colliders = GetComponentsInChildren<Collider>(true);
            SetCollisions(false);
            if (_profile == null || !_profile.Settings.TryValidate(out _))
            { Debug.LogError("[EnemyController] Missing or invalid enemy profile on " + name, this); return; }
            if (_colliders.Length == 0)
            { Debug.LogError("[EnemyController] No colliders on " + name, this); return; }
            var ids = new int[_colliders.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var collider = _colliders[i];
                if (!Owns(collider) || collider.isTrigger)
                { Debug.LogError("[EnemyController] Missing, foreign or trigger hit collider on " + name, this); return; }
                if ((Physics.DefaultRaycastLayers & (1 << collider.gameObject.layer)) == 0)
                { Debug.LogError("[EnemyController] Enemy must be on a raycastable layer.", this); return; }
                ids[i] = collider.GetInstanceID();
            }
            _architecture = GameApp.Interface;
            _model = _architecture.GetModel<EnemyModel>();
            _deathSubscription = _architecture.RegisterEvent<EnemyDiedEvent>(OnDied);
            _unregisterSubscription = _architecture.RegisterEvent<EnemyUnregisteredEvent>(fact =>
            { if (fact.EnemyId == _id) { SetCollisions(false); _id = Guid.Empty; } });
            _id = Guid.NewGuid();
            _architecture.SendCommand(new RegisterEnemyCommand(_id, _profile.Settings, ids));
            var state = Snapshot;
            if (!state.Exists) { Release(); return; }
            SetCollisions(state.IsAlive);
        }

        private bool Owns(Collider collider) => collider != null &&
            collider.GetComponentInParent<EnemyController>(true) == this;
        private void SetCollisions(bool active)
        {
            if (_colliders == null) return;
            foreach (var collider in _colliders) if (Owns(collider)) collider.enabled = active;
        }
        private void OnDied(EnemyDiedEvent fact)
        {
            if (fact.Result.EnemyId == _id && !Snapshot.IsAlive) SetCollisions(false);
        }
        private void Update()
        {
            if (_id != Guid.Empty && !ContextAlive) Release();
        }
        private void Release()
        {
            SetCollisions(false);
            _deathSubscription?.UnRegister();
            _deathSubscription = null;
            _unregisterSubscription?.UnRegister(); _unregisterSubscription = null;
            if (ContextAlive && _id != Guid.Empty) _architecture.SendCommand(new UnregisterEnemyCommand(_id));
            _id = Guid.Empty;
            _architecture = null;
            _model = null;
        }
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }
    }
}
