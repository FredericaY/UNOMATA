using UnityEngine;
using UnityEngine.Animations.Rigging;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 瞄准动画桥接器（QFramework Controller 层）。订阅 <see cref="AimStateChangedEvent"/>，
    /// 进入/退出瞄准时同步渐变三处权重：
    ///   1) UpperBodyAim 动画层权重 —— 持枪手型 pose（端枪动画叠加）。
    ///   2) Aim Rig 权重           —— Multi-Aim Constraint IK 让上半身脊椎链朝向 AimTarget（准星方向）。
    ///   3) MoveX / MoveY          —— 驱动 AimMove BlendTree 的瞄准移动方向。
    ///
    /// 上半身朝向（Yaw 残差 + Pitch）现由 Animation Rigging Multi-Aim Constraint 实现，
    /// 不再手写 spine 骨骼旋转。下半身 Yaw 由 <c>StrafeController</c> 死区迟滞负责，二者组合：
    /// 小幅转视角脚不动、仅上半身 IK 扭腰；大幅转视角脚追身、上半身 IK 回中性。
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class AnimatorAimBridge : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;

        [SerializeField] private Animator _animator;

        [Tooltip("上半身瞄准 IK Rig（Multi-Aim Constraint 链 spine_01→spine_02→spine_03）。")]
        [SerializeField] private Rig _aimRig;

        [Tooltip("瞄准权重 0↔1 渐变时长（秒）。动画层与 Rig 权重共用。")]
        [SerializeField, Range(0.05f, 0.5f)] private float _weightLerpTime = 0.15f;

        private PlayerInputModel _inputModel;
        private int _layerIndex = -1;
        private float _targetWeight;

        private void Start()
        {
            _inputModel = this.GetModel<PlayerInputModel>();

            this.RegisterEvent<AimStateChangedEvent>(OnAimStateChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            if (_animator != null)
                _layerIndex = _animator.GetLayerIndex("UpperBodyAim");
        }

        private void OnAimStateChanged(AimStateChangedEvent e)
        {
            _targetWeight = e.IsAiming ? 1f : 0f;
            if (_animator != null)
                _animator.SetBool("IsAiming", e.IsAiming);
        }

        private void Update()
        {
            float step = Time.deltaTime / Mathf.Max(_weightLerpTime, 0.0001f);

            if (_animator != null && _layerIndex >= 0)
            {
                float cur = _animator.GetLayerWeight(_layerIndex);
                _animator.SetLayerWeight(_layerIndex, Mathf.MoveTowards(cur, _targetWeight, step));

                if (_inputModel != null)
                {
                    _animator.SetFloat("MoveX", _inputModel.Move.Value.x);
                    _animator.SetFloat("MoveY", _inputModel.Move.Value.y);
                }
            }

            if (_aimRig != null)
                _aimRig.weight = Mathf.MoveTowards(_aimRig.weight, _targetWeight, step);
        }
    }
}
