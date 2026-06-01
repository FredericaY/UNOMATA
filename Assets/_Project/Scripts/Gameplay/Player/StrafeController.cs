using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 瞄准下半身朝向控制器（QFramework Controller 层，TPS 死区迟滞模型）。
    /// [DefaultExecutionOrder(10)] 确保在 TPC LateUpdate(0) 之后覆盖 transform.rotation。
    ///
    /// 瞄准时控制角色整体（下半身）绕世界 Y 轴 Yaw：
    ///   1. 进入瞄准当帧：snap 对齐相机 Yaw（一次性，上下半身整体对齐）
    ///   2. 移动中（Move ≠ 0）：直接锁相机 Yaw（无死区，保证 strafe BlendTree 方向正确）
    ///   3. 静止：死区迟滞——|Δ| ≤ 死区时脚不动，仅上半身扭腰（由 AnimatorAimBridge 负责）；
    ///            |Δ| > 死区时身体 Yaw 用 MoveTowardsAngle 平滑追向相机
    /// 覆盖后恢复 PlayerCameraRoot 世界 Rotation，防止破坏 TPC 的 Cinemachine 相机目标。
    ///
    /// 上半身 Yaw 补偿 + Pitch 由 AnimatorAimBridge[20]（本脚本之后运行）负责。
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class StrafeController : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;

        [SerializeField] private Transform _cameraRoot;  // 拖入 PlayerCameraRoot

        [Header("死区迟滞参数（可运行时调）")]
        [SerializeField, Range(0f, 90f)]   private float _deadzoneDeg   = 60f;  // 静止时上半身最大扭转 / 下半身起转阈值

        [SerializeField, Range(45f, 720f)] private float _turnSpeedDeg  = 360f; // 静止超死区后下半身追身角速度(度/秒)
        [SerializeField, Range(0f, 0.5f)]  private float _moveThreshold = 0.1f; // 判定"移动"的 Move 模长阈值

        private PlayerInputModel _inputModel;
        private Camera _mainCamera;
        private bool _isAiming;
        private bool _justEnteredAiming;

        private void Start()
        {
            _mainCamera = Camera.main;
            _inputModel = this.GetModel<PlayerInputModel>();

            this.RegisterEvent<AimStateChangedEvent>(OnAimStateChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // 自动查找 PlayerCameraRoot（若 Inspector 未赋值）
            if (_cameraRoot == null)
            {
                var t = transform.Find("PlayerCameraRoot");
                if (t == null)
                {
                    foreach (Transform child in GetComponentsInChildren<Transform>())
                    {
                        if (child.name == "PlayerCameraRoot") { _cameraRoot = child; break; }
                    }
                }
                else _cameraRoot = t;
            }
        }

        private void OnAimStateChanged(AimStateChangedEvent e)
        {
            _isAiming = e.IsAiming;
            if (e.IsAiming) _justEnteredAiming = true;   // 标记进入瞄准，LateUpdate 当帧 snap
        }

        private void LateUpdate()
        {
            if (!_isAiming || _mainCamera == null) return;

            // 1. 保存 TPC 设置的相机目标世界 Rotation
            Quaternion savedCamRootRot = _cameraRoot != null ? _cameraRoot.rotation : Quaternion.identity;

            float camYaw  = _mainCamera.transform.eulerAngles.y;
            float bodyYaw = transform.eulerAngles.y;

            // 2. 计算本帧下半身目标 Yaw
            if (_justEnteredAiming)
            {
                bodyYaw = camYaw;                        // 进入瞄准：snap 对齐
                _justEnteredAiming = false;
            }
            else
            {
                bool moving = _inputModel != null &&
                              _inputModel.Move.Value.sqrMagnitude > _moveThreshold * _moveThreshold;
                if (moving)
                {
                    bodyYaw = camYaw;                    // 移动：锁相机
                }
                else
                {
                    float delta = Mathf.DeltaAngle(bodyYaw, camYaw);   // 静止：死区迟滞
                    if (Mathf.Abs(delta) > _deadzoneDeg)
                        bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, camYaw, _turnSpeedDeg * Time.deltaTime);
                    // 死区内：bodyYaw 不变，脚定住（上半身由 AnimatorAimBridge 扭腰跟随）
                }
            }

            transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);

            // 3. 恢复相机目标 Rotation（防止 Cinemachine 抖动）
            if (_cameraRoot != null)
                _cameraRoot.rotation = savedCamRootRot;
        }
    }
}
