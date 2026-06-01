namespace Unomata.Gameplay
{
    /// <summary>
    /// 瞄准状态切换事件（QFramework 广播事件）。
    /// 由 PlayerSystem.SetAiming 触发，AnimatorAimBridge / CameraAimBridge 订阅。
    /// </summary>
    public struct AimStateChangedEvent
    {
        /// <summary>true = 进入瞄准，false = 退出瞄准。</summary>
        public bool IsAiming;
    }
}
