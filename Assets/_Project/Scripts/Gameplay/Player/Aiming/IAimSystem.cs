using System;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    public interface IAimSystem : ISystem
    {
        void BeginContext(Guid context, int[] ignoredColliders);
        void Prepare(AimFrameInput input);
        void Complete(Guid context, int frame, Vector3 muzzlePosition, Vector3 barrelDirection, bool gripsReachable = true);
        void Invalidate(Guid context, int frame, bool release);
    }
}