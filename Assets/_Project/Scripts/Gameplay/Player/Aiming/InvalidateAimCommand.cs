using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class InvalidateAimCommand : AbstractCommand
    {
        private readonly Guid _context;
        private readonly int _frame;
        private readonly bool _release;
        public InvalidateAimCommand(Guid context, int frame, bool release=false)
        { _context=context; _frame=frame; _release=release; }
        protected override void OnExecute() => this.GetSystem<IAimSystem>().Invalidate(_context, _frame, _release);
    }
}