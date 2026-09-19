using System;
using UnityEngine;

namespace Unomata.Gameplay
{
    public readonly struct AimFrameInput
    {
        public Guid Context { get; }
        public int Frame { get; }
        public bool IsAiming { get; }
        public bool IsTransitioning { get; }
        public Vector3 CameraPosition { get; }
        public Vector3 CameraForward { get; }
        public Vector3 Up { get; }
        public Vector3 PivotPosition { get; }
        public Vector3 MuzzleLocalPosition { get; }
        public Quaternion MuzzleLocalRotation { get; }
        public float FarDistance { get; }
        public int CameraMask { get; }
        public int MuzzleMask { get; }
        public float MinimumTravel { get; }
        public float OriginRadius { get; }

        public AimFrameInput(Guid context, int frame, bool aiming, bool transitioning,
            Vector3 cameraPosition, Vector3 cameraForward, Vector3 up, Vector3 pivotPosition,
            Vector3 muzzleLocalPosition, Quaternion muzzleLocalRotation, float farDistance,
            int cameraMask, int muzzleMask, float minimumTravel = 0.05f, float originRadius = 0.01f)
        {
            Context = context; Frame = frame; IsAiming = aiming; IsTransitioning = transitioning;
            CameraPosition = cameraPosition; CameraForward = cameraForward; Up = up; PivotPosition = pivotPosition;
            MuzzleLocalPosition = muzzleLocalPosition; MuzzleLocalRotation = muzzleLocalRotation;
            FarDistance = farDistance; CameraMask = cameraMask; MuzzleMask = muzzleMask;
            MinimumTravel = minimumTravel; OriginRadius = originRadius;
        }
    }

    public readonly struct AimPoseSolution
    {
        public Guid Context { get; }
        public int Frame { get; }
        public bool IsValid { get; }
        public AimStatus Status { get; }
        public AimFailure Failure { get; }
        public Vector3 Target { get; }
        public Vector3 PivotPosition { get; }
        public Quaternion PivotRotation { get; }

        public AimPoseSolution(Guid context, int frame, bool valid, AimStatus status, AimFailure failure,
            Vector3 target, Vector3 pivotPosition, Quaternion pivotRotation)
        {
            Context = context; Frame = frame; IsValid = valid; Status = status; Failure = failure;
            Target = target; PivotPosition = pivotPosition; PivotRotation = pivotRotation;
        }
    }
}