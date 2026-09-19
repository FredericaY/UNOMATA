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
    /// 当前固定步枪由 PlayerAimPresentation 唯一控制；兼容入口不得重挂武器。
    /// </summary>
    public class PlayerAnimEventReceiver : MonoBehaviour
    {
        /// <summary>
        /// 接收 RifleGirl 动画的 SwitchSocket 事件。旧素材兼容的明确无操作入口；正式项目 clip 已清理事件。
        /// </summary>
        /// <param name="slot">挂点描述字符串（如 "To_Hand_R_Socket"），当前忽略。</param>
        public void SwitchSocket(string slot)
        {
            // Intentionally ignored: legacy events may never take ownership from PlayerAimPresentation.
        }
    }
}
