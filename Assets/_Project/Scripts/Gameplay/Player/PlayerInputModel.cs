using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 玩家输入状态数据模型。
    /// 全部输入的唯一数据源，不含任何业务逻辑。
    /// </summary>
    public class PlayerInputModel : AbstractModel
    {
        /// <summary>移动输入（WASD），范围 [-1,1] x [-1,1]</summary>
        public BindableProperty<Vector2> Move { get; } = new BindableProperty<Vector2>(Vector2.zero);

        /// <summary>跳跃输入</summary>
        public BindableProperty<bool> Jump { get; } = new BindableProperty<bool>(false);

        /// <summary>冲刺输入</summary>
        public BindableProperty<bool> Sprint { get; } = new BindableProperty<bool>(false);

        /// <summary>瞄准输入（右键）</summary>
        public BindableProperty<bool> IsAiming { get; } = new BindableProperty<bool>(false);

        /// <summary>射击输入（左键，B2a 阶段使用）</summary>
        public BindableProperty<bool> Fire { get; } = new BindableProperty<bool>(false);

        protected override void OnInit() { }
    }
}
