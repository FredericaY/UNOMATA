/**
 * QFramework 可用性验证脚本（Phase 0 验证用）
 * 验证通过后可保留作为参考，也可删除。
 *
 * 验证链路：
 *   Start() → SendCommand<QFTestCommand>
 *   → QFTestCommand.OnExecute() → SendEvent<QFTestEvent>
 *   → 监听方收到事件 → Console 输出 [QF验证通过]
 *
 * 使用方法：新建 GameObject，挂载此脚本，进入 Play Mode，
 *           观察 Console 输出 "[QF验证通过]" 即为成功。
 */
using UnityEngine;
using QFramework;

namespace Unomata.Gameplay.Tests
{
    // ── 测试 Event ──────────────────────────────────────
    public struct QFTestEvent
    {
        public string Message;
    }

    // ── 测试 Command ─────────────────────────────────────
    public class QFTestCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            Debug.Log("[QFrameworkValidator] Command 执行中...");
            this.SendEvent(new QFTestEvent { Message = "Command→System→Event 链路正常" });
        }
    }

    // ── 验证 Controller ──────────────────────────────────
    public class QFrameworkValidator : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;

        void Start()
        {
            Debug.Log("[QFrameworkValidator] 开始验证 QFramework IOC/事件机制...");

            // 订阅事件
            this.RegisterEvent<QFTestEvent>(OnTestEvent).UnRegisterWhenGameObjectDestroyed(gameObject);

            // 发送命令（Phase 0 链路验证）
            this.SendCommand<QFTestCommand>();

            // Phase 2 骨架链路验证
            ValidatePhase2Skeleton();
        }

        void OnTestEvent(QFTestEvent e)
        {
            Debug.Log("[QF验证通过] " + e.Message);
        }

        // ── Phase 2 骨架验证 ─────────────────────────────
        void ValidatePhase2Skeleton()
        {
            bool pass = true;

            // 1. PlayerModel HP 初始值验证
            var playerModel = this.GetModel<PlayerModel>();
            if (playerModel == null) { Debug.LogError("[QF验证失败] PlayerModel 未注册"); pass = false; }
            else if (playerModel.HP.Value != 100f) { Debug.LogError($"[QF验证失败] PlayerModel.HP 初始值应为 100，实际为 {playerModel.HP.Value}"); pass = false; }

            // 2. PlayerSystem.TakeDamage 扣血验证
            var playerSystem = this.GetSystem<PlayerSystem>();
            if (playerSystem == null) { Debug.LogError("[QF验证失败] PlayerSystem 未注册"); pass = false; }
            else
            {
                playerSystem.TakeDamage(30f);
                if (playerModel != null && playerModel.HP.Value != 70f)
                { Debug.LogError($"[QF验证失败] TakeDamage(30) 后 HP 应为 70，实际为 {playerModel.HP.Value}"); pass = false; }

                // 3. HP 下限不低于零
                playerSystem.TakeDamage(200f);
                if (playerModel != null && playerModel.HP.Value != 0f)
                { Debug.LogError($"[QF验证失败] TakeDamage(200) 后 HP 应为 0，实际为 {playerModel.HP.Value}"); pass = false; }

                // 恢复初始值，避免影响游戏运行
                if (playerModel != null) playerModel.HP.Value = playerModel.MaxHp.Value;
            }

            // 4. WaveSystem 可获取 WaveModel
            var waveSystem = this.GetSystem<WaveSystem>();
            if (waveSystem == null) { Debug.LogError("[QF验证失败] WaveSystem 未注册"); pass = false; }

            var waveModel = this.GetModel<WaveModel>();
            if (waveModel == null) { Debug.LogError("[QF验证失败] WaveModel 未注册"); pass = false; }

            if (pass)
                Debug.Log("[QF验证通过] Phase2 骨架 System/Model 链路正常");
        }
    }
}
