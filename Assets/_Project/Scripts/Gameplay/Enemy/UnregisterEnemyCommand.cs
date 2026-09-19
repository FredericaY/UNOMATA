using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class UnregisterEnemyCommand : AbstractCommand
    {
        private readonly Guid _id;
        public UnregisterEnemyCommand(Guid id) { _id = id; }
        protected override void OnExecute() => this.GetSystem<EnemySystem>().Unregister(_id);
    }
}
