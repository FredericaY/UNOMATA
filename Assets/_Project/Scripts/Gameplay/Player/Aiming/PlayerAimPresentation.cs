using System;
using System.Collections.Generic;
using Cinemachine;
using QFramework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

namespace Unomata.Gameplay
{
    /// <summary>Single scene pose coordinator. Domain calculations are submitted through commands and read through queries.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class PlayerAimPresentation : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private Animator _animator;
        [SerializeField] private RuntimeAnimatorController _controller;
        [SerializeField] private RigBuilder _rigBuilder;
        [SerializeField] private Rig _torsoRig;
        [SerializeField] private Rig _handsRig;
        [SerializeField] private PlayerAimProfile _profile;
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private CinemachineVirtualCamera _aimCamera;
        [SerializeField] private Camera _renderCamera;
        [SerializeField] private Transform _weapon;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Transform _rightGrip;
        [SerializeField] private Transform _leftGrip;
        [SerializeField] private Transform _rightHand;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHint;
        [SerializeField] private Transform _leftHint;
        [SerializeField] private Transform _rightTarget;
        [SerializeField] private Transform _leftTarget;
        [SerializeField] private Transform[] _torsoTargets = new Transform[3];

        private IArchitecture _architecture;
        private PlayerInputModel _input;
        private AimModel _aimModel;
        private Guid _context;
        private PlayableGraph _graph;
        private Transform _rightUpperArm,_leftUpperArm,_rightElbow,_leftElbow;
        private Vector2 _motionBlend,_motionBlendVelocity;
        private AnimatorControllerPlayable _animation;
        private CinemachineBrain.UpdateMethod _oldCameraUpdate;
        private CinemachineBrain.BrainUpdateMethod _oldBlendUpdate;
        private AnimatorCullingMode _oldCulling;
        private Vector3 _muzzleOffset;
        private Quaternion _muzzleRotation;
        private bool _started, _initialized, _reported, _focused = true, _jumpInProgress;
        private static readonly int JumpStartState=Animator.StringToHash("Base Layer.JumpStart");
        private float _aimWeight, _gaitBlend, _gaitVelocity, _turnBlend, _turnVelocity;
        public float TurnBlend => _turnBlend;
        private int _lastEvaluationFrame = -1;
        private int _cameraPriority = int.MinValue;

        public float AimWeight => _aimWeight;
        public int PoseFrame { get; private set; } = -1;
        public int CameraFrame { get; private set; } = -1;
        public int EvaluationCount { get; private set; }
        public float AnimationTimeBeforeRig { get; private set; }
        public float AnimationTimeAfterRig { get; private set; }
        public Vector2 MotionBlend => _motionBlend;
        public Guid ContextId => _context;
        public Transform Muzzle => _muzzle;
        public Transform RightGrip => _rightGrip;
        public Transform LeftGrip => _leftGrip;
        public Transform RightHand => _rightHand;
        public Transform LeftHand => _leftHand;
        public PlayerMotor Motor => _motor;
        public bool IsInitialized => _initialized;
        public AimSnapshot Snapshot => ContextAlive
            ? _architecture.SendQuery(new AimSnapshotQuery(_context,Time.frameCount))
            : AimSnapshot.Unavailable(_context,Time.frameCount,AimStatus.Invalid,AimFailure.InactiveContext);
        public AnimatorStateInfo MovementState => _initialized ? _animation.GetCurrentAnimatorStateInfo(0) : default;
        public void GetMovementClips(List<AnimatorClipInfo> clips)
        {
            clips.Clear();
            if (_initialized) _animation.GetCurrentAnimatorClipInfo(0,clips);
        }

        private bool ContextAlive => _architecture!=null && _aimModel!=null &&
            ReferenceEquals(_architecture.GetModel<AimModel>(),_aimModel);

        private void Awake() { _focused=Application.isFocused; }
        private void OnEnable() { _reported=false; if (_started) TryInitialize(); }
        private void Start() { _started=true; TryInitialize(); }

