using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Unomata.Gameplay;
namespace Unomata.Editor.Validation
{
    public static class AimJumpAnimationSetup
    {
        public static string Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(AimRepairSetup.Folder+"/ShoulderAim.controller");
            ConfigureController(controller);
            var profile=AssetDatabase.LoadAssetAtPath<PlayerAimProfile>(AimRepairSetup.Folder+"/RifleAim.asset");
            var data=new SerializedObject(profile);
            data.FindProperty("_jumpTakeoffPhase").floatValue=.15f;data.FindProperty("_jumpApexPhase").floatValue=.40f;
            data.FindProperty("_jumpLandingPhase").floatValue=.90f;data.FindProperty("_fallStartPhase").floatValue=.75f;
            data.FindProperty("_jumpBlendDuration").floatValue=.04f;data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            return "JumpStart/InAir share a physics-phased AirR clip; takeoff is explicitly blended in the launch frame.";
        }
        public static void ConfigureController(AnimatorController controller)
        {
            foreach(var name in new[]{"AirPhase","VerticalVelocity"})
                if(!controller.parameters.Any(x=>x.name==name))controller.AddParameter(name,AnimatorControllerParameterType.Float);
            var machine=controller.layers[0].stateMachine;
            var jump=machine.states.Single(x=>x.state.name=="JumpStart").state;
            var air=machine.states.Single(x=>x.state.name=="InAir").state;
            var land=machine.states.Single(x=>x.state.name=="Land").state;
            foreach(var item in machine.states)
                foreach(var transition in item.state.transitions.ToArray())
                    if(transition.conditions.Any(x=>x.parameter=="Jump"))item.state.RemoveTransition(transition);
            for(int i=controller.parameters.Length-1;i>=0;i--)if(controller.parameters[i].name=="Jump")controller.RemoveParameter(i);
            var motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(AimRepairSetup.Folder+"/JumpStart.anim");
            foreach(var state in new[]{jump,air})
            {state.motion=motion;state.speed=1;state.speedParameterActive=false;state.timeParameter="AirPhase";state.timeParameterActive=true;EditorUtility.SetDirty(state);}
            foreach(var transition in jump.transitions.ToArray())jump.RemoveTransition(transition);
            var touch=jump.AddTransition(land);Setup(touch,.04f);touch.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            var descend=jump.AddTransition(air);Setup(descend,.03f);descend.AddCondition(AnimatorConditionMode.Less,0,"VerticalVelocity");
            foreach(var transition in air.transitions)if(transition.destinationState==land)Setup(transition,.04f);
            EditorUtility.SetDirty(controller);
        }
        private static void Setup(AnimatorStateTransition t,float duration)
        {t.hasExitTime=false;t.hasFixedDuration=true;t.duration=duration;t.offset=0;t.interruptionSource=TransitionInterruptionSource.SourceThenDestination;}
    }
}