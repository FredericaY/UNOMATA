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

            // 发送命令
            this.SendCommand<QFTestCommand>();
        }

        void OnTestEvent(QFTestEvent e)
        {
            Debug.Log("[QF验证通过] " + e.Message);
        }
    }
}
