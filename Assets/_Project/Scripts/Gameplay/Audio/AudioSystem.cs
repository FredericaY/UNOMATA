using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 音频业务系统（QFramework System 层）。
    ///
    /// 提供 PlayFootstep / PlayLand / Play 三个公开接口；
    /// 内部通过 <c>this.SendEvent</c> 广播对应 QF Event，
    /// 由 <see cref="AudioBridge"/>（MonoBehaviour IController）订阅后实际出声。
    ///
    /// 本 System 不持有任何 AudioSource 或 MonoBehaviour 引用，
    /// 符合 QFramework 分层规范（System 层禁持 MB 引用）。
    ///
    /// 后续接入点：
    ///   B2a/B2b 枪声/命中音 → SendCommand&lt;PlaySoundCommand&gt;(SoundId.GunShot, pos)
    ///   Phase 3 UI 音          → 同一接口
    /// </summary>
    public class AudioSystem : AbstractSystem
    {
        private AudioModel _audioModel;

        protected override void OnInit()
        {
            _audioModel = this.GetModel<AudioModel>();
        }

        // ── 脚步音 ────────────────────────────────────────────────
        /// <summary>
        /// 播放脚步音。通过 <see cref="FootstepPlayedEvent"/> 通知 AudioBridge 出声。
        /// 若 AudioModel.FootstepClips 未注入（null/空），不出声并打 Warning。
        /// </summary>
        /// <param name="pos">发声世界坐标（来自 PlayerArmature.transform.position）。</param>
        public void PlayFootstep(Vector3 pos)
        {
            if (_audioModel.FootstepClips == null || _audioModel.FootstepClips.Length == 0)
            {
                Debug.LogWarning("[AudioSystem] PlayFootstep: FootstepClips 未注入，跳过播放。");
                return;
            }
            this.SendEvent(new FootstepPlayedEvent { Position = pos });
        }

        // ── 落地音 ────────────────────────────────────────────────
        /// <summary>
        /// 播放落地音。通过 <see cref="LandPlayedEvent"/> 通知 AudioBridge 出声。
        /// 若 AudioModel.LandingClip 未注入（null），不出声并打 Warning。
        /// </summary>
        /// <param name="pos">发声世界坐标。</param>
        public void PlayLand(Vector3 pos)
        {
            if (_audioModel.LandingClip == null)
            {
                Debug.LogWarning("[AudioSystem] PlayLand: LandingClip 未注入，跳过播放。");
                return;
            }
            this.SendEvent(new LandPlayedEvent { Position = pos });
        }

        // ── 通用音效（预留）──────────────────────────────────────
        /// <summary>
        /// 通用音效播放接口，预留给枪声/命中音/UI 音。
        /// 通过 <see cref="SoundPlayedEvent"/> 广播，AudioBridge 按 SoundId 分发。
        /// </summary>
        /// <param name="id">音效类型。</param>
        /// <param name="pos">发声世界坐标。</param>
        public void Play(SoundId id, Vector3 pos)
        {
            this.SendEvent(new SoundPlayedEvent { Id = id, Position = pos });
        }
    }
}
