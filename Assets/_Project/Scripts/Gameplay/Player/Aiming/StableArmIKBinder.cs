using UnityEngine;
using UnityEngine.Animations.Rigging;
namespace Unomata.Gameplay
{
    public sealed class StableArmIKBinder : AnimationJobBinder<StableArmIKJob,TwoBoneIKConstraintData>
    {
        private readonly TwoBoneIKConstraintJobBinder<TwoBoneIKConstraintData> _source=new TwoBoneIKConstraintJobBinder<TwoBoneIKConstraintData>();
        public override StableArmIKJob Create(Animator animator,ref TwoBoneIKConstraintData data,Component component)
            => new StableArmIKJob{Core=_source.Create(animator,ref data,component)};
        public override void Destroy(StableArmIKJob job) => _source.Destroy(job.Core);
    }
}