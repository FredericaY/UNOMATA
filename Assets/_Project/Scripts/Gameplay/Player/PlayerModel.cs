using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 玩家数据模型（QFramework Model 层）。
    /// 仅存储玩家状态数据，不包含任何业务逻辑。
    /// 当前血量与最大血量使用 float，便于 Phase 4 联动伤害减免系数计算。
    /// </summary>
    public class PlayerModel : AbstractModel
    {
        /// <summary>玩家当前生命值，初始 100f，不得低于 0。</summary>
        public BindableProperty<float> HP = new BindableProperty<float>(100f);

        /// <summary>玩家最大生命值，初始 100f。</summary>
        public BindableProperty<float> MaxHp = new BindableProperty<float>(100f);

        protected override void OnInit()
        {
            // BindableProperty 已在字段声明时初始化，此处无需额外操作。
        }
    }
}
