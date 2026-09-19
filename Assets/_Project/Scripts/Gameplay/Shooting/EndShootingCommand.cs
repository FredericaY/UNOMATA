using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class EndShootingCommand : AbstractCommand
    {
        private readonly Guid _context;
        public EndShootingCommand(Guid context) { _context = context; }
        protected override void OnExecute() => this.GetSystem<ShootingSystem>().End(_context);
    }
}
