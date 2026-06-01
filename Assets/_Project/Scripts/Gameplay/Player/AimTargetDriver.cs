using UnityEngine;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 瞄准目标驱动器。每帧把 AimTarget 置于渲染相机正前方的世界点，
    /// 供 Animation Rigging 的 Multi-Aim Constraint 链瞄准（上半身朝向准星方向）。
    ///
    /// 用实际渲染相机（Camera.main，带 CinemachineBrain）的 forward，
    /// 这样无论 PlayerFollowCamera / PlayerAimCamera 谁激活，AimTarget 始终落在玩家视线方向。
    /// 非瞄准时 Rig 权重为 0，AimTarget 更新不影响表现。
    ///
    /// DefaultExecutionOrder(-5)：在 Rig 评估前更新目标位置。
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class AimTargetDriver : MonoBehaviour
    {
        [Tooltip("被驱动的瞄准目标（AimRig 的 Multi-Aim Source）。")]
        [SerializeField] private Transform _aimTarget;

        [Tooltip("瞄准方向源相机。留空自动取 Camera.main。")]
        [SerializeField] private Camera _aimCamera;

        [Tooltip("目标点距相机的距离（米）。越大上半身朝向越接近相机视线、视差越小。")]
        [SerializeField, Range(5f, 200f)] private float _distance = 50f;

        private void Awake()
        {
            if (_aimCamera == null)
                _aimCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_aimTarget == null)
                return;

            Camera cam = _aimCamera != null ? _aimCamera : Camera.main;
            if (cam == null)
                return;

            Transform camT = cam.transform;
            _aimTarget.position = camT.position + camT.forward * _distance;
        }
    }
}
