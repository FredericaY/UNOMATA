using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 波次管理系统（QFramework System 层）。
    /// 持有 WaveModel 引用，管理波次推进与敌人生成。
    /// B0 骨架阶段方法均为空实现，B3c 填充完整逻辑。
    /// </summary>
    public class WaveSystem : AbstractSystem
    {
        private WaveModel _waveModel;

        protected override void OnInit()
        {
            _waveModel = this.GetModel<WaveModel>();
        }

        /// <summary>
        /// 开始新一波次，生成本波敌人。
        /// TODO (B3c): 读取 WaveConfig，Instantiate 敌人 Prefab，设置 WaveModel.AliveCount。
        /// </summary>
        public void OnStartWave()
        {
            // TODO (B3c): 填充波次开始生成逻辑
        }

        /// <summary>
        /// 通知系统一个敌人已被消灭。
        /// TODO (B3c): WaveModel.AliveCount--，归零时触发 OnWaveClear 事件，延迟推进下一波。
        /// </summary>
        public void OnEnemyKilled()
        {
            // TODO (B3c): 填充 AliveCount-- 与全灭判定逻辑
        }
    }
}
