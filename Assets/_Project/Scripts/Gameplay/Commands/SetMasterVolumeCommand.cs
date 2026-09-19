using QFramework;
namespace Unomata.Gameplay
{
    public sealed class SetMasterVolumeCommand : AbstractCommand
    {
        private readonly float _volume;
        public SetMasterVolumeCommand(float volume) { _volume = volume; }
        protected override void OnExecute() => this.GetSystem<AudioSystem>().SetMasterVolume(_volume);
    }
}
