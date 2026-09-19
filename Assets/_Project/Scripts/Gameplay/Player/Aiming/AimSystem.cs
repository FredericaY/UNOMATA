using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class AimSystem : AbstractSystem, IAimSystem
    {
        private AimModel _model;
        private IAimWorldQuery _world;
        private AimFrameInput _pending;
        private int _latestFrame=-1;

        protected override void OnInit()
        {
            _model = this.GetModel<AimModel>();
            _world = this.GetUtility<IAimWorldQuery>();
        }

        public void BeginContext(Guid context, int[] ignoredColliders)
        {
            if (context == Guid.Empty) throw new ArgumentException("A scene context must have an identity.", nameof(context));
            _model.ActiveContext = context;
            _model.Solution = default;
            _model.Snapshot = AimSnapshot.Unavailable(context, -1, AimStatus.Inactive, AimFailure.None);
            _pending = default;
            _latestFrame=-1;
            _world.SetIgnoredColliders(ignoredColliders);
        }

        public void Prepare(AimFrameInput input)
        {
            if (input.Context == Guid.Empty || input.Context != _model.ActiveContext || input.Frame<_latestFrame) return;
            _latestFrame=input.Frame;
            _pending = input;
            var status = input.IsAiming ? AimStatus.Invalid : AimStatus.Inactive;
            _model.Snapshot = AimSnapshot.Unavailable(input.Context, input.Frame, status, AimFailure.StaleFrame);
            if (!input.IsAiming) { Fail(input, AimStatus.Inactive, AimFailure.None); return; }
            if (!AimGeometry.Finite(input.CameraPosition) || !AimGeometry.Finite(input.CameraForward) ||
                input.CameraForward.sqrMagnitude < 0.000001f || !AimGeometry.Finite(input.FarDistance) ||
                input.FarDistance <= 0 || !AimGeometry.Finite(input.OriginRadius) || input.OriginRadius < 0 ||
                input.CameraMask==0 || input.MuzzleMask==0 || !AimGeometry.Finite(input.MinimumTravel) || input.MinimumTravel<0 ||
                !AimGeometry.Finite(input.PivotPosition) || !AimGeometry.Finite(input.MuzzleLocalPosition) ||
                !AimGeometry.Finite(input.MuzzleLocalRotation) || !AimGeometry.Finite(input.Up) || input.Up.sqrMagnitude<0.000001f)
            { Fail(input, AimStatus.Invalid, AimFailure.MissingConfiguration); return; }
            if (_world.IsInsideObstacle(input.CameraPosition, input.OriginRadius, input.CameraMask))
            { Fail(input, AimStatus.Blocked, AimFailure.CameraInsideObstacle); return; }
            var direction = input.CameraForward.normalized;
            Vector3 target;
            if (!_world.Raycast(input.CameraPosition, direction, input.FarDistance, input.CameraMask, out target))
                target = input.CameraPosition + direction * input.FarDistance;
            // Reject points behind the held weapon before solving a pose that would turn it backwards.
            if (Vector3.Dot(target - input.PivotPosition, direction) <= input.MinimumTravel ||
                !AimGeometry.TrySolve(input.PivotPosition, target, input.MuzzleLocalPosition,
                    input.MuzzleLocalRotation, input.Up, input.MinimumTravel, out var rotation))
            { Fail(input, AimStatus.Invalid, AimFailure.TargetUnreachable); return; }
            _model.Solution = new AimPoseSolution(input.Context, input.Frame, true,
                input.IsTransitioning ? AimStatus.Transition : AimStatus.Ready,
                AimFailure.None, target, input.PivotPosition, rotation);
        }

        public void Complete(Guid context, int frame, Vector3 muzzlePosition, Vector3 barrelDirection, bool gripsReachable = true)
        {
            if (context != _model.ActiveContext || context == Guid.Empty ||
                context != _pending.Context || frame != _pending.Frame) return;
            var solution = _model.Solution;
            if (!solution.IsValid || solution.Context != context || solution.Frame != frame) return;
            if (!gripsReachable || !AimGeometry.Finite(muzzlePosition) || !AimGeometry.Finite(barrelDirection) || barrelDirection.sqrMagnitude < 0.000001f)
            { Fail(_pending, AimStatus.Invalid, AimFailure.InvalidPose); return; }
            var direction = barrelDirection.normalized;
            var delta = solution.Target - muzzlePosition;
            float distance = delta.magnitude;
            var status = solution.Status;
            var failure = AimFailure.None;
            bool blocked = false;
            var obstruction = Vector3.zero;
            // A lowering/raising barrel can temporarily face away; the solved target is still reachable.
            if (status != AimStatus.Transition && Vector3.Dot(delta, direction) <= _pending.MinimumTravel)
            { status = AimStatus.Invalid; failure = AimFailure.TargetUnreachable; }
            else if (_world.IsInsideObstacle(muzzlePosition, _pending.OriginRadius, _pending.MuzzleMask))
            { status = AimStatus.Blocked; failure = AimFailure.MuzzleInsideObstacle; blocked = true; obstruction = muzzlePosition; }
            else if (_world.Raycast(muzzlePosition, delta / distance, Mathf.Max(0, distance - 0.002f), _pending.MuzzleMask, out obstruction))
            { status = AimStatus.Blocked; failure = AimFailure.MuzzleObstructed; blocked = true; }
            _model.Snapshot = new AimSnapshot(context, frame, status, failure, muzzlePosition, direction,
                solution.Target, blocked, obstruction, AimGeometry.AngleDegrees(direction, delta));
        }

        private void Fail(AimFrameInput input, AimStatus status, AimFailure failure)
        {
            _model.Solution = new AimPoseSolution(input.Context, input.Frame, false, status, failure,
                Vector3.zero, input.PivotPosition, Quaternion.identity);
            _model.Snapshot = AimSnapshot.Unavailable(input.Context, input.Frame, status, failure);
        }

        public void Invalidate(Guid context, int frame, bool release)
        {
            if (context != _model.ActiveContext || context == Guid.Empty) return;
            _model.Solution = default;
            _model.Snapshot = AimSnapshot.Unavailable(context, frame, AimStatus.Inactive, AimFailure.InactiveContext);
            _pending = default;
            if (release) { _model.ActiveContext = Guid.Empty; _world.Clear(); }
        }

        protected override void OnDeinit()
        {
            _world.Clear();
            _model.ActiveContext = Guid.Empty;
            _model.Solution = default;
            _model.Snapshot = default;
        }
    }
}