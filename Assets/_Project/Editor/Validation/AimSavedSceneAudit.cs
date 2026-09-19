using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
namespace Unomata.Editor.Validation
{
    public static class AimSavedSceneAudit
    {
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=SceneManager.GetActiveScene();var checks=new List<string>();
            Action<bool,string> check=(ok,label)=>{if(!ok)throw new InvalidOperationException(label);checks.Add(label);};
            check(scene.path=="Assets/_Project/Scenes/SampleScene.unity"&&!scene.isDirty,"saved formal scene loaded");
            var roots=scene.GetRootGameObjects();
            var player=roots.Single(x=>x.name=="PlayerArmature");var view=player.GetComponent<PlayerAimPresentation>();
            check(view!=null&&view.enabled&&player.GetComponent<PlayerMotor>().enabled,"single project motor and presentation");
            check(player.GetComponent<StarterAssets.ThirdPersonController>()==null&&player.GetComponent<StrafeController>()==null&&player.GetComponent<AnimatorAimBridge>()==null&&player.GetComponent<CameraAimBridge>()==null&&player.GetComponent<AimTargetDriver>()==null,"legacy writers removed");
            check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player)==AimSceneMigration.PlayerPrefab,"formal project prefab connected");
            check(player.GetComponentsInChildren<Animator>(false).Count(a=>a.enabled)==1,"one active humanoid animator");
            var builder=player.GetComponent<RigBuilder>();check(!builder.enabled&&builder.layers.Count==2,"automatic rig graph disabled with two valid layers");
            check(builder.layers.All(x=>x.rig!=null),"rig references saved");
            check(player.GetComponentsInChildren<StableArmIKConstraint>(true).Length==2,"stable arm constraints saved");
            check(player.GetComponentsInChildren<TwoBoneIKConstraint>(true).Length==0,"legacy pole solver removed");
            var data=new SerializedObject(view);
            foreach(var field in new[]{"_motor","_animator","_controller","_rigBuilder","_torsoRig","_handsRig","_profile","_brain","_aimCamera","_renderCamera","_weapon","_muzzle","_rightGrip","_leftGrip","_rightHand","_leftHand","_rightHint","_leftHint","_rightTarget","_leftTarget"})
                check(data.FindProperty(field).objectReferenceValue!=null,"saved reference "+field);
            var profile=(PlayerAimProfile)data.FindProperty("_profile").objectReferenceValue;check(profile.IsValid,"saved aim profile valid");
            var muzzle=(Transform)data.FindProperty("_muzzle").objectReferenceValue;
            var mesh=muzzle.parent.GetComponentInChildren<MeshFilter>();check(Vector3.Angle(mesh.transform.forward,muzzle.forward)<0.01f,"muzzle axis equals mesh barrel axis");
            var receiver=player.GetComponent<PlayerAnimEventReceiver>();var parent=muzzle.parent.parent;receiver.SwitchSocket("To_Hand_R_Socket");check(muzzle.parent.parent==parent,"legacy event cannot reparent weapon");
            var controller=(AnimatorController)data.FindProperty("_controller").objectReferenceValue;
            foreach(var clip in controller.animationClips.Distinct())
            {check(clip.isHumanMotion,"humanoid clip "+clip.name);check(AnimationUtility.GetAnimationEvents(clip).Length==0,"no legacy events "+clip.name);check(AssetDatabase.GetAssetPath(clip).StartsWith(AimRepairSetup.Folder+"/"),"project-owned clip "+clip.name);}
            check(!controller.layers[0].stateMachine.states.Any(x=>x.state.name=="Fly"),"no reachable Fly state");
            check(!roots.Any(x=>x.name=="QFrameworkValidator"),"no automatic HP validation");
            check(!roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).Any(t=>t.name.Contains("Validation_Fixture")||t.name=="AimValidationTarget"||t.name=="AimInspectionCamera"||t.name=="AimInspectionGround"),"no diagnostic object persisted");
            var audio=roots.Select(x=>x.GetComponent<AudioBridge>()).Single(x=>x!=null);var ad=new SerializedObject(audio);
            check(ad.FindProperty("_pose").objectReferenceValue==view&&ad.FindProperty("_playerAnimator").objectReferenceValue==player.GetComponent<Animator>(),"audio rebound to current character");
            check(audio.GetComponents<MonoBehaviour>().All(c=>c!=null&&c.GetType().Name!="UnRegisterOnDestroyTrigger"),"no runtime audio helper persisted");
            var json=JsonConvert.SerializeObject(new{state="passed",assertions=checks.Count,checks},Formatting.Indented);
            File.WriteAllText(".utmp/aim-repair/saved-scene-audit.json",json);return JsonConvert.SerializeObject(new{state="passed",assertions=checks.Count});
        }
    }
}