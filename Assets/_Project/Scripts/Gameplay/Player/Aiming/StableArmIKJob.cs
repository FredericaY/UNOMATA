using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Unomata.Gameplay
{
    /// <summary>Package two-bone solve with a pole correction that cannot move its solved endpoint.</summary>
    [Unity.Burst.BurstCompile]
    public struct StableArmIKJob : IWeightedAnimationJob
    {
        public TwoBoneIKConstraintJob Core;
        public FloatProperty jobWeight { get => Core.jobWeight; set => Core.jobWeight=value; }
        public void ProcessRootMotion(AnimationStream stream) { }
        public void ProcessAnimation(AnimationStream stream)
        {
            float weight=jobWeight.Get(stream);
            if(weight<=0)
            {
                AnimationRuntimeUtils.PassThrough(stream,Core.root);
                AnimationRuntimeUtils.PassThrough(stream,Core.mid);
                AnimationRuntimeUtils.PassThrough(stream,Core.tip);
                return;
            }
            AnimationRuntimeUtils.SolveTwoBoneIK(stream,Core.root,Core.mid,Core.tip,Core.target,Core.hint,
                Core.targetPositionWeight.Get(stream)*weight,Core.targetRotationWeight.Get(stream)*weight,0,Core.targetOffset);
            float hintWeight=Core.hintWeight.Get(stream)*weight;
            if(hintWeight<=0||!Core.hint.IsValid(stream))return;
            var root=Core.root.GetPosition(stream);var mid=Core.mid.GetPosition(stream);var tip=Core.tip.GetPosition(stream);
            var axis=tip-root;
            if(axis.sqrMagnitude<1e-10f)return;
            axis.Normalize();
            var bend=Vector3.ProjectOnPlane(mid-root,axis);
            var desired=Vector3.ProjectOnPlane(Core.hint.GetPosition(stream)-root,axis);
            float reach=Vector3.Distance(root,mid)+Vector3.Distance(mid,tip);
            if(bend.sqrMagnitude<reach*reach*0.001f||desired.sqrMagnitude<1e-10f)return;
            var tipRotation=Core.tip.GetRotation(stream);
            Core.root.SetRotation(stream,PoleRotation(axis,bend,desired,hintWeight)*Core.root.GetRotation(stream));
            Core.tip.SetRotation(stream,tipRotation);
        }
        public static Quaternion PoleRotation(Vector3 axis,Vector3 from,Vector3 to,float weight)
        {
            if(axis.sqrMagnitude<1e-10f)return Quaternion.identity;
            axis.Normalize();from=Vector3.ProjectOnPlane(from,axis);to=Vector3.ProjectOnPlane(to,axis);
            if(from.sqrMagnitude<1e-10f||to.sqrMagnitude<1e-10f)return Quaternion.identity;
            from.Normalize();to.Normalize();
            float radians=Mathf.Atan2(Vector3.Dot(axis,Vector3.Cross(from,to)),Mathf.Clamp(Vector3.Dot(from,to),-1,1));
            // Unlike a general FromToRotation, this axis stays defined even for opposite projected vectors.
            return Quaternion.AngleAxis(radians*Mathf.Rad2Deg*Mathf.Clamp01(weight),axis);
        }
    }
}