using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class TickShootingCommand : AbstractCommand
    {
        private readonly Guid _context;
        private readonly int _frame;
        private readonly float _delta;
        public TickShootingCommand(Guid context, int frame, float delta) { _context = context; _frame = frame; _delta = delta; }
        protected override void OnExecute() => this.GetSystem<ShootingSystem>().Tick(_context, _frame, _delta);
    }
}
