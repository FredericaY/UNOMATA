using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 音频资产数据模型（QFramework Model 层）。
    ///
    /// 仅持有音频资产引用与音量参数，不包含任何播放逻辑。
    /// 字段由 <see cref="AudioBridge"/> 在 Awake() 阶段注入（Inspector 引用 → Model）。
    ///
    /// 预留 MasterVolume 供 Phase 3 UI 音量滑块绑定。
    /// </summary>
    public class AudioModel : AbstractModel
    {
        /// <summary>脚步音 AudioClip 池，由 AudioBridge 从 Inspector 注入。</summary>
        public AudioClip[] FootstepClips;

        /// <summary>落地音 AudioClip，由 AudioBridge 从 Inspector 注入。</summary>
        public AudioClip LandingClip;

        /// <summary>主音量（0~1），预留给 Phase 3 音量 UI。默认 1f。</summary>
        public BindableProperty<float> MasterVolume = new BindableProperty<float>(1f);

        protected override void OnInit()
        {
            // 字段由 AudioBridge.Awake() 运行时注入，此处无需初始化。
        }
    }
}
