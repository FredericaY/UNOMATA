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

        protected override void OnInit()
        {
            _playerModel = this.GetModel<PlayerModel>();
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
    }
}
