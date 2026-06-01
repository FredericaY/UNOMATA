using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 设置玩家瞄准状态命令。
    /// 由 TempAimInputDriver（B1b.2）触发，B1b.3 后改由 PlayerController 触发。
    /// </summary>
    public class SetAimStateCommand : AbstractCommand
    {
        private readonly bool _isAiming;

        public SetAimStateCommand(bool isAiming)
        {
            _isAiming = isAiming;
        }

        protected override void OnExecute()
        {
            this.GetSystem<PlayerSystem>().SetAiming(_isAiming);
        }
    }
}
