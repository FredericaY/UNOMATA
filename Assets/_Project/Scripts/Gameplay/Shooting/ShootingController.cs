using System;
using QFramework;
using UnityEngine;
namespace Unomata.Gameplay
{
    [DefaultExecutionOrder(600)]
    public sealed class ShootingController : MonoBehaviour, IController
    {
        [SerializeField] private PlayerAimPresentation _pose;
        [SerializeField] private RifleProfile _profile;
        private IArchitecture _architecture;
        private ShootingModel _model;
        private Guid _context;
        private bool _focused, _reported;
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;
        public Guid Context => _context;
        public RifleProfile Profile => _profile;
        public PlayerAimPresentation Pose => _pose;
        private bool ContextAlive => _architecture != null && _model != null &&
            ReferenceEquals(_architecture.GetModel<ShootingModel>(), _model);
        private void Awake() { _focused = Application.isFocused; }
        private void OnEnable() { _reported = false; }

        private void LateUpdate()
        {
            if (!_focused) { Stop(); return; }
            if (_pose == null || _profile == null)
            {
                if (!_reported) Debug.LogError("[ShootingController] Missing pose or rifle profile on " + name, this);
                _reported = true; Stop(); return;
            }
            if (!_pose.isActiveAndEnabled || !_pose.IsInitialized) { Stop(); return; }
            var snapshot = _pose.Snapshot;
            if (snapshot.SceneGeneration == Guid.Empty || snapshot.FrameId != Time.frameCount) { Stop(); return; }
            if (!ContextAlive || _context == Guid.Empty || _model.AimContext != snapshot.SceneGeneration)
            {
                Stop();
                if (!_profile.Settings.TryValidate(out var error))
                {
                    if (!_reported) Debug.LogError("[ShootingController] " + error, this);
                    _reported = true; return;
                }
                _architecture = GameApp.Interface;
                _model = _architecture.GetModel<ShootingModel>();
                var colliders = GetComponentsInChildren<Collider>(true);
                var ids = new int[colliders.Length];
                for (int i = 0; i < ids.Length; i++) ids[i] = colliders[i].GetInstanceID();
                _context = Guid.NewGuid();
                _architecture.SendCommand(new BeginShootingCommand(_context, snapshot.SceneGeneration, _profile.Settings, ids));
                if (_model.Context != _context) { _context = Guid.Empty; return; }
            }
            _architecture.SendCommand(new TickShootingCommand(_context, Time.frameCount, Time.deltaTime));
        }
        private void Stop()
        {
            if (ContextAlive && _context != Guid.Empty) _architecture.SendCommand(new EndShootingCommand(_context));
            _context = Guid.Empty;
        }
        private void OnApplicationFocus(bool focused) { _focused = focused; if (!focused) Stop(); }
        private void OnDisable() { Stop(); }
        private void OnDestroy() { Stop(); }
    }
}