        private string ConfigurationError()
        {
            string missing = null;
            if (_motor==null) missing="motor";
            else if (_animator==null || _controller==null) missing="animator/controller";
            else if (_rigBuilder==null || _torsoRig==null || _handsRig==null) missing="rig references";
            else if (_profile==null || !_profile.IsValid) missing="valid aim profile";
            else if (_brain==null || _aimCamera==null || _renderCamera==null) missing="camera references";
            else if (_weapon==null || _muzzle==null || _rightGrip==null || _leftGrip==null ||
                     _rightHand==null || _leftHand==null || _rightHint==null || _leftHint==null || _rightTarget==null || _leftTarget==null) missing="weapon/grips/arms";
            else if (_torsoTargets==null || _torsoTargets.Length!=3 ||
                     _torsoTargets[0]==null || _torsoTargets[1]==null || _torsoTargets[2]==null) missing="torso targets";
            return missing;
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;
            string missing=ConfigurationError();
            if (missing!=null)
            {
                if (!_reported) Debug.LogError("[PlayerAimPresentation] Missing "+missing+" on "+name,this);
                _reported=true; return false;
            }

            _architecture=GameApp.Interface;
            _input=_architecture.GetModel<PlayerInputModel>();
            _aimModel=_architecture.GetModel<AimModel>();
            _context=Guid.NewGuid();
            var colliders=GetComponentsInChildren<Collider>(true);
            var ignored=new int[colliders.Length];
            for(int i=0;i<ignored.Length;i++)ignored[i]=colliders[i].GetInstanceID();
            _architecture.SendCommand(new BeginAimContextCommand(_context,ignored));
            _rightUpperArm=_animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _leftUpperArm=_animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _rightElbow=_animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _leftElbow=_animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _muzzleOffset=_weapon.InverseTransformPoint(_muzzle.position);
            _muzzleRotation=Quaternion.Inverse(_weapon.rotation)*_muzzle.rotation;
            _oldCameraUpdate=_brain.m_UpdateMethod;
            _oldBlendUpdate=_brain.m_BlendUpdateMethod;
            _oldCulling=_animator.cullingMode;
            _brain.m_UpdateMethod=CinemachineBrain.UpdateMethod.ManualUpdate;
            _brain.m_BlendUpdateMethod=CinemachineBrain.BrainUpdateMethod.LateUpdate;
            _animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            _animator.applyRootMotion=false;
            _animator.runtimeAnimatorController=null;
            _rigBuilder.enabled=false;
            _graph=PlayableGraph.Create(name+"_AimPresentation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _animation=AnimatorControllerPlayable.Create(_graph,_controller);
            var output=AnimationPlayableOutput.Create(_graph,"Locomotion",_animator);
            output.SetSourcePlayable(_animation);
            if (!_rigBuilder.Build(_graph))
            {
                _rigBuilder.Clear();
                _graph.Destroy();
                _architecture.SendCommand(new InvalidateAimCommand(_context,Time.frameCount,true));
                _brain.m_UpdateMethod=_oldCameraUpdate;
                _brain.m_BlendUpdateMethod=_oldBlendUpdate;
                _animator.cullingMode=_oldCulling;
                _animator.runtimeAnimatorController=_controller;
                if (!_reported) Debug.LogError("[PlayerAimPresentation] Rig graph could not be built.",this);
                _reported=true; return false;
            }
            _graph.Play();
            _aimWeight=_gaitBlend=_gaitVelocity=_turnBlend=_turnVelocity=0;
            _jumpInProgress=false;
            _motionBlend=_motionBlendVelocity=Vector2.zero;
            _cameraPriority=int.MinValue;
            _lastEvaluationFrame=-1;
            _initialized=true;
            return true;
        }

        private void LateUpdate()
        {
            if (_initialized && (!ContextAlive || ConfigurationError()!=null)) Release();
            if (!TryInitialize() || !ContextAlive || _lastEvaluationFrame==Time.frameCount) return;
            if (!_motor.isActiveAndEnabled || _motor.MovementFrame!=Time.frameCount)
            {
                _architecture.SendCommand(new InvalidateAimCommand(_context,Time.frameCount));
                return;
            }
            _lastEvaluationFrame=Time.frameCount;
            bool aiming=_focused && _input.IsAiming.Value;
            float dt=Time.deltaTime;
            _aimWeight=Mathf.MoveTowards(_aimWeight,aiming?1:0,dt/_profile.BlendDuration);
            int priority=aiming?15:0;
            if (_cameraPriority!=priority)
            {
                _aimCamera.Priority=priority;
                _aimCamera.MoveToTopOfPrioritySubqueue();
                _cameraPriority=priority;
            }
            _brain.ManualUpdate();
            CameraFrame=Time.frameCount;

            Vector3 localVelocity=_motor.LocalVelocity;
            Vector3 direction=localVelocity.sqrMagnitude>0.0001f?localVelocity.normalized:Vector3.zero;
            var desiredMotion=new Vector2(direction.x,direction.z)*Mathf.Clamp01(_motor.PlanarSpeed/_motor.WalkSpeed);
            _motionBlend=Vector2.SmoothDamp(_motionBlend,desiredMotion,ref _motionBlendVelocity,_profile.MotionBlendTime,Mathf.Infinity,dt);
            _gaitBlend=Mathf.SmoothDamp(_gaitBlend,Mathf.InverseLerp(_motor.WalkSpeed,_motor.RunSpeed,_motor.PlanarSpeed),ref _gaitVelocity,_profile.MotionBlendTime,Mathf.Infinity,dt);
            if(aiming && !_motor.SprintDirectionAllowed){_gaitBlend=0;_gaitVelocity=0;}
            _animation.SetFloat("Gait",_gaitBlend);
            _animation.SetFloat("Speed",_motor.PlanarSpeed);
            _animation.SetFloat("MoveX",_motionBlend.x);
            _animation.SetFloat("MoveY",_motionBlend.y);
            _animation.SetFloat("MotionSpeed",1);
            _animation.SetBool("IsAiming",aiming);
            _animation.SetBool("Grounded",_motor.IsGrounded);
            if(_motor.JumpTriggered)_jumpInProgress=true;
            float riseSpeed=Mathf.Max(_motor.JumpTakeoffSpeed,0.001f);
            float airPhase;
            if(_motor.IsGrounded){airPhase=_profile.JumpLandingPhase;_jumpInProgress=false;}
            else if(_jumpInProgress && _motor.VerticalVelocity>=0)
                airPhase=Mathf.Lerp(_profile.JumpTakeoffPhase,_profile.JumpApexPhase,1-Mathf.Clamp01(_motor.VerticalVelocity/riseSpeed));
            else airPhase=Mathf.Lerp(_jumpInProgress?_profile.JumpApexPhase:_profile.FallStartPhase,
                _profile.JumpLandingPhase,Mathf.Clamp01(-_motor.VerticalVelocity/riseSpeed));
            _animation.SetFloat("AirPhase",airPhase);
            _animation.SetFloat("VerticalVelocity",_motor.VerticalVelocity);
            if(_motor.JumpTriggered)_animation.CrossFadeInFixedTime(JumpStartState,_profile.JumpBlendDuration,0,0);
            _animation.SetBool("FreeFall",_motor.FreeFall);
            _animation.SetFloat("AimMotionRate",ComputeMotionRate(direction,_motor.PlanarSpeed));
            _turnBlend=Mathf.SmoothDamp(_turnBlend,_motor.PlanarSpeed<0.05f?_motor.TurnRate:0,
                ref _turnVelocity,_profile.MotionBlendTime,Mathf.Infinity,dt);
            _animation.SetFloat("Turn",_turnBlend);
            _animation.SetFloat("TurnSpeed",Mathf.Max(0.1f,Mathf.Abs(_turnBlend)/90f));
            _animation.SetLayerWeight(1,_aimWeight);
            float residual=Mathf.Clamp(Mathf.DeltaAngle(transform.eulerAngles.y,_motor.OrbitYaw),-35f,35f);
            for(int i=0;i<3;i++)
                _torsoTargets[i].rotation=transform.rotation*
                    Quaternion.Euler(_motor.OrbitPitch*_profile.PitchFraction(i),residual*_profile.YawFraction(i),0)*
                    _profile.NeutralSpine(i);
            _torsoRig.weight=_aimWeight*_profile.TorsoAssistWeight;
            _handsRig.weight=0;
            _rigBuilder.SyncLayers();

            // Sample the unmodified pose first. The second pass uses the same humanoid stream, at zero delta time.
            _graph.Evaluate(dt);
            var baseRightPosition=_rightHand.position;
            var baseRightRotation=_rightHand.rotation;
            var baseLeftPosition=_leftHand.position;
            var baseLeftRotation=_leftHand.rotation;
            var baseRightElbow=_rightElbow.position;
            var baseLeftElbow=_leftElbow.position;

            var viewRotation=Quaternion.Euler(_motor.OrbitPitch,_motor.OrbitYaw,0);
            var shoulder=(_rightUpperArm.position+_leftUpperArm.position)*0.5f;
            var desiredPivot=shoulder+viewRotation*_profile.GripOffset;
            var lowerPosition=baseRightPosition;
            var lowerRotation=baseRightRotation;
            bool visualAim=aiming || _aimWeight>0;
            bool transition=!aiming || _aimWeight<0.9999f || _motor.EntryProgress<0.9999f;
            var frame=new AimFrameInput(_context,Time.frameCount,visualAim,transition,
                _renderCamera.transform.position,_renderCamera.transform.forward,_renderCamera.transform.up,
                desiredPivot,_muzzleOffset,_muzzleRotation,_profile.FarDistance,_profile.CameraMask,_profile.MuzzleMask);
            _architecture.SendCommand(new PrepareAimFrameCommand(frame));
            var pose=_architecture.SendQuery(new AimPoseQuery(_context,Time.frameCount));
            if (pose.IsValid)
                _weapon.SetPositionAndRotation(Vector3.Lerp(lowerPosition,pose.PivotPosition,_aimWeight),
                    Quaternion.Slerp(lowerRotation,pose.PivotRotation,_aimWeight));
            else _weapon.SetPositionAndRotation(lowerPosition,lowerRotation);
            _rightTarget.SetPositionAndRotation(_rightGrip.position,_rightGrip.rotation);
            _leftTarget.SetPositionAndRotation(Vector3.Lerp(baseLeftPosition,_leftGrip.position,_aimWeight),
                Quaternion.Slerp(baseLeftRotation,_leftGrip.rotation,_aimWeight));
            _leftHint.position=Vector3.Lerp(baseLeftElbow,transform.TransformPoint(_profile.LeftElbow),_aimWeight);
            _rightHint.position=Vector3.Lerp(baseRightElbow,transform.TransformPoint(_profile.RightElbow),_aimWeight);
            AnimationTimeBeforeRig=_animation.GetCurrentAnimatorStateInfo(0).normalizedTime;
            _torsoRig.weight=_aimWeight*_profile.TorsoAssistWeight;
            _handsRig.weight=1;
            _rigBuilder.SyncLayers();
            _graph.Evaluate(0);
            AnimationTimeAfterRig=_animation.GetCurrentAnimatorStateInfo(0).normalizedTime;
            EvaluationCount++;
            PoseFrame=Time.frameCount;
            if (!_focused) _architecture.SendCommand(new InvalidateAimCommand(_context,Time.frameCount));
            else
            {
                bool reachable=transition || (( _rightHand.position-_rightGrip.position).sqrMagnitude<=0.0001f &&
                    (_leftHand.position-_leftGrip.position).sqrMagnitude<=0.0001f &&
                    Quaternion.Angle(_rightHand.rotation,_rightGrip.rotation)<=2f && Quaternion.Angle(_leftHand.rotation,_leftGrip.rotation)<=2f);
                _architecture.SendCommand(new CompleteAimFrameCommand(_context,Time.frameCount,_muzzle.position,_muzzle.forward,reachable));
            }
        }

        private float ComputeMotionRate(Vector3 direction,float speed)
        {
            if(speed<0.05f)return 1;
            float lateral=Mathf.Abs(direction.x);
            float forward=Mathf.Abs(direction.z);
            float total=Mathf.Max(lateral+forward,0.0001f);
            float a=lateral/total, b=forward/total;
            float nativeForward=direction.z>=0 ? _profile.ForwardWalkSpeed : _profile.BackWalkSpeed;
            float strideSpeed=Mathf.Sqrt(a*a*_profile.SideWalkSpeed*_profile.SideWalkSpeed+b*b*nativeForward*nativeForward);
            return speed/Mathf.Max(Mathf.Lerp(strideSpeed,_profile.RunStrideSpeed,_gaitBlend),0.1f);
        }

        private void OnApplicationFocus(bool focused)
        {
            _focused=focused;
            if(!focused && ContextAlive)
                _architecture.SendCommand(new InvalidateAimCommand(_context,Time.frameCount));
        }

        public void ReleaseRuntimeResources() { Release(); }
        private void OnApplicationQuit() { Release(); }
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }

        private void Release()
        {
            if(!_initialized && !_graph.IsValid())return;
            if(ContextAlive)_architecture.SendCommand(new InvalidateAimCommand(_context,Time.frameCount,true));
            if(_rigBuilder!=null)_rigBuilder.Clear();
            if(_graph.IsValid())_graph.Destroy();
            if(_brain!=null)
            {
                _brain.m_UpdateMethod=_oldCameraUpdate;
                _brain.m_BlendUpdateMethod=_oldBlendUpdate;
            }
            if(_aimCamera!=null)
            {
                _aimCamera.Priority=0;
                _aimCamera.MoveToTopOfPrioritySubqueue();
            }
            if(_animator!=null)
            {
                _animator.cullingMode=_oldCulling;
                _animator.runtimeAnimatorController=_controller;
            }
            _initialized=false;
            PoseFrame=CameraFrame=-1;
            _lastEvaluationFrame=-1;
            _context=Guid.Empty;
            _aimWeight=0;
        }
    }
}