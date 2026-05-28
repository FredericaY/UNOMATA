using UnityEngine;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 玩家动画事件接收器（MonoBehaviour）。
    ///
    /// 职责：吞掉 CombatGirls RifleGirl 系列 fbx 内嵌的 SwitchSocket AnimationEvent，
    /// 避免 Console 红色错误 spam。
    ///
    /// 注：脚步音/落地音触发已迁移至 <see cref="AudioBridge"/> 的 Update 相位驱动方案，
    /// 不再依赖 AnimationEvent，故 OnFootstep/OnLand 方法已移除。
    ///
    /// B1b.2 / Phase 4 持枪 IK 接入时，将在 SwitchSocket 中实现真实挂点切换。
    /// </summary>
    public class PlayerAnimEventReceiver : MonoBehaviour
    {
        /// <summary>
        /// 接收 RifleGirl 动画的 SwitchSocket 事件。当前为占位空实现。
        /// </summary>
        /// <param name="slot">挂点描述字符串（如 "To_Hand_R_Socket"），当前忽略。</param>
        public void SwitchSocket(string slot)
        {
            // placeholder — B1b.2 / Phase 4 持枪 IK 接入时填充实际逻辑
        }
    }
}
