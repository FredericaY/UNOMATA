using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class EnemySnapshotQuery : AbstractQuery<EnemySnapshot>
    {
        private readonly Guid _id;
        public EnemySnapshotQuery(Guid id) { _id = id; }
        protected override EnemySnapshot OnDo() => this.GetSystem<EnemySystem>().Read(_id);
    }
}
