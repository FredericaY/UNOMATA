using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 波次数据模型（QFramework Model 层）。
    /// 仅存储波次状态数据，不包含任何业务逻辑。
    /// B0 阶段只维护波次编号与存活敌人数，敌人对象列表由 B3c 扩展。
    /// </summary>
    public class WaveModel : AbstractModel
    {
        /// <summary>当前波次编号，0 表示游戏尚未开始第一波。</summary>
        public BindableProperty<int> WaveNumber = new BindableProperty<int>(0);

        /// <summary>本波当前存活敌人数量，归零时由 WaveSystem 触发波次结算。</summary>
        public BindableProperty<int> AliveCount = new BindableProperty<int>(0);

        protected override void OnInit()
        {
            // BindableProperty 已在字段声明时初始化，此处无需额外操作。
        }
    }
}
