using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class SetEnemyHackFactorCommand : AbstractCommand
    {
        private readonly Guid _id;
        private readonly float _factor;
        public SetEnemyHackFactorCommand(Guid id, float factor) { _id = id; _factor = factor; }
        protected override void OnExecute() => this.GetSystem<EnemySystem>().SetHackFactor(_id, _factor);
    }
}
