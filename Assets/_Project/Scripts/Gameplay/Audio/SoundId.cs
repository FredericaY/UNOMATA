using UnityEngine;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 音效 ID 枚举，用于统一的 <see cref="AudioSystem.Play(SoundId, Vector3)"/> 接口。
    ///
    /// 本期实装：Footstep / Land。
    /// 预留（B2a/B2b/Phase3 接入）：GunShot / HitSurface / HitEnemy / UIClick。
    /// </summary>
    public enum SoundId
    {
        Footstep,
        Land,
        GunShot,
        HitSurface,
        HitEnemy,
        UIClick,
    }
}
