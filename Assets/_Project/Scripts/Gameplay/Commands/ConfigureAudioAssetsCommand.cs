using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class ConfigureAudioAssetsCommand : AbstractCommand
    {
        private readonly AudioClip[] _footsteps;
        private readonly AudioClip _landing;
        public ConfigureAudioAssetsCommand(AudioClip[] footsteps,AudioClip landing)
        {_footsteps=footsteps;_landing=landing;}
        protected override void OnExecute()=>this.GetSystem<AudioSystem>().ConfigureAssets(_footsteps,_landing);
    }
}