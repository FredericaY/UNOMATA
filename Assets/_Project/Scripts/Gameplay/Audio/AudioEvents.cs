using UnityEngine;

namespace Unomata.Gameplay
{
    // ── 脚步音事件 ────────────────────────────────────────────────
    /// <summary>脚步音播放请求，由 <see cref="AudioSystem"/> 广播，<see cref="AudioBridge"/> 接收出声。</summary>
    public struct FootstepPlayedEvent
    {
        public Vector3 Position;
    }

    // ── 落地音事件 ────────────────────────────────────────────────
    /// <summary>落地音播放请求，由 <see cref="AudioSystem"/> 广播，<see cref="AudioBridge"/> 接收出声。</summary>
    public struct LandPlayedEvent
    {
        public Vector3 Position;
    }

    // ── 通用音效事件（预留）──────────────────────────────────────
    /// <summary>
    /// 通用音效播放请求，预留给 B2a/B2b/Phase3 的枪声、命中音、UI 音。
    /// 由 <see cref="AudioSystem.Play(SoundId, Vector3)"/> 广播。
    /// </summary>
    public struct SoundPlayedEvent
    {
        public SoundId Id;
        public Vector3 Position;
    }
}
