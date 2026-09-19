using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class BeginShootingCommand : AbstractCommand
    {
        private readonly Guid _context, _aimContext;
        private readonly WeaponSettings _settings;
        private readonly int[] _ignored;
        public BeginShootingCommand(Guid context, Guid aimContext, WeaponSettings settings, int[] ignored)
        { _context = context; _aimContext = aimContext; _settings = settings; _ignored = ignored; }
        protected override void OnExecute() => this.GetSystem<ShootingSystem>().Begin(_context, _aimContext, _settings, _ignored);
    }
}
