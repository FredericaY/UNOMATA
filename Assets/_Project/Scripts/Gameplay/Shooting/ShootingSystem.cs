using System;
using QFramework;
using UnityEngine;
namespace Unomata.Gameplay
{
    public sealed class ShootingSystem : AbstractSystem
    {
        private ShootingModel _model;
        private PlayerInputModel _input;
        private AimModel _aim;
        private EnemySystem _enemies;
        private AudioSystem _audio;
        private IShotWorldQuery _world;
        private bool _reportedBadDelta;
        protected override void OnInit()
        {
            _model = this.GetModel<ShootingModel>();
            _input = this.GetModel<PlayerInputModel>();
            _aim = this.GetModel<AimModel>();
            _enemies = this.GetSystem<EnemySystem>();
            _audio = this.GetSystem<AudioSystem>();
            _world = this.GetUtility<IShotWorldQuery>();
        }
        public bool Begin(Guid context, Guid aimContext, WeaponSettings settings, int[] ignoredColliders)
        {
            if (!settings.TryValidate(out var error)) { Debug.LogWarning("[ShootingSystem] " + error); return false; }
            if (context == Guid.Empty || aimContext == Guid.Empty || _aim.ActiveContext != aimContext) return false;
            if (_model.Context == context) return true;
            End(_model.Context);
            _model.Context = context; _model.AimContext = aimContext; _model.Settings = settings;
            _model.Clock = _model.NextShotTime = 0; _model.LastFrame = -1; _model.ShotCount = 0;
            _model.WasEligible = false; _model.RequiresFireRelease = _input.Fire.Value;
            _model.LastShot = default; _reportedBadDelta = false;
            _world.SetIgnoredColliders(ignoredColliders);
            _enemies.BeginShotContext(context);
            return true;
        }

        public void Tick(Guid context, int frame, float deltaTime)
        {
            if (context == Guid.Empty || context != _model.Context || frame <= _model.LastFrame) return;
            _model.LastFrame = frame;
            if (!CombatNumbers.IsNonNegative(deltaTime))
            {
                if (!_reportedBadDelta) Debug.LogWarning("[ShootingSystem] Delta time must be finite and nonnegative.");
                _reportedBadDelta = true; _model.WasEligible = false; return;
            }
            _model.Clock += deltaTime;
            if (!_input.Fire.Value) _model.RequiresFireRelease = false;
            var snapshot = _aim.Snapshot;
            var pose = _aim.Solution;
            bool eligible = !_model.RequiresFireRelease && _input.Fire.Value && _input.IsAiming.Value &&
                _aim.ActiveContext == _model.AimContext && pose.Context == _model.AimContext && pose.Frame == frame &&
                pose.IsValid && pose.Status == AimStatus.Ready &&
                snapshot.SceneGeneration == _model.AimContext && snapshot.FrameId == frame &&
                (snapshot.Status == AimStatus.Ready ||
                 (snapshot.Status == AimStatus.Blocked && snapshot.Failure == AimFailure.MuzzleObstructed)) &&
                CombatNumbers.IsFinite(snapshot.MuzzlePosition) && CombatNumbers.IsFinite(snapshot.BarrelDirection) &&
                snapshot.BarrelDirection.sqrMagnitude > 0.000001f;
            if (!eligible)
            {
                _model.WasEligible = false;
                _model.NextShotTime = Math.Max(_model.NextShotTime, _model.Clock);
                return;
            }
            if (_world.IsInsideObstacle(snapshot.MuzzlePosition, _model.Settings.OriginRadius, _model.Settings.HitMask))
            {
                _model.WasEligible = false;
                _model.NextShotTime = Math.Max(_model.NextShotTime, _model.Clock);
                return;
            }
            if (!_model.WasEligible) _model.NextShotTime = Math.Max(_model.NextShotTime, _model.Clock);
            _model.WasEligible = true;
            if (_model.Clock + 0.0000001d < _model.NextShotTime) return;

            double period = 1d / _model.Settings.ShotsPerSecond;
            _model.NextShotTime = _model.Clock - _model.NextShotTime >= period
                ? _model.Clock + period : _model.NextShotTime + period;
            var shot = new ShotId(context, ++_model.ShotCount);
            Vector3 direction = snapshot.BarrelDirection.normalized;
            ShotHit hit = _world.Raycast(snapshot.MuzzlePosition, direction, _model.Settings.Range, _model.Settings.HitMask);
            Vector3 endPoint = hit.HasHit ? hit.Point : snapshot.MuzzlePosition + direction * _model.Settings.Range;
            var target = hit.HasHit ? _enemies.ReadCollider(hit.ColliderId) : default;
            var damage = target.IsAlive ? _enemies.ApplyDamage(target.Id, shot, _model.Settings.Damage, endPoint) : default;
            var kind = !hit.HasHit ? ShotHitKind.Miss : damage.Accepted ? ShotHitKind.Enemy : ShotHitKind.Surface;
            var fact = new ShotFiredEvent(shot, _model.AimContext, frame, _model.Clock, snapshot.MuzzlePosition,
                direction, endPoint, hit.Normal, kind, damage.Accepted ? target.Id : Guid.Empty, damage);
            _model.LastShot = fact;
            this.SendEvent(fact);
            _audio.Play(SoundId.GunShot, fact.Origin);
            if (kind != ShotHitKind.Miss) _audio.Play(kind == ShotHitKind.Enemy ? SoundId.HitEnemy : SoundId.HitSurface, fact.EndPoint);
        }

        public void End(Guid context)
        {
            if (context == Guid.Empty || context != _model.Context) return;
            _enemies.ReleaseShotContext(context);
            _world.Clear();
            _model.Context = _model.AimContext = Guid.Empty;
            _model.WasEligible = false; _model.RequiresFireRelease = true;
        }
        protected override void OnDeinit() { End(_model.Context); }
    }
}
