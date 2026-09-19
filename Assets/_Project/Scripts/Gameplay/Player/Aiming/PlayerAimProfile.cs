using UnityEngine;

namespace Unomata.Gameplay
{
    [CreateAssetMenu(menuName="UNOMATA/Player Aim Profile")]
    public sealed class PlayerAimProfile : ScriptableObject
    {
        [SerializeField] private float _farDistance = 200f;
        [SerializeField] private LayerMask _cameraMask = 1;
        [SerializeField] private LayerMask _muzzleMask = 1;
        [SerializeField] private float _blendDuration = 0.3f;
        [SerializeField,Range(0.03f,0.3f)] private float _motionBlendTime = 0.12f;
        [SerializeField,Range(0,1)] private float _torsoAssistWeight = 0.8f;
        [SerializeField,Range(0,1)] private float _jumpTakeoffPhase=0.15f;
        [SerializeField,Range(0,1)] private float _jumpApexPhase=0.40f;
        [SerializeField,Range(0,1)] private float _jumpLandingPhase=0.90f;
        [SerializeField,Range(0,1)] private float _fallStartPhase=0.75f;
        [SerializeField,Range(0.01f,0.1f)] private float _jumpBlendDuration=0.04f;
        public float JumpTakeoffPhase=>_jumpTakeoffPhase;
        public float JumpApexPhase=>_jumpApexPhase;
        public float JumpLandingPhase=>_jumpLandingPhase;
        public float FallStartPhase=>_fallStartPhase;
        public float JumpBlendDuration=>_jumpBlendDuration;
        public float MotionBlendTime => _motionBlendTime;
        public float TorsoAssistWeight => _torsoAssistWeight;
        [SerializeField] private Vector3 _gripOffset = new Vector3(0.2f,-0.08f,0.22f);
        [SerializeField] private Vector3 _leftElbow = new Vector3(-0.45f,1.15f,0);
        [SerializeField] private Vector3 _rightElbow = new Vector3(0.5f,1.15f,-0.1f);
        [SerializeField] private Quaternion[] _neutralSpine = new Quaternion[3];
        [SerializeField] private float[] _pitchFractions = {0.15f,0.35f,0.55f};
        [SerializeField] private float[] _yawFractions = {0.3f,0.6f,1f};

        [SerializeField] private float _forwardWalkSpeed=1.523172f;
        [SerializeField] private float _backWalkSpeed=1.452247f;
        [SerializeField] private float _sideWalkSpeed=1.396815f;
        [SerializeField] private float _runStrideSpeed=5.1104f;
        public float RunStrideSpeed=>_runStrideSpeed;
        public float ForwardWalkSpeed=>_forwardWalkSpeed;
        public float BackWalkSpeed=>_backWalkSpeed;
        public float SideWalkSpeed=>_sideWalkSpeed;
        public float FarDistance => _farDistance;
        public int CameraMask => _cameraMask.value;
        public int MuzzleMask => _muzzleMask.value;
        public float BlendDuration => _blendDuration;
        public Vector3 GripOffset => _gripOffset;
        public Vector3 LeftElbow => _leftElbow;
        public Vector3 RightElbow => _rightElbow;
        public Quaternion NeutralSpine(int index) => _neutralSpine[index];
        public float PitchFraction(int index) => _pitchFractions[index];
        public float YawFraction(int index) => _yawFractions[index];

        public bool IsValid => _farDistance>0 && AimGeometry.Finite(_farDistance) &&
            AimGeometry.Finite(_blendDuration) && _blendDuration>0 && _blendDuration<=0.35f &&
            AimGeometry.Finite(_motionBlendTime) && _motionBlendTime>0 &&
            AimGeometry.Finite(_gripOffset) && AimGeometry.Finite(_leftElbow) && AimGeometry.Finite(_rightElbow) &&
            AimGeometry.Finite(_torsoAssistWeight) && _torsoAssistWeight>=0 && _torsoAssistWeight<=1 &&
            ValidSpeed(_forwardWalkSpeed) && ValidSpeed(_backWalkSpeed) && ValidSpeed(_sideWalkSpeed) && ValidSpeed(_runStrideSpeed) &&
            ValidFraction(_jumpTakeoffPhase) && ValidFraction(_jumpApexPhase) && ValidFraction(_jumpLandingPhase) &&
            _jumpTakeoffPhase<_jumpApexPhase && _jumpApexPhase<_jumpLandingPhase && ValidFraction(_fallStartPhase) &&
            AimGeometry.Finite(_jumpBlendDuration) && _jumpBlendDuration>0 && _jumpBlendDuration<=0.1f &&
            _cameraMask.value!=0 && _muzzleMask.value!=0 &&
            _neutralSpine!=null && _neutralSpine.Length==3 &&
            _pitchFractions!=null && _pitchFractions.Length==3 &&
            _yawFractions!=null && _yawFractions.Length==3 &&
            AimGeometry.Finite(_neutralSpine[0]) && AimGeometry.Finite(_neutralSpine[1]) && AimGeometry.Finite(_neutralSpine[2]) &&
            ValidFraction(_pitchFractions[0]) && ValidFraction(_pitchFractions[1]) && ValidFraction(_pitchFractions[2]) &&
            ValidFraction(_yawFractions[0]) && ValidFraction(_yawFractions[1]) && ValidFraction(_yawFractions[2]);
        private static bool ValidSpeed(float value)=>AimGeometry.Finite(value)&&value>0;
        private static bool ValidFraction(float value)=>AimGeometry.Finite(value)&&value>=0&&value<=1;
    }
}