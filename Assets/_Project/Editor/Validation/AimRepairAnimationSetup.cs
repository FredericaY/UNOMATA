using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
using Object=UnityEngine.Object;

namespace Unomata.Editor.Validation
{
    public static class AimRepairAnimationSetup
    {
        public static string Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=AimRepairSetup.PrototypeScene && scene.path!="Assets/_Project/Scenes/SampleScene.unity")throw new InvalidOperationException("Open the aim prototype scene.");
            var player=scene.GetRootGameObjects().Single(x=>x.name=="PlayerArmature");
            string folder=AimRepairSetup.Folder;
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/ShoulderAim.controller");
            var machine=controller.layers[0].stateMachine;
            var aimState=machine.states.Single(x=>x.state.name=="AimGround").state;
            foreach(var name in new[]{"TurnLeft","TurnRight"})
            {
                var turn=machine.states.Single(x=>x.state.name==name).state;
                if(!turn.transitions.Any(t=>t.conditions.Any(c=>c.parameter=="Speed")))
                {
                    var transition=turn.AddTransition(aimState);transition.hasExitTime=false;
                    transition.hasFixedDuration=true;transition.duration=0.1f;
                    transition.AddCondition(AnimatorConditionMode.Greater,0.08f,"Speed");
                }
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/"+name+".anim");
                var settings=AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime=true;settings.loopBlend=true;
                AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
            }
            EditorUtility.SetDirty(controller);
            var names=new[]{"Walk","Run","AimWalk_F","AimWalk_B","AimWalk_L","AimWalk_R","TurnLeft","TurnRight"};
            var phases=new List<(AnimationClip Clip,float Left,float Right)>();
            var evidence=new List<object>();
            foreach(var name in names)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/"+name+".anim");
                var phase=SamplePhase(player,clip);
                phases.Add((clip,phase.x,phase.y));
                evidence.Add(new{clip=name,left=phase.x,right=phase.y,method=clip.averageSpeed.sqrMagnitude>0.01f?"maximum foot lead along travel":"minimum foot height"});
            }
            var profile=AssetDatabase.LoadAssetAtPath<FootstepPhaseProfile>(folder+"/Footsteps.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<FootstepPhaseProfile>();AssetDatabase.CreateAsset(profile,folder+"/Footsteps.asset");}
            var data=new SerializedObject(profile);var entries=data.FindProperty("_entries");entries.arraySize=phases.Count;
            for(int i=0;i<phases.Count;i++)
            {
                var entry=entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_clip").objectReferenceValue=phases[i].Clip;
                entry.FindPropertyRelative("_left").floatValue=phases[i].Left;
                entry.FindPropertyRelative("_right").floatValue=phases[i].Right;
            }
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(profile);
            var audio=scene.GetRootGameObjects().Select(x=>x.GetComponent<AudioBridge>()).Single(x=>x!=null);
            var so=new SerializedObject(audio);
            so.FindProperty("_pose").objectReferenceValue=player.GetComponent<PlayerAimPresentation>();
            so.FindProperty("_phaseProfile").objectReferenceValue=profile;
            so.ApplyModifiedPropertiesWithoutUndo();
            var aimProfile=AssetDatabase.LoadAssetAtPath<PlayerAimProfile>(folder+"/RifleAim.asset");
            var aimData=new SerializedObject(aimProfile);
            aimData.FindProperty("_forwardWalkSpeed").floatValue=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/AimWalk_F.anim").averageSpeed.magnitude;
            aimData.FindProperty("_backWalkSpeed").floatValue=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/AimWalk_B.anim").averageSpeed.magnitude;
            aimData.FindProperty("_sideWalkSpeed").floatValue=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/AimWalk_L.anim").averageSpeed.magnitude;
            aimData.FindProperty("_runStrideSpeed").floatValue=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/Run.anim").averageSpeed.magnitude;
            aimData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(aimProfile);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory(".utmp/aim-repair");
            File.WriteAllText(".utmp/aim-repair/footstep-phases.json",JsonConvert.SerializeObject(evidence,Formatting.Indented));
            return JsonConvert.SerializeObject(new{entries=phases.Count,scene=scene.path,dirty=scene.isDirty,phases=evidence});
        }

        private static Vector2 SamplePhase(GameObject source,AnimationClip clip)
        {
            var scene=EditorSceneManager.NewPreviewScene();var graph=PlayableGraph.Create("FootstepPhaseCalibration");GameObject clone=null;
            try
            {
                var create=typeof(AimRepairDiagnostics).GetMethod("CreateClone",BindingFlags.NonPublic|BindingFlags.Static);
                clone=(GameObject)create.Invoke(null,new object[]{source,scene});
                var animator=clone.GetComponent<Animator>();
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);
                playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);
                var output=AnimationPlayableOutput.Create(graph,"Sample",animator);output.SetSourcePlayable(playable);graph.Play();
                var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                float bestLeft=float.NegativeInfinity,bestRight=float.NegativeInfinity,lp=0,rp=0;
                var velocity=clip.averageSpeed;velocity.y=0;
                if(clip.name.StartsWith("Run_") && float.TryParse(clip.name.Substring(4),out float angle))
                    velocity=Quaternion.Euler(0,angle,0)*velocity;
                bool moving=velocity.sqrMagnitude>0.01f;velocity.Normalize();
                for(int i=0;i<128;i++)
                {
                    float phase=i/128f;playable.SetTime(phase*clip.length);graph.Evaluate(0);
                    var l=clone.transform.InverseTransformPoint(left.position);
                    var r=clone.transform.InverseTransformPoint(right.position);
                    float ls=moving?Vector3.Dot(l,velocity):-l.y;
                    float rs=moving?Vector3.Dot(r,velocity):-r.y;
                    if(ls>bestLeft){bestLeft=ls;lp=phase;}
                    if(rs>bestRight){bestRight=rs;rp=phase;}
                }
                return new Vector2(lp,rp);
            }
            finally{if(graph.IsValid())graph.Destroy();if(clone!=null)Object.DestroyImmediate(clone);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}