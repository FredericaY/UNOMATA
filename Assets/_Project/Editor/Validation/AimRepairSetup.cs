using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using Newtonsoft.Json;
using StarterAssets;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
using Object=UnityEngine.Object;

namespace Unomata.Editor.Validation
{
    /// <summary>Explicit project-only asset migration. Preview assets remain separate until acceptance.</summary>
    public static class AimRepairSetup
    {
        public const string PrototypeScene="Assets/_Project/Scenes/Sandbox/AimLocomotion.unity";
        public const string Folder="Assets/_Project/Animations/Player/Aiming";
        private const float SupportGripRetraction = 0.10f; // Rear of the existing handguard; one weapon calibration for every pose.
        private const string Rifle="Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/";
        private const string Female="Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/Animations_Rifle/";

        public static string CreatePrototype()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before scene migration.");
            var original=SceneManager.GetActiveScene();
            if(original.isDirty)throw new InvalidOperationException("Save/review the active scene before migration.");
            original=EditorSceneManager.OpenScene("Assets/_Project/Scenes/SampleScene.unity",OpenSceneMode.Single);
            var source=original.GetRootGameObjects().Single(x=>x.name=="PlayerArmature");
            var aim=SamplePose(source,LoadClip(Rifle+"Aiming/R_AimIdle.fbx"));
            var low=SamplePose(source,LoadClip(Rifle+"Normal/R_Idle.fbx"));
            EnsureFolder(Folder);
            var controller=BuildController(Folder+"/ShoulderAim.controller");
            var profile=AssetDatabase.LoadAssetAtPath<PlayerAimProfile>(Folder+"/RifleAim.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<PlayerAimProfile>();AssetDatabase.CreateAsset(profile,Folder+"/RifleAim.asset");}
            var settings=new SerializedObject(profile);
            settings.FindProperty("_gripOffset").vector3Value=aim.RightPosition-aim.Shoulder;
            settings.FindProperty("_leftElbow").vector3Value=aim.LeftElbow+new Vector3(-0.12f,0,-0.12f);
            settings.FindProperty("_rightElbow").vector3Value=aim.RightElbow+new Vector3(0.12f,0,-0.12f);
            var spine=settings.FindProperty("_neutralSpine");spine.arraySize=3;
            for(int i=0;i<3;i++)spine.GetArrayElementAtIndex(i).quaternionValue=aim.Spine[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeScene)==null)
                if(!AssetDatabase.CopyAsset(original.path,PrototypeScene))throw new InvalidOperationException("Cannot copy baseline scene.");
            var scene=EditorSceneManager.OpenScene(PrototypeScene,OpenSceneMode.Single);
            controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/ShoulderAim.controller");
            profile=AssetDatabase.LoadAssetAtPath<PlayerAimProfile>(Folder+"/RifleAim.asset");
            var player=scene.GetRootGameObjects().Single(x=>x.name=="PlayerArmature");
            var prefabRoot=PrefabUtility.GetOutermostPrefabInstanceRoot(player);
            if(prefabRoot!=null)PrefabUtility.UnpackPrefabInstance(prefabRoot,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var animator=player.GetComponent<Animator>();
            var model=player.transform.Find("Rifle_Full_Body");
            var modelPrefab=PrefabUtility.GetOutermostPrefabInstanceRoot(model.gameObject);
            if(modelPrefab!=null)PrefabUtility.UnpackPrefabInstance(modelPrefab,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var b in player.GetComponents<MonoBehaviour>())
                if(b is ThirdPersonController || b is StrafeController || b is AnimatorAimBridge ||
                    b is CameraAimBridge || b is AimTargetDriver) Object.DestroyImmediate(b);
            foreach(var b in model.GetComponents<MonoBehaviour>())
                if(b.GetType().FullName=="CombatGirls.WeaponControl.Character_Weapon_Controller")Object.DestroyImmediate(b);
            var nested=model.GetComponent<Animator>();if(nested!=null)Object.DestroyImmediate(nested);
            var rigBuilder=player.GetComponent<RigBuilder>();
            rigBuilder.enabled=false;rigBuilder.Clear();rigBuilder.layers.Clear();
            var oldRig=model.Find("AimRig");if(oldRig!=null)Object.DestroyImmediate(oldRig.gameObject);
            var oldTarget=player.transform.Find("AimTarget");if(oldTarget!=null)Object.DestroyImmediate(oldTarget.gameObject);
            var controls=Child(player.transform,"PlayerAimControls");
            var torsoRig=Child(model,"TorsoAimRig").gameObject.GetComponent<Rig>()??Child(model,"TorsoAimRig").gameObject.AddComponent<Rig>();
            var handRig=Child(model,"HandAimRig").gameObject.GetComponent<Rig>()??Child(model,"HandAimRig").gameObject.AddComponent<Rig>();
            torsoRig.weight=0;handRig.weight=1;
            var bones=new[]{HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest};
            var torsoTargets=new Transform[3];
            for(int i=0;i<3;i++)
            {
                var target=Child(controls,"TorsoTarget"+i);
                target.rotation=player.transform.rotation*aim.Spine[i];
                torsoTargets[i]=target;
                var node=Child(torsoRig.transform,"TorsoRotation"+i);
                var constraint=node.GetComponent<MultiRotationConstraint>()??node.gameObject.AddComponent<MultiRotationConstraint>();
                var data=constraint.data;data.constrainedObject=animator.GetBoneTransform(bones[i]);
                data.maintainOffset=false;data.offset=Vector3.zero;
                data.constrainedXAxis=data.constrainedYAxis=data.constrainedZAxis=true;
                var sources=new WeightedTransformArray();sources.Add(new WeightedTransform(target,1));
                data.sourceObjects=sources;constraint.data=data;constraint.weight=1;
            }
            var rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            var leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var mesh=player.GetComponentsInChildren<MeshFilter>(true).Single(x=>x.sharedMesh!=null&&x.sharedMesh.name=="Weapon_Rifle");
            var weapon=mesh.transform.parent;
            foreach(var pc in weapon.GetComponents<ParentConstraint>())Object.DestroyImmediate(pc);
            weapon.SetParent(controls,true);
            if(weapon.parent!=controls)throw new InvalidOperationException("Weapon must be independent of the hand skeleton.");
            var muzzle=Child(weapon,"Muzzle");
            Vector3 tip=FindMuzzleCenter(mesh.sharedMesh);
            muzzle.localPosition=weapon.InverseTransformPoint(mesh.transform.TransformPoint(tip));
            muzzle.localRotation=Quaternion.LookRotation(weapon.InverseTransformDirection(mesh.transform.forward),
                weapon.InverseTransformDirection(mesh.transform.right));
            var rightGrip=Child(weapon,"RightGrip");rightGrip.localPosition=Vector3.zero;rightGrip.localRotation=Quaternion.identity;
            var leftGrip=Child(weapon,"LeftGrip");
            leftGrip.localPosition=Quaternion.Inverse(aim.RightRotation)*(aim.LeftPosition-aim.RightPosition)
                - muzzle.localRotation*Vector3.forward*SupportGripRetraction;
            leftGrip.localRotation=Quaternion.Inverse(aim.RightRotation)*aim.LeftRotation;
            var rightHint=Child(controls,"RightElbowHint");rightHint.localPosition=profile.RightElbow;
            var leftHint=Child(controls,"LeftElbowHint");leftHint.localPosition=profile.LeftElbow;
            var rightTarget=Child(controls,"RightHandTarget");rightTarget.SetPositionAndRotation(rightGrip.position,rightGrip.rotation);
            var leftTarget=Child(controls,"LeftHandTarget");leftTarget.SetPositionAndRotation(leftGrip.position,leftGrip.rotation);
            Arm(handRig.transform,"RightArm",animator,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,rightTarget,rightHint);
            Arm(handRig.transform,"LeftArm",animator,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,leftTarget,leftHint);
            rigBuilder.layers.Add(new RigLayer(torsoRig));rigBuilder.layers.Add(new RigLayer(handRig));
            var motor=player.GetComponent<PlayerMotor>()??player.AddComponent<PlayerMotor>();
            SetObject(motor,"_cameraRoot",player.transform.Find("PlayerCameraRoot"));
            animator.runtimeAnimatorController=controller;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion=false;
            var camera=scene.GetRootGameObjects().Select(x=>x.GetComponent<Camera>()).First(x=>x!=null);
            var brain=camera.GetComponent<CinemachineBrain>();
            var cameras=scene.GetRootGameObjects().Select(x=>x.GetComponent<CinemachineVirtualCamera>()).Where(x=>x!=null).ToArray();
            foreach(var vcam in cameras)
            {
                var body=vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if(body!=null){body.CameraCollisionFilter=1;body.IgnoreTag="Player";body.DampingFromCollision=0.15f;}
            }
            var aimCamera=cameras.Single(x=>x.name=="PlayerAimCamera");
            var view=player.GetComponent<PlayerAimPresentation>()??player.AddComponent<PlayerAimPresentation>();
            var so=new SerializedObject(view);
            Assign(so,"_motor",motor);Assign(so,"_animator",animator);Assign(so,"_controller",controller);
            Assign(so,"_rigBuilder",rigBuilder);Assign(so,"_torsoRig",torsoRig);Assign(so,"_handsRig",handRig);
            Assign(so,"_profile",profile);Assign(so,"_brain",brain);Assign(so,"_aimCamera",aimCamera);Assign(so,"_renderCamera",camera);
            Assign(so,"_weapon",weapon);Assign(so,"_muzzle",muzzle);Assign(so,"_rightGrip",rightGrip);Assign(so,"_leftGrip",leftGrip);
            Assign(so,"_rightHand",rightHand);Assign(so,"_leftHand",leftHand);Assign(so,"_rightHint",rightHint);Assign(so,"_leftHint",leftHint);
            Assign(so,"_rightTarget",rightTarget);Assign(so,"_leftTarget",leftTarget);
            var targets=so.FindProperty("_torsoTargets");targets.arraySize=3;
            for(int i=0;i<3;i++)targets.GetArrayElementAtIndex(i).objectReferenceValue=torsoTargets[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var root in scene.GetRootGameObjects())if(root.name=="QFrameworkValidator")Object.DestroyImmediate(root);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(".utmp/aim-repair");
            File.WriteAllText(".utmp/aim-repair/calibration.json",JsonConvert.SerializeObject(new
            {aim=JsonUtility.ToJson(aim),low=JsonUtility.ToJson(low),muzzleMeshLocal=V(tip),muzzleWeaponLocal=V(muzzle.localPosition),muzzleRotation=Q(muzzle.localRotation)},Formatting.Indented));
            return JsonConvert.SerializeObject(new{scene=scene.path,profile=profile.name,validProfile=profile.IsValid,muzzle=V(muzzle.localPosition),dirty=scene.isDirty});
        }

        private static AnimatorController BuildController(string path)
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller!=null){AimJumpAnimationSetup.ConfigureController(controller);return controller;}
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach(var name in new[]{"Speed","MoveX","MoveY","MotionSpeed","AimMotionRate","Turn","TurnSpeed"})
                controller.AddParameter(name,AnimatorControllerParameterType.Float);
            foreach(var name in new[]{"Jump","Grounded","FreeFall","IsAiming"})
                controller.AddParameter(name,AnimatorControllerParameterType.Bool);
            var baseMachine=controller.layers[0].stateMachine;
            var freeTree=Tree(controller,"FreeLocomotion",BlendTreeType.Simple1D,"Speed","Speed");
            freeTree.AddChild(CopyClip(Rifle+"Normal/R_Idle.fbx","Idle"),0);
            freeTree.AddChild(CopyClip(Rifle+"Normal/R_Walk.fbx","Walk"),2);
            freeTree.AddChild(CopyClip(Rifle+"Normal/R_Run.fbx","Run"),5.335f);
            var ground=State(baseMachine,"Ground",freeTree);
            baseMachine.defaultState=ground;
            var aimTree=Tree(controller,"AimLocomotion",BlendTreeType.SimpleDirectional2D,"MoveX","MoveY");
            aimTree.AddChild(CopyClip(Rifle+"Aiming/R_AimIdle.fbx","AimIdle"),Vector2.zero);
            aimTree.AddChild(CopyClip(Rifle+"Aiming/R_AimWalk_F.fbx","AimWalk_F"),Vector2.up);
            aimTree.AddChild(CopyClip(Rifle+"Aiming/R_AimWalk_B.fbx","AimWalk_B"),Vector2.down);
            aimTree.AddChild(CopyClip(Rifle+"Aiming/R_AimWalk_FL.fbx","AimWalk_L"),Vector2.left);
            aimTree.AddChild(CopyClip(Rifle+"Aiming/R_AimWalk_FR.fbx","AimWalk_R"),Vector2.right);
            var aim=State(baseMachine,"AimGround",aimTree);
            aim.speedParameter="AimMotionRate";aim.speedParameterActive=true;
            var jump=State(baseMachine,"JumpStart",CopyClip(Female+"Jumps/Jump/R_Jump_AirR.fbx","JumpStart"));
            jump.speed=3;
            var air=State(baseMachine,"InAir",CopyClip(Female+"Jumps/Jump/R_Jump_AirL.fbx","InAir"));
            var landingTree=Tree(controller,"Landing",BlendTreeType.Simple1D,"Speed","Speed");
            landingTree.AddChild(CopyClip(Female+"Jumps/Land/R_Land_2h.fbx","Land"),0);
            landingTree.AddChild(CopyClip(Female+"Jumps/LandToRun/R_Land_ToRun1.fbx","LandWalk"),2);
            landingTree.AddChild(CopyClip(Female+"Jumps/LandToRun/R_Land_ToRun3.fbx","LandRun"),5.335f);
            var land=State(baseMachine,"Land",landingTree);
            Transition(ground,aim,0.12f).AddCondition(AnimatorConditionMode.If,0,"IsAiming");
            Transition(aim,ground,0.12f).AddCondition(AnimatorConditionMode.IfNot,0,"IsAiming");
            foreach(var state in new[]{ground,aim})
            {
                Transition(state,jump,0.05f).AddCondition(AnimatorConditionMode.If,0,"Jump");
                Transition(state,air,0.05f).AddCondition(AnimatorConditionMode.If,0,"FreeFall");
            }
            var intoAir=Transition(jump,air,0.06f);intoAir.hasExitTime=true;intoAir.exitTime=0.25f;
            Transition(air,land,0.07f).AddCondition(AnimatorConditionMode.If,0,"Grounded");
            var landAim=Transition(land,aim,0.1f);landAim.hasExitTime=true;landAim.exitTime=0.18f;landAim.AddCondition(AnimatorConditionMode.If,0,"IsAiming");
            var landFree=Transition(land,ground,0.1f);landFree.hasExitTime=true;landFree.exitTime=0.18f;landFree.AddCondition(AnimatorConditionMode.IfNot,0,"IsAiming");
            foreach(var pair in new[]{new {Name="TurnLeft",Path=Rifle+"Aiming/R_AimTurn_L90.fbx",Sign=-1},new{Name="TurnRight",Path=Rifle+"Aiming/R_AimTurn_R90.fbx",Sign=1}})
            {
                var turn=State(baseMachine,pair.Name,CopyClip(pair.Path,pair.Name));
                turn.speedParameter="TurnSpeed";turn.speedParameterActive=true;
                var enter=Transition(aim,turn,0.06f);
                enter.AddCondition(AnimatorConditionMode.Less,0.08f,"Speed");
                enter.AddCondition(pair.Sign<0?AnimatorConditionMode.Less:AnimatorConditionMode.Greater,pair.Sign*15,"Turn");
                var end=Transition(turn,aim,0.08f);
                end.AddCondition(pair.Sign<0?AnimatorConditionMode.Greater:AnimatorConditionMode.Less,pair.Sign*5,"Turn");
                Transition(turn,ground,0.08f).AddCondition(AnimatorConditionMode.IfNot,0,"IsAiming");
                Transition(turn,jump,0.05f).AddCondition(AnimatorConditionMode.If,0,"Jump");
            }
            var upper=new AnimatorStateMachine{name="UpperBodyAim"};
            AssetDatabase.AddObjectToAsset(upper,controller);
            var upperState=State(upper,"Hold",CopyClip(Rifle+"Aiming/R_AimIdle.fbx","AimIdle"));
            upper.defaultState=upperState;
            var layers=controller.layers.ToList();
            layers.Add(new AnimatorControllerLayer{name="UpperBodyAim",stateMachine=upper,defaultWeight=0,
                blendingMode=AnimatorLayerBlendingMode.Override,avatarMask=AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/_Project/Animations/Player/UpperBody.mask")});
            controller.layers=layers.ToArray();
            AimJumpAnimationSetup.ConfigureController(controller);
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip CopyClip(string source,string name)
        {
            string path=Folder+"/"+name+".anim";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing!=null)return existing;
            var copy=Object.Instantiate(LoadClip(source));copy.name=name;
            AnimationUtility.SetAnimationEvents(copy,Array.Empty<AnimationEvent>());
            AssetDatabase.CreateAsset(copy,path);return copy;
        }

        private static AnimationClip LoadClip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(x=>!x.name.StartsWith("__preview__"));
        private static AnimatorState State(AnimatorStateMachine machine,string name,Motion motion)
        {var s=machine.AddState(name);s.motion=motion;s.writeDefaultValues=true;return s;}
        private static AnimatorStateTransition Transition(AnimatorState source,AnimatorState target,float duration)
        {var t=source.AddTransition(target);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=duration;t.interruptionSource=TransitionInterruptionSource.SourceThenDestination;return t;}
        private static BlendTree Tree(AnimatorController controller,string name,BlendTreeType type,string x,string y)
        {var tree=new BlendTree{name=name,blendType=type,blendParameter=x,blendParameterY=y,useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);return tree;}
        private static Transform Child(Transform parent,string name)
        {var t=parent.Find(name);if(t!=null)return t;var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        private static void Arm(Transform parent,string name,Animator animator,HumanBodyBones root,HumanBodyBones mid,HumanBodyBones tip,Transform target,Transform hint)
        {
            var node=Child(parent,name);var c=node.GetComponent<StableArmIKConstraint>()??node.gameObject.AddComponent<StableArmIKConstraint>();
            var d=c.data;d.root=animator.GetBoneTransform(root);d.mid=animator.GetBoneTransform(mid);d.tip=animator.GetBoneTransform(tip);
            d.target=target;d.hint=hint;d.targetPositionWeight=d.targetRotationWeight=d.hintWeight=1;
            d.maintainTargetPositionOffset=d.maintainTargetRotationOffset=false;c.data=d;c.weight=1;
        }
        private static void SetObject(Object o,string property,Object value)
        {var so=new SerializedObject(o);Assign(so,property,value);so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Assign(SerializedObject so,string property,Object value)
        {var p=so.FindProperty(property);if(p==null)throw new InvalidOperationException(property);p.objectReferenceValue=value;}
        private static void EnsureFolder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;var parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        private static Vector3 FindMuzzleCenter(Mesh mesh)
        {
            using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
            using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
            {
                data[0].GetVertices(vertices);float maxZ=float.NegativeInfinity;
                for(int i=0;i<vertices.Length;i++)maxZ=Mathf.Max(maxZ,vertices[i].z);
                var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,maxZ);
                var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,maxZ);
                for(int i=0;i<vertices.Length;i++)if(vertices[i].z>maxZ-0.0001f)
                {min=Vector3.Min(min,vertices[i]);max=Vector3.Max(max,vertices[i]);}
                return (min+max)*0.5f;
            }
        }

        [Serializable] private sealed class Pose
        {
            public Vector3 RightPosition,LeftPosition,Shoulder,RightElbow,LeftElbow;
            public Quaternion RightRotation,LeftRotation;
            public Quaternion[] Spine;
        }
        private static Pose SamplePose(GameObject source,AnimationClip clip)
        {
            var scene=EditorSceneManager.NewPreviewScene();var graph=PlayableGraph.Create("AimCalibration");GameObject clone=null;
            try
            {
                var create=typeof(AimRepairDiagnostics).GetMethod("CreateClone",BindingFlags.Static|BindingFlags.NonPublic);
                clone=(GameObject)create.Invoke(null,new object[]{source,scene});
                var a=clone.GetComponent<Animator>();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);
                var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(playable);graph.Play();graph.Evaluate(0.1f);
                var right=a.GetBoneTransform(HumanBodyBones.RightHand);var left=a.GetBoneTransform(HumanBodyBones.LeftHand);
                return new Pose{RightPosition=right.position,LeftPosition=left.position,RightRotation=right.rotation,LeftRotation=left.rotation,
                    Shoulder=(a.GetBoneTransform(HumanBodyBones.RightUpperArm).position+a.GetBoneTransform(HumanBodyBones.LeftUpperArm).position)*0.5f,
                    RightElbow=a.GetBoneTransform(HumanBodyBones.RightLowerArm).position,LeftElbow=a.GetBoneTransform(HumanBodyBones.LeftLowerArm).position,
                    Spine=new[]{a.GetBoneTransform(HumanBodyBones.Spine).rotation,a.GetBoneTransform(HumanBodyBones.Chest).rotation,a.GetBoneTransform(HumanBodyBones.UpperChest).rotation}};
            }
            finally{if(graph.IsValid())graph.Destroy();if(clone!=null)Object.DestroyImmediate(clone);EditorSceneManager.ClosePreviewScene(scene);}
        }
        private static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
        private static float[] Q(Quaternion q)=>new[]{q.x,q.y,q.z,q.w};
    }
}