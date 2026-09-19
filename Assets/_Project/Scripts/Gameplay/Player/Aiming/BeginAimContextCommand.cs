using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class BeginAimContextCommand : AbstractCommand
    {
        private readonly Guid _context;
        private readonly int[] _ignored;
        public BeginAimContextCommand(Guid context, int[] ignored) { _context=context; _ignored=ignored; }
        protected override void OnExecute() => this.GetSystem<IAimSystem>().BeginContext(_context, _ignored);
    }
}