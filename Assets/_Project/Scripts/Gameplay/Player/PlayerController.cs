using UnityEngine;
using UnityEngine.InputSystem;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 玩家输入 QF 接入点（QFramework Controller 层）。
    /// 接管 PlayerInput 的全部 Action 回调，通过 Command 写入 PlayerInputModel。
    /// 禁用此组件后角色完全无响应（输入入口唯一性验证）。
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class PlayerController : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;

        [SerializeField] private PlayerInput _playerInput;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _aimAction;
        private InputAction _fireAction;

        private void Start()
        {
            var actions = _playerInput.actions;
            _moveAction   = actions.FindAction("Move");
            _jumpAction   = actions.FindAction("Jump");
            _sprintAction = actions.FindAction("Sprint");
            _aimAction    = actions.FindAction("Aim");
            _fireAction   = actions.FindAction("Fire");

            _moveAction.performed   += OnMove;
            _moveAction.canceled    += OnMove;
            _jumpAction.performed   += OnJump;
            _jumpAction.canceled    += OnJump;
            _sprintAction.performed += OnSprint;
            _sprintAction.canceled  += OnSprint;
            _aimAction.performed    += OnAim;
            _aimAction.canceled     += OnAim;
            _fireAction.performed   += OnFire;
            _fireAction.canceled    += OnFire;
        }

        private void OnDisable()
        {
            if (_moveAction == null) return;
            _moveAction.performed   -= OnMove;
            _moveAction.canceled    -= OnMove;
            _jumpAction.performed   -= OnJump;
            _jumpAction.canceled    -= OnJump;
            _sprintAction.performed -= OnSprint;
            _sprintAction.canceled  -= OnSprint;
            _aimAction.performed    -= OnAim;
            _aimAction.canceled     -= OnAim;
            _fireAction.performed   -= OnFire;
            _fireAction.canceled    -= OnFire;
        }

        // ── 回调 ──────────────────────────────────────────────────────────

        private void OnMove(InputAction.CallbackContext ctx)
        {
            var v = ctx.performed ? ctx.ReadValue<Vector2>() : Vector2.zero;
            this.SendCommand(new SetMoveInputCommand(v));
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            this.SendCommand(new SetJumpInputCommand(ctx.performed));
        }

        private void OnSprint(InputAction.CallbackContext ctx)
        {
            this.SendCommand(new SetSprintInputCommand(ctx.performed));
        }

        private void OnAim(InputAction.CallbackContext ctx)
        {
            this.SendCommand(new SetAimStateCommand(ctx.performed));
        }

        private void OnFire(InputAction.CallbackContext ctx)
        {
            this.SendCommand(new SetFireInputCommand(ctx.performed));
        }
    }
}
