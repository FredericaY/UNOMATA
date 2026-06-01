using Cinemachine;
using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 瞄准相机桥接器（QFramework Controller 层）。
    /// 订阅 AimStateChangedEvent，通过切换 PlayerAimCamera Priority
    /// 触发 Cinemachine Brain 自动混合过渡（默认 0.3s Ease In Out）。
    /// </summary>
    public class CameraAimBridge : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;

        [SerializeField] private CinemachineVirtualCamera _aimCamera;

        private void Start()
        {
            this.RegisterEvent<AimStateChangedEvent>(OnAimStateChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnAimStateChanged(AimStateChangedEvent e)
        {
            if (_aimCamera == null) return;
            // Cinemachine 2.x: Priority 变化不会自动触发 mActiveCameras 重排。
            // 需要 disable → 改 Priority → enable，强制 CinemachineCore 重新插入排序。
            _aimCamera.enabled = false;
            _aimCamera.Priority = e.IsAiming ? 15 : 0;
            _aimCamera.enabled = true;
        }
    }
}
