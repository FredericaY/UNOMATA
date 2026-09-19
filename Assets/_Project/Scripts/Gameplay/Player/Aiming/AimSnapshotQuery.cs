using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class AimSnapshotQuery : AbstractQuery<AimSnapshot>
    {
        private readonly Guid _context;
        private readonly int _frame;
        public AimSnapshotQuery(Guid context, int frame) { _context=context; _frame=frame; }
        protected override AimSnapshot OnDo()
        {
            var model = this.GetModel<AimModel>();
            var snapshot = model.Snapshot;
            return model.ActiveContext==_context && snapshot.SceneGeneration==_context && snapshot.FrameId==_frame
                ? snapshot : AimSnapshot.Unavailable(_context, _frame, AimStatus.Invalid, AimFailure.StaleFrame);
        }
    }
}