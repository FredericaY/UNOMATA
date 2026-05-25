using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// UNOMATA QFramework Architecture 入口类
    /// Phase 0: 最小化初始化，Phase 2 补充 Systems/Models
    /// </summary>
    public class GameApp : Architecture<GameApp>
    {
        protected override void Init()
        {
            // Phase 2 补充：
            // this.RegisterSystem<HackSystem>(new HackSystem());
            // this.RegisterSystem<WaveSystem>(new WaveSystem());
            // this.RegisterSystem<PlayerSystem>(new PlayerSystem());
            // this.RegisterModel<PlayerModel>(new PlayerModel());
            // this.RegisterModel<WaveModel>(new WaveModel());
        }
    }
}
