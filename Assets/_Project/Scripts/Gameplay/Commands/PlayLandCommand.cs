using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 落地音播放指令（QFramework Command 层）。
    ///
    /// 当前 AudioBridge 采用 Update 状态检测方案，落地音直接在
    /// <see cref="AudioBridge"/> 内部触发，不经本 Command。
    /// 本 Command 保留供外部显式调用（如未来 AI 落地音接入）。
    /// 路由到 <see cref="AudioSystem.PlayLand(Vector3)"/>。
    /// </summary>
    public class PlayLandCommand : AbstractCommand
    {
        private readonly Vector3 _pos;

        public PlayLandCommand(Vector3 pos)
        {
            _pos = pos;
        }

        protected override void OnExecute()
        {
            this.GetSystem<AudioSystem>().PlayLand(_pos);
        }
    }
}
