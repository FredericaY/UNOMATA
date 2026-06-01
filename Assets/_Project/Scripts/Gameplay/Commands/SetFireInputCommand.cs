using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 设置射击输入命令（骨架）。B2a 阶段填充实际射击逻辑。
    /// </summary>
    public class SetFireInputCommand : AbstractCommand
    {
        private readonly bool _fire;

        public SetFireInputCommand(bool fire) { _fire = fire; }

        protected override void OnExecute()
        {
            this.GetModel<PlayerInputModel>().Fire.Value = _fire;
            // TODO B2a: 在此处添加射击逻辑
        }
    }
}
