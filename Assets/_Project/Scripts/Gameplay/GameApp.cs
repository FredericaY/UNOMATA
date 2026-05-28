using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// UNOMATA QFramework Architecture 入口类。
    ///
    /// 已注册：
    ///   Model：PlayerModel / WaveModel / AudioModel
    ///   System：PlayerSystem / WaveSystem / AudioSystem
    ///   Commands（骨架）：StartHackCommand / SelectCardCommand / HealCommand / DamagePlayerCommand
    ///                     PlayFootstepCommand / PlayLandCommand
    ///
    /// Phase 4 补充：HackSystem / SyncRateModel / SyncRateSystem 等。
    /// </summary>
    public class GameApp : Architecture<GameApp>
    {
        protected override void Init()
        {
            // ── Model 层（先于 System 注册）────────────────────────────
            this.RegisterModel<PlayerModel>(new PlayerModel());
            this.RegisterModel<WaveModel>(new WaveModel());
            this.RegisterModel<AudioModel>(new AudioModel());

            // ── System 层 ───────────────────────────────────────────────
            this.RegisterSystem<PlayerSystem>(new PlayerSystem());
            this.RegisterSystem<WaveSystem>(new WaveSystem());
            this.RegisterSystem<AudioSystem>(new AudioSystem());
        }
    }
}
