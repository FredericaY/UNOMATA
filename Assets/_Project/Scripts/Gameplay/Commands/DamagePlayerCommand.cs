using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 对玩家造成伤害指令，由敌人 AI (B3b) 或其他伤害来源调用。
    /// TODO (B3b): 填充实现——调用 this.GetSystem&lt;PlayerSystem&gt;().TakeDamage(damage)。
    /// </summary>
    public class DamagePlayerCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            // TODO (B3b): 填充：调用 PlayerSystem.TakeDamage(damage)
        }
    }
}
