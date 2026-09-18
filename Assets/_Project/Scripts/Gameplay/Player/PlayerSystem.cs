using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 玩家业务逻辑系统（QFramework System 层）。
    /// 持有 PlayerModel 引用，处理受伤、回复等逻辑。
    /// </summary>
    public class PlayerSystem : AbstractSystem
    {
        private PlayerModel _playerModel;
        private PlayerInputModel _inputModel;
        private IUnRegister _aimSubscription;

        protected override void OnInit()
        {
            _playerModel = this.GetModel<PlayerModel>();
            _inputModel  = this.GetModel<PlayerInputModel>();

            // 订阅输入 Model 的 IsAiming 变化，自动触发瞄准状态切换
            _aimSubscription = _inputModel.IsAiming.Register(SetAiming);
            SetAiming(_inputModel.IsAiming.Value);
        }

        protected override void OnDeinit()
        {
            _aimSubscription?.UnRegister();
            _aimSubscription = null;
        }

        /// <summary>
        /// 对玩家造成伤害，HP 不得低于 0。
        /// Phase 4 联动时此方法将接收经 DamageReductionFactor 修正后的伤害值。
        /// </summary>
        /// <param name="raw">原始伤害量（已经过调用方计算）</param>
        public void TakeDamage(float raw)
        {
            _playerModel.HP.Value = Mathf.Max(0f, _playerModel.HP.Value - raw);
        }

        /// <summary>
        /// 设置玩家瞄准状态，写入 Model 并广播 AimStateChangedEvent。
        /// </summary>
        /// <param name="isAiming">是否进入瞄准状态</param>
        public void SetAiming(bool isAiming)
        {
            if (_playerModel.IsAiming.Value == isAiming) return;
            _playerModel.IsAiming.Value = isAiming;
            this.SendEvent(new AimStateChangedEvent { IsAiming = isAiming });
        }
    }
}
