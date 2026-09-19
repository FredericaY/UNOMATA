using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public sealed class AimPoseQuery : AbstractQuery<AimPoseSolution>
    {
        private readonly Guid _context;
        private readonly int _frame;
        public AimPoseQuery(Guid context, int frame) { _context=context; _frame=frame; }
        protected override AimPoseSolution OnDo()
        {
            var model = this.GetModel<AimModel>();
            var solution = model.Solution;
            return model.ActiveContext==_context && solution.Context==_context && solution.Frame==_frame ? solution : default;
        }
    }
}