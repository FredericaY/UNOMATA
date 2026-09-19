using System;
using QFramework;

namespace Unomata.Gameplay
{
    public sealed class AimModel : AbstractModel
    {
        public Guid ActiveContext { get; internal set; }
        public AimPoseSolution Solution { get; internal set; }
        public AimSnapshot Snapshot { get; internal set; }
        protected override void OnInit() { }
    }
}