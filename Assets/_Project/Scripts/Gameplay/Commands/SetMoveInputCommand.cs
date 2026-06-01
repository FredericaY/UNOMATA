using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 设置移动输入命令。
    /// 将移动输入向量写入 PlayerInputModel.Move。
    /// </summary>
    public class SetMoveInputCommand : AbstractCommand
    {
        private readonly Vector2 _move;

        public SetMoveInputCommand(Vector2 move)
        {
            _move = move;
        }

        protected override void OnExecute()
        {
            this.GetModel<PlayerInputModel>().Move.Value = _move;
        }
    }
}
