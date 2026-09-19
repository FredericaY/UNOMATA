using QFramework;
namespace Unomata.Gameplay
{
    /// <summary>Single gameplay composition root. Register data, utilities, then dependent systems.</summary>
    public class GameApp : Architecture<GameApp>
    {
        protected override void Init()
        {
            this.RegisterModel<PlayerModel>(new PlayerModel());
            this.RegisterModel<PlayerInputModel>(new PlayerInputModel());
            this.RegisterModel<WaveModel>(new WaveModel());
            this.RegisterModel<AudioModel>(new AudioModel());
            this.RegisterModel<AimModel>(new AimModel());
            this.RegisterModel<EnemyModel>(new EnemyModel());
            this.RegisterModel<ShootingModel>(new ShootingModel());

            this.RegisterUtility<IAimWorldQuery>(new UnityAimWorldQuery());
            this.RegisterUtility<IShotWorldQuery>(new UnityShotWorldQuery());

            this.RegisterSystem<PlayerSystem>(new PlayerSystem());
            this.RegisterSystem<WaveSystem>(new WaveSystem());
            this.RegisterSystem<AudioSystem>(new AudioSystem());
            this.RegisterSystem<IAimSystem>(new AimSystem());
            this.RegisterSystem<EnemySystem>(new EnemySystem());
            this.RegisterSystem<ShootingSystem>(new ShootingSystem());
        }
    }
}
