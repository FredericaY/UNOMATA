using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unomata.Gameplay
{
    /// <summary>
    /// StarterAssetsInputs 适配器。
    /// LateUpdate 单向将 PlayerInputModel 的值写入 StarterAssetsInputs，
    /// 使 ThirdPersonController 无感知地继续读取 SA 字段。
    ///
    /// PlayerInput.Behavior = InvokeCSharpEvents 下，SendMessages 路由失效，
    /// Look Action 需在此处手动订阅并直接 pass-through 到 _sai.look。
    /// Look 不进入 PlayerInputModel（纯相机控制，无业务语义）。
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class SAInputAdapter : MonoBehaviour
    {
        private PlayerInputModel _model;
        private StarterAssetsInputs _sai;
        private InputAction _lookAction;

        private void Start()
        {
            _model = GameApp.Interface.GetModel<PlayerInputModel>();
            _sai   = GetComponent<StarterAssetsInputs>();

            var pi = GetComponent<PlayerInput>();
            if (pi != null)
            {
                _lookAction = pi.actions.FindAction("Look");
                if (_lookAction != null)
                {
                    _lookAction.performed += OnLook;
                    _lookAction.canceled  += OnLook;
                }
            }
        }

        private void OnDestroy()
        {
            if (_lookAction != null)
            {
                _lookAction.performed -= OnLook;
                _lookAction.canceled  -= OnLook;
            }
        }

        private void OnLook(InputAction.CallbackContext ctx)
        {
            if (_sai == null) return;
            _sai.look = ctx.performed ? ctx.ReadValue<Vector2>() : Vector2.zero;
        }

        private void LateUpdate()
        {
            if (_model == null || _sai == null) return;
            _sai.move   = _model.Move.Value;
            _sai.jump   = _model.Jump.Value;
            _sai.sprint = _model.Sprint.Value;
        }
    }
}
