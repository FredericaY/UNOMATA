using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class CompleteAimFrameCommand : AbstractCommand
    {
        private readonly Guid _context;
        private readonly int _frame;
        private readonly Vector3 _muzzle, _direction;
        private readonly bool _gripsReachable;
        public CompleteAimFrameCommand(Guid context, int frame, Vector3 muzzle, Vector3 direction, bool gripsReachable = true)
        { _context=context; _frame=frame; _muzzle=muzzle; _direction=direction; _gripsReachable=gripsReachable; }
        protected override void OnExecute() => this.GetSystem<IAimSystem>().Complete(_context, _frame, _muzzle, _direction, _gripsReachable);
    }
}