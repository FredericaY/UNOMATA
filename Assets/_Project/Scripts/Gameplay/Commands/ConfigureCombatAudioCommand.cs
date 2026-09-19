using QFramework;
namespace Unomata.Gameplay
{
    public sealed class ConfigureCombatAudioCommand : AbstractCommand
    {
        private readonly CombatAudioSettings _settings;
        public ConfigureCombatAudioCommand(CombatAudioSettings settings) { _settings = settings; }
        protected override void OnExecute() => this.GetSystem<AudioSystem>().ConfigureCombat(_settings);
    }
}
