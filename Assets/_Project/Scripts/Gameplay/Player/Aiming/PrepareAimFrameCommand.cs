using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class PrepareAimFrameCommand : AbstractCommand
    {
        private readonly AimFrameInput _input;
        public PrepareAimFrameCommand(AimFrameInput input) { _input=input; }
        protected override void OnExecute() => this.GetSystem<IAimSystem>().Prepare(_input);
    }
}