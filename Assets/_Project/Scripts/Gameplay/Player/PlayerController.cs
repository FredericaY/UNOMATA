using UnityEngine;
using UnityEngine.InputSystem;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>Owns business input subscriptions; writes input state only through commands.</summary>
    [DefaultExecutionOrder(-20)]
    public class PlayerController : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;
        [SerializeField] private PlayerInput _playerInput;

        private IArchitecture _architecture;
        private PlayerInputModel _model;
        private PlayerInputBinding _binding;
        private PlayerInput _attemptedPlayer;
        private InputActionAsset _attemptedAsset;
        private bool _started, _focused = true, _validationFailed;
        private bool _jumpArmed, _fireArmed;

        private void Awake() { _focused = Application.isFocused; }

        private void OnEnable()
        {
            _validationFailed = false;
            InputSystem.onActionChange += OnActionChange;
            // Bind from Start/Update so state is sampled in the player input update context.
        }

        private void Start()
        {
            _started = true;
            MaintainBinding();
        }

        private void Update()
        {
            MaintainBinding();
            if (_binding == null) return;
            if (!PlayerInputBinding.IsHeld(_binding.Jump)) _jumpArmed = true;
            if (!PlayerInputBinding.IsHeld(_binding.Fire)) _fireArmed = true;
        }

        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused) Suspend();
            // On regain, Update samples fresh game input rather than editor input buffers.
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
            Suspend();
        }

        private void OnDestroy() { Suspend(); }

        private bool ContextIsAlive =>
            _architecture != null && _model != null &&
            ReferenceEquals(_architecture.GetModel<PlayerInputModel>(), _model);

        private void MaintainBinding()
        {
            if (!_started || !isActiveAndEnabled || !_focused) { Suspend(); return; }
            if (_binding != null)
            {
                if (_binding.IsCurrent(_playerInput) && ContextIsAlive) return;
                Suspend();
            }

            var asset = _playerInput != null ? _playerInput.actions : null;
            if (_validationFailed && _attemptedPlayer == _playerInput && _attemptedAsset == asset) return;
            _attemptedPlayer = _playerInput;
            _attemptedAsset = asset;
            if (!PlayerInputBinding.TryResolve(_playerInput, out var next, out var error))
            {
                _validationFailed = true;
                ResetInput();
                Debug.LogError("[PlayerController] Cannot bind '" + name + "': " + error, this);
                return;
            }
            _validationFailed = false;
            if (!next.IsCurrent(_playerInput)) return;

            _architecture = GameApp.Interface;
            _model = _architecture.GetModel<PlayerInputModel>();
            _binding = next;
            _jumpArmed = !PlayerInputBinding.IsHeld(next.Jump);
            _fireArmed = !PlayerInputBinding.IsHeld(next.Fire);
            next.Move.performed += OnMove; next.Move.canceled += OnMove;
            next.Jump.performed += OnJump; next.Jump.canceled += OnJump;
            next.Sprint.performed += OnSprint; next.Sprint.canceled += OnSprint;
            next.Aim.performed += OnAim; next.Aim.canceled += OnAim;
            next.Fire.performed += OnFire; next.Fire.canceled += OnFire;
            _architecture.SendCommand(new SetMoveInputCommand(next.Move.ReadValue<Vector2>()));
            _architecture.SendCommand(new SetSprintInputCommand(next.Sprint.IsPressed()));
            _architecture.SendCommand(new SetAimStateCommand(next.Aim.IsPressed()));
            _architecture.SendCommand(new SetJumpInputCommand(false));
            _architecture.SendCommand(new SetFireInputCommand(false));
        }

        private void OnActionChange(object changed, InputActionChange change)
        {
            if (_binding == null) return; // Update handles activation after a suspension.
            if (!_binding.Owns(changed)) return;
            if (change == InputActionChange.ActionDisabled || change == InputActionChange.ActionMapDisabled ||
                change == InputActionChange.BoundControlsAboutToChange)
                Suspend();
        }

        private void Suspend()
        {
            var old = _binding;
            _binding = null; // Canceled/cleanup callbacks cannot write after this point.
            if (old != null)
            {
                old.Move.performed -= OnMove; old.Move.canceled -= OnMove;
                old.Jump.performed -= OnJump; old.Jump.canceled -= OnJump;
                old.Sprint.performed -= OnSprint; old.Sprint.canceled -= OnSprint;
                old.Aim.performed -= OnAim; old.Aim.canceled -= OnAim;
                old.Fire.performed -= OnFire; old.Fire.canceled -= OnFire;
                ResetInput();
            }
            _jumpArmed = _fireArmed = false;
        }

        private void ResetInput()
        {
            // Never consult the lazy GameApp.Interface during teardown.
            if (ContextIsAlive) _architecture.SendCommand(new ResetPlayerInputCommand());
        }

        private bool CanReceive()
        {
            if (_binding != null && _focused && isActiveAndEnabled &&
                _binding.IsCurrent(_playerInput) && ContextIsAlive) return true;
            Suspend();
            return false;
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            if (CanReceive())
                _architecture.SendCommand(new SetMoveInputCommand(ctx.canceled ? Vector2.zero : ctx.ReadValue<Vector2>()));
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (!CanReceive()) return;
            bool pressed = !ctx.canceled && ctx.ReadValueAsButton();
            if (!pressed) _jumpArmed = true;
            _architecture.SendCommand(new SetJumpInputCommand(pressed && _jumpArmed));
        }

        private void OnSprint(InputAction.CallbackContext ctx)
        {
            if (CanReceive())
                _architecture.SendCommand(new SetSprintInputCommand(!ctx.canceled && ctx.ReadValueAsButton()));
        }

        private void OnAim(InputAction.CallbackContext ctx)
        {
            if (CanReceive())
                _architecture.SendCommand(new SetAimStateCommand(!ctx.canceled && ctx.ReadValueAsButton()));
        }

        private void OnFire(InputAction.CallbackContext ctx)
        {
            if (!CanReceive()) return;
            bool pressed = !ctx.canceled && ctx.ReadValueAsButton();
            if (!pressed) _fireArmed = true;
            _architecture.SendCommand(new SetFireInputCommand(pressed && _fireArmed));
        }
    }
}
