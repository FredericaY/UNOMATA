using UnityEngine;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 音效 ID 枚举，用于统一的 <see cref="AudioSystem.Play(SoundId, Vector3)"/> 接口。
    ///
    /// 已实装：Footstep / Land / GunShot / HitSurface / HitEnemy。
    /// UIClick 为后续 UI 保留。
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
