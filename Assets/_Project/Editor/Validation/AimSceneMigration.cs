using System;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
using Object=UnityEngine.Object;

namespace Unomata.Editor.Validation
{
    public static class AimSceneMigration
    {
        public const string PlayerPrefab="Assets/_Project/Prefabs/Player/ShoulderAimPlayer.prefab";
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=AimRepairSetup.PrototypeScene||scene.isDirty)throw new InvalidOperationException("Load the saved validated prototype first.");
            var player=scene.GetRootGameObjects().Single(x=>x.name=="PlayerArmature");
            if(!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Player"))AssetDatabase.CreateFolder("Assets/_Project/Prefabs","Player");
            var view=player.GetComponent<PlayerAimPresentation>();
            var refs=new SerializedObject(view);
            var brain=refs.FindProperty("_brain").objectReferenceValue;
            var aimCamera=refs.FindProperty("_aimCamera").objectReferenceValue;
            var renderCamera=refs.FindProperty("_renderCamera").objectReferenceValue;
            foreach(var property in new[]{"_brain","_aimCamera","_renderCamera"})refs.FindProperty(property).objectReferenceValue=null;
            refs.ApplyModifiedPropertiesWithoutUndo();
            var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(player,PlayerPrefab,InteractionMode.AutomatedAction);
            refs=new SerializedObject(view);
            refs.FindProperty("_brain").objectReferenceValue=brain;
            refs.FindProperty("_aimCamera").objectReferenceValue=aimCamera;
            refs.FindProperty("_renderCamera").objectReferenceValue=renderCamera;
            refs.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/SampleScene.unity");
            var old=scene.GetRootGameObjects().Single(x=>x.name=="PlayerArmature");
            var position=old.transform.position;var rotation=old.transform.rotation;
            Object.DestroyImmediate(old);
            player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab),scene);
            player.name="PlayerArmature";player.transform.SetPositionAndRotation(position,rotation);
            var root=player.transform.Find("PlayerCameraRoot");
            var cameras=scene.GetRootGameObjects().Select(x=>x.GetComponent<CinemachineVirtualCamera>()).Where(x=>x!=null).ToArray();
            foreach(var camera in cameras)
            {
                camera.Follow=root;
                var follow=camera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if(follow!=null){follow.CameraCollisionFilter=1;follow.IgnoreTag="Player";follow.DampingFromCollision=0.15f;}
                EditorUtility.SetDirty(camera);
            }
            var main=scene.GetRootGameObjects().Select(x=>x.GetComponent<Camera>()).First(x=>x!=null);
            view=player.GetComponent<PlayerAimPresentation>();refs=new SerializedObject(view);
            refs.FindProperty("_brain").objectReferenceValue=main.GetComponent<CinemachineBrain>();
            refs.FindProperty("_aimCamera").objectReferenceValue=cameras.Single(x=>x.name=="PlayerAimCamera");
            refs.FindProperty("_renderCamera").objectReferenceValue=main;
            refs.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            var audio=scene.GetRootGameObjects().Select(x=>x.GetComponent<AudioBridge>()).Single(x=>x!=null);
            var audioRefs=new SerializedObject(audio);
            audioRefs.FindProperty("_playerAnimator").objectReferenceValue=player.GetComponent<Animator>();
            audioRefs.FindProperty("_pose").objectReferenceValue=view;
            audioRefs.FindProperty("_phaseProfile").objectReferenceValue=AssetDatabase.LoadAssetAtPath<FootstepPhaseProfile>(AimRepairSetup.Folder+"/Footsteps.asset");
            audioRefs.ApplyModifiedPropertiesWithoutUndo();
            foreach(var item in scene.GetRootGameObjects())if(item.name=="QFrameworkValidator")Object.DestroyImmediate(item);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return "Migrated SampleScene using "+PlayerPrefab+"; cameras and audio rebound; automatic HP validator removed.";
        }
    }
}