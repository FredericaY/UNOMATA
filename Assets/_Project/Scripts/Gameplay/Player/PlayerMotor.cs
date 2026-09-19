using QFramework;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unomata.Gameplay
{
    /// <summary>
    /// Project adaptation of the bundled StarterAssets ThirdPersonController.
    /// Owns scene movement/orbit/facing; business input remains owned by PlayerInputModel.
    /// Gravity, jump timeout and capsule movement preserve the vendor baseline.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [RequireComponent(typeof(CharacterController), typeof(StarterAssetsInputs), typeof(PlayerInput))]
    public sealed class PlayerMotor : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;
        [SerializeField] private Transform _cameraRoot;
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _sprintSpeed = 5.335f;
        [SerializeField] private float _rotationSmoothTime = 0.12f;
        [SerializeField] private float _speedChangeRate = 10f;
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -15f;
        [SerializeField] private float _jumpTimeout = 0.5f;
        [SerializeField] private float _fallTimeout = 0.15f;
        [SerializeField] private float _groundedOffset = -0.14f;
        [SerializeField] private float _groundedRadius = 0.28f;
        [SerializeField] private LayerMask _groundLayers = 1;
        [SerializeField] private float _topClamp = 70f;
        [SerializeField] private float _bottomClamp = -30f;
        [SerializeField] private float _turnStart = 15f;
        [SerializeField] private float _turnStop = 5f;
        [SerializeField] private float _turnSpeed = 360f;
        [SerializeField] private float _entryDuration = 0.3f;

        private readonly SprintDirectionQuery _sprintDirectionQuery=new SprintDirectionQuery();
        public bool SprintDirectionAllowed { get; private set; }
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private PlayerInput _playerInput;
        private PlayerInputModel _model;
        private IArchitecture _architecture;
        private float _speed, _rotationVelocity, _verticalVelocity, _jumpTimer, _fallTimer;
        private float _moveYaw, _entryStartYaw, _entryTargetYaw, _entryElapsed;
        private bool _grounded = true, _wasAiming, _turning, _started, _wasOnGround = true;
        private int _lastFrame = -1;

        public float WalkSpeed=>_moveSpeed;
        public float RunSpeed=>_sprintSpeed;
        public Vector3 LocalVelocity { get; private set; }
        public float PlanarSpeed { get; private set; }
        public float OrbitYaw { get; private set; }
        public float OrbitPitch { get; private set; }
        public bool IsGrounded => _grounded && _verticalVelocity <= 0;
        public bool JumpTriggered { get; private set; }
        public bool FreeFall => !IsGrounded && _fallTimer <= 0;
        public float VerticalVelocity => _verticalVelocity;
        public float JumpTakeoffSpeed => Mathf.Sqrt(_jumpHeight*-2f*_gravity);
        public float TurnRate { get; private set; }
        public int LandingSequence { get; private set; }
        public int MovementFrame { get; private set; } = -1;
        public float EntryProgress => _entryDuration <= 0 ? 1 : Mathf.Clamp01(_entryElapsed / _entryDuration);

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            if (_cameraRoot == null)
            {
                Debug.LogError("[PlayerMotor] Missing camera root.", this);
                enabled = false; return;
            }
            _architecture = GameApp.Interface;
            _model = _architecture.GetModel<PlayerInputModel>();
            OrbitYaw = _cameraRoot.eulerAngles.y;
            OrbitPitch = Mathf.DeltaAngle(0, _cameraRoot.eulerAngles.x);
            _moveYaw = transform.eulerAngles.y;
            _jumpTimer = _jumpTimeout;
            _fallTimer = _fallTimeout;
            _started = true;
        }

        private void Update()
        {
            if (!_started || _lastFrame == Time.frameCount) return;
            if (_architecture == null || !ReferenceEquals(_architecture.GetModel<PlayerInputModel>(),_model))
            { _architecture=GameApp.Interface; _model=_architecture.GetModel<PlayerInputModel>(); }
            _lastFrame = Time.frameCount;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            UpdateOrbit(dt);
            JumpAndGravity(dt);
            _grounded = Physics.CheckSphere(transform.position + Vector3.up * -_groundedOffset,
                _groundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);
            Move(dt);
            _cameraRoot.rotation = Quaternion.Euler(OrbitPitch, OrbitYaw, 0);
            bool onGround = IsGrounded;
            if (onGround && !_wasOnGround) LandingSequence++;
            _wasOnGround = onGround;
            MovementFrame = Time.frameCount;
        }

        private void UpdateOrbit(float dt)
        {
            float multiplier = _playerInput.currentControlScheme == "KeyboardMouse" ? 1f : dt;
            OrbitYaw = Mathf.Repeat(OrbitYaw + _input.look.x * multiplier, 360);
            OrbitPitch = Mathf.Clamp(OrbitPitch + _input.look.y * multiplier, _bottomClamp, _topClamp);
        }

        private void Move(float dt)
        {
            Vector2 input = Vector2.ClampMagnitude(_input.move, 1);
            bool moving = input.sqrMagnitude > 0.0001f;
            bool aiming = _model.IsAiming.Value;
            SprintDirectionAllowed=_architecture.SendQuery(_sprintDirectionQuery);
            float targetSpeed = moving ? (_input.sprint && SprintDirectionAllowed ? _sprintSpeed : _moveSpeed) : 0;
            float inputMagnitude = _input.analogMovement ? input.magnitude : 1;
            float currentSpeed = Vector3.ProjectOnPlane(_controller.velocity, Vector3.up).magnitude;
            if (Mathf.Abs(currentSpeed-targetSpeed) > 0.1f)
                _speed = Mathf.Round(Mathf.Lerp(currentSpeed,targetSpeed*inputMagnitude,dt*_speedChangeRate)*1000)/1000;
            else _speed = targetSpeed;
            if(aiming && !SprintDirectionAllowed)_speed=Mathf.Min(_speed,_moveSpeed*inputMagnitude);
            if (moving) _moveYaw = Mathf.Atan2(input.x,input.y)*Mathf.Rad2Deg + OrbitYaw;
            float bodyYaw = transform.eulerAngles.y;
            float before = bodyYaw;
            if (aiming && !_wasAiming)
            {
                _entryStartYaw = bodyYaw;
                _entryTargetYaw = bodyYaw + Mathf.DeltaAngle(bodyYaw,OrbitYaw);
                _entryElapsed = 0; _turning = false;
            }
            if (aiming)
            {
                if (_entryElapsed < _entryDuration)
                {
                    _entryElapsed = Mathf.Min(_entryElapsed+dt,_entryDuration);
                    float t = Mathf.SmoothStep(0,1,EntryProgress);
                    // Keep a continuous target while entering aim; crossing +/-180 must not reverse the interpolation.
                    _entryTargetYaw += Mathf.DeltaAngle(_entryTargetYaw,OrbitYaw);
                    bodyYaw = Mathf.LerpUnclamped(_entryStartYaw,_entryTargetYaw,t);
                }
                else if (moving) { bodyYaw=OrbitYaw; _turning=false; }
                else
                {
                    float delta=Mathf.DeltaAngle(bodyYaw,OrbitYaw);
                    if (!_turning && Mathf.Abs(delta)>_turnStart) _turning=true;
                    if (_turning)
                    {
                        bodyYaw=Mathf.MoveTowardsAngle(bodyYaw,OrbitYaw,_turnSpeed*dt);
                        if (Mathf.Abs(Mathf.DeltaAngle(bodyYaw,OrbitYaw))<=_turnStop) _turning=false;
                    }
                }
            }
            else if (moving)
                bodyYaw=Mathf.SmoothDampAngle(bodyYaw,_moveYaw,ref _rotationVelocity,_rotationSmoothTime,Mathf.Infinity,dt);
            _wasAiming=aiming;
            transform.rotation=Quaternion.Euler(0,bodyYaw,0);
            TurnRate=Mathf.DeltaAngle(before,bodyYaw)/dt;
            Vector3 direction=Quaternion.Euler(0,_moveYaw,0)*Vector3.forward;
            var beforePosition=transform.position;
            _controller.Move(direction.normalized*(_speed*dt) + Vector3.up*(_verticalVelocity*dt));
            Vector3 velocity=(transform.position-beforePosition)/dt;
            velocity.y=0;
            LocalVelocity=transform.InverseTransformDirection(velocity);
            PlanarSpeed=velocity.magnitude;
        }

        private void JumpAndGravity(float dt)
        {
            JumpTriggered=false;
            if (_grounded)
            {
                _fallTimer=_fallTimeout;
                if (_verticalVelocity<0) _verticalVelocity=-2;
                if (_input.jump && _jumpTimer<=0)
                {
                    _verticalVelocity=JumpTakeoffSpeed;
                    JumpTriggered=true;
                    _input.jump=false;
                }
                if (_jumpTimer>=0) _jumpTimer-=dt;
            }
            else
            {
                _jumpTimer=_jumpTimeout;
                if (_fallTimer>=0) _fallTimer-=dt;
                _input.jump=false;
            }
            if (_verticalVelocity<53f) _verticalVelocity+=_gravity*dt;
        }

        private void OnDisable()
        {
            _speed=0; LocalVelocity=Vector3.zero; PlanarSpeed=0; TurnRate=0;
            _turning=false; _wasAiming=false; _lastFrame=-1; SprintDirectionAllowed=false;
            MovementFrame=-1;
        }
    }
}