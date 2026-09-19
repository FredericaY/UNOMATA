using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 只更新射击输入意图；ShootingSystem 在本帧姿态完成后执行射击。
    /// </summary>
    public class SetFireInputCommand : AbstractCommand
    {
        private readonly bool _fire;

        public SetFireInputCommand(bool fire) { _fire = fire; }

        protected override void OnExecute()
        {
            this.GetModel<PlayerInputModel>().Fire.Value = _fire;
        }
    }
}
