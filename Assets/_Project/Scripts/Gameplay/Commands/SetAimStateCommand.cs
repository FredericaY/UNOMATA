using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 设置玩家瞄准状态命令。
    /// 写入唯一的瞄准输入状态，由 PlayerSystem 订阅后同步业务状态和事件。
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
            this.GetModel<PlayerInputModel>().IsAiming.Value = _isAiming;
        }
    }
}
