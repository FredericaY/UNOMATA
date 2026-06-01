using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 设置冲刺输入命令。将冲刺状态写入 PlayerInputModel.Sprint。
    /// </summary>
    public class SetSprintInputCommand : AbstractCommand
    {
        private readonly bool _sprint;

        public SetSprintInputCommand(bool sprint) { _sprint = sprint; }

        protected override void OnExecute()
        {
            this.GetModel<PlayerInputModel>().Sprint.Value = _sprint;
        }
    }
}
