using System;
using System.Collections.Generic;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class EnemyModel : AbstractModel
    {
        internal sealed class Record
        {
            internal EnemySnapshot State;
            internal int[] ColliderIds;
            internal readonly Dictionary<Guid, long> ConsumedShots = new Dictionary<Guid, long>();
        }
        internal readonly HashSet<Guid> ActiveShotContexts = new HashSet<Guid>();
        internal readonly Dictionary<Guid, Record> Records = new Dictionary<Guid, Record>();
        internal readonly Dictionary<int, Guid> ColliderOwners = new Dictionary<int, Guid>();
        public int RegisteredCount => Records.Count;
        public EnemyDamageResult LastDamage { get; internal set; }
        protected override void OnInit() { }
        protected override void OnDeinit()
        { Records.Clear(); ColliderOwners.Clear(); ActiveShotContexts.Clear(); LastDamage = default; }
    }
}
