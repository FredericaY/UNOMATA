using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 脚步音播放指令（QFramework Command 层）。
    ///
    /// 当前 AudioBridge 采用 Update 相位驱动方案，脚步音直接在
    /// <see cref="AudioBridge"/> 内部触发，不经本 Command。
    /// 本 Command 保留供外部显式调用（如未来 AI 脚步音接入）。
    /// 路由到 <see cref="AudioSystem.PlayFootstep(Vector3)"/>。
    /// </summary>
    public class PlayFootstepCommand : AbstractCommand
    {
        private readonly Vector3 _pos;

        public PlayFootstepCommand(Vector3 pos)
        {
            _pos = pos;
        }

        protected override void OnExecute()
        {
            this.GetSystem<AudioSystem>().PlayFootstep(_pos);
        }
    }
}
