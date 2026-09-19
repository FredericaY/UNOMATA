using UnityEngine;
using UnityEngine.Animations.Rigging;
namespace Unomata.Gameplay
{
    [DisallowMultipleComponent,AddComponentMenu("UNOMATA/Stable Arm IK Constraint")]
    public sealed class StableArmIKConstraint : RigConstraint<StableArmIKJob,TwoBoneIKConstraintData,StableArmIKBinder> { }
}