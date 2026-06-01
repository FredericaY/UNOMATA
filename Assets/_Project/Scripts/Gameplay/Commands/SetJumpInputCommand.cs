using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 设置跳跃输入命令。
    /// 将跳跃输入状态写入 PlayerInputModel.Jump。
    /// </summary>
    public class SetJumpInputCommand : AbstractCommand
    {
        private readonly bool _jump;

        public SetJumpInputCommand(bool jump)
        {
            _jump = jump;
        }

        protected override void OnExecute()
        {
            this.GetModel<PlayerInputModel>().Jump.Value = _jump;
        }
    }
}
