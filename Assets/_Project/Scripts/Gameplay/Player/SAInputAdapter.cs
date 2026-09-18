using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unomata.Gameplay
{
    /// <summary>Prepares the vendor input buffer before movement and owns the separate Look route.</summary>
    [DefaultExecutionOrder(-10)]
    public class SAInputAdapter : MonoBehaviour
    {
        private PlayerInputModel _model;
        private QFramework.IArchitecture _architecture;
        private StarterAssetsInputs _sai;
        private PlayerInput _playerInput;
        private InputActionAsset _asset, _attemptedAsset;
        private InputActionMap _map;
        private InputAction _look;
        private bool _started, _focused = true, _validationFailed, _jumpWasPressed;

        private void Awake()
        {
            _focused = Application.isFocused;
            _sai = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _validationFailed = false;
            InputSystem.onActionChange += OnActionChange;
            // Start/Update binds against the current player input state.
        }

        private void Start() { _started = true; MaintainBinding(); }

        private void Update()
        {
            MaintainBinding();
            if (_look == null) return;
            _sai.move = _model.Move.Value;
            _sai.sprint = _model.Sprint.Value;
            bool pressed = _model.Jump.Value;
            if (!pressed) _sai.jump = false;
            else if (!_jumpWasPressed) _sai.jump = true;
            _jumpWasPressed = pressed;
        }

        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused) Suspend();
            // Rebind in Update after focus has returned.
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
            Suspend();
        }

        private void OnDestroy() { Suspend(); }

        private bool SourceIsReady =>
            _playerInput != null && _playerInput.isActiveAndEnabled && _playerInput.inputIsActive &&
            _playerInput.actions == _asset && _playerInput.currentActionMap == _map &&
            _map != null && _map.enabled && _look != null && _look.enabled;

        private void MaintainBinding()
        {
            if (!_started || !isActiveAndEnabled || !_focused) { Suspend(); return; }
            if (_look != null)
            {
                if (SourceIsReady && _architecture != null &&
                    ReferenceEquals(_architecture.GetModel<PlayerInputModel>(), _model)) return;
                Suspend();
            }
            var asset = _playerInput != null ? _playerInput.actions : null;
            if (_validationFailed && asset == _attemptedAsset) return;
            _attemptedAsset = asset;
            var map = asset != null ? asset.FindActionMap(PlayerInputBinding.MapName) : null;
            var look = map?.FindAction("Look");
            if (_sai == null || _playerInput == null || map == null || look == null ||
                look.type != InputActionType.Value || look.expectedControlType != "Vector2")
            {
                _validationFailed = true;
                ClearBuffer();
                Debug.LogError("[SAInputAdapter] Cannot bind '" + name +
                    "': require StarterAssetsInputs, PlayerInput asset and Player/Look (Value/Vector2).", this);
                return;
            }
            _validationFailed = false;
            if (!_playerInput.isActiveAndEnabled || !_playerInput.inputIsActive ||
                _playerInput.currentActionMap != map || !map.enabled || !look.enabled) return;
            _architecture = GameApp.Interface;
            _model = _architecture.GetModel<PlayerInputModel>();
            _asset = asset; _map = map; _look = look;
            _jumpWasPressed = _model.Jump.Value; // An already held button is not a new request.
            look.performed += OnLook;
            look.canceled += OnLook;
            _sai.look = look.ReadValue<Vector2>();
        }

        private void OnActionChange(object changed, InputActionChange change)
        {
            if (_look == null) return;
            bool owns = ReferenceEquals(changed, _map) || ReferenceEquals(changed, _asset) ||
                        (changed is InputAction action && ReferenceEquals(action, _look));
            if (owns && (change == InputActionChange.ActionDisabled ||
                         change == InputActionChange.ActionMapDisabled ||
                         change == InputActionChange.BoundControlsAboutToChange))
                Suspend();
        }

        private void OnLook(InputAction.CallbackContext ctx)
        {
            if (!_focused || !isActiveAndEnabled || !SourceIsReady) { Suspend(); return; }
            _sai.look = ctx.canceled ? Vector2.zero : ctx.ReadValue<Vector2>();
        }

        private void Suspend()
        {
            if (_look != null)
            {
                _look.performed -= OnLook;
                _look.canceled -= OnLook;
            }
            _look = null; _map = null; _asset = null;
            _jumpWasPressed = false;
            ClearBuffer();
        }

        private void ClearBuffer()
        {
            if (_sai == null) return;
            _sai.move = Vector2.zero;
            _sai.look = Vector2.zero;
            _sai.jump = _sai.sprint = false;
        }
    }
}
