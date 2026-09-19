using System;
using UnityEngine;

namespace Unomata.Gameplay
{
    public enum AimStatus { Inactive, Transition, Ready, Blocked, Invalid }
    public enum AimFailure { None, MissingConfiguration, StaleFrame, InactiveContext, CameraInsideObstacle, MuzzleInsideObstacle, TargetUnreachable, MuzzleObstructed, InvalidPose }

    public readonly struct AimSnapshot
    {
        public Guid SceneGeneration { get; }
        public int FrameId { get; }
        public AimStatus Status { get; }
        public AimFailure Failure { get; }
        public Vector3 MuzzlePosition { get; }
        public Vector3 BarrelDirection { get; }
        public Vector3 DesiredTarget { get; }
        public bool HasObstruction { get; }
        public Vector3 ObstructionPoint { get; }
        public float AimErrorDegrees { get; }

        public AimSnapshot(Guid sceneGeneration, int frameId, AimStatus status, AimFailure failure,
            Vector3 muzzlePosition, Vector3 barrelDirection, Vector3 desiredTarget,
            bool hasObstruction, Vector3 obstructionPoint, float aimErrorDegrees)
        {
            SceneGeneration = sceneGeneration; FrameId = frameId; Status = status; Failure = failure;
            MuzzlePosition = muzzlePosition; BarrelDirection = barrelDirection; DesiredTarget = desiredTarget;
            HasObstruction = hasObstruction; ObstructionPoint = obstructionPoint; AimErrorDegrees = aimErrorDegrees;
        }

        public static AimSnapshot Unavailable(Guid context, int frame, AimStatus status, AimFailure failure) =>
            new AimSnapshot(context, frame, status, failure, Vector3.zero, Vector3.zero, Vector3.zero, false, Vector3.zero, 0);
    }
}