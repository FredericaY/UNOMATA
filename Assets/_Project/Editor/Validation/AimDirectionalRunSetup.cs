using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Unomata.Editor.Validation
{
    /// <summary>Current user rule: aim sprint is forward only; former directional Run experiments stay disconnected.</summary>
    public static class AimDirectionalRunSetup
    {
        public static string Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(AimRepairSetup.Folder+"/ShoulderAim.controller");
            var all=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)).OfType<BlendTree>().ToArray();
            var direction=all.Single(x=>x.name=="AimLocomotion");
            var walk=GetTree(controller,all,"AimWalkDirections");
            walk.blendType=BlendTreeType.SimpleDirectional2D;walk.blendParameter="MoveX";walk.blendParameterY="MoveY";
            walk.children=Array.Empty<ChildMotion>();
            walk.AddChild(Clip("AimWalk_F"),Vector2.up);walk.AddChild(Clip("AimWalk_B"),Vector2.down);
            walk.AddChild(Clip("AimWalk_L"),Vector2.left);walk.AddChild(Clip("AimWalk_R"),Vector2.right);
            var forward=GetTree(controller,all,"AimForwardSpeed");
            forward.blendType=BlendTreeType.Simple1D;forward.blendParameter="Gait";forward.useAutomaticThresholds=false;
            forward.children=Array.Empty<ChildMotion>();forward.AddChild(Clip("AimWalk_F"),0);forward.AddChild(Clip("Run"),1);
            direction.children=Array.Empty<ChildMotion>();direction.AddChild(Clip("AimIdle"),Vector2.zero);
            for(int i=0;i<8;i++)
            {
                float angle=i*45*Mathf.Deg2Rad;
                direction.AddChild(i==0?forward:walk,new Vector2(Mathf.Sin(angle),Mathf.Cos(angle)));
            }
            if(!controller.parameters.Any(x=>x.name=="Gait"))controller.AddParameter("Gait",AnimatorControllerParameterType.Float);
            var used=new HashSet<BlendTree>();
            foreach(var layer in controller.layers)Collect(layer.stateMachine,used);
            foreach(var tree in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)).OfType<BlendTree>())
                if(!used.Contains(tree))Object.DestroyImmediate(tree,true);
            foreach(var tree in used)EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            if(controller.animationClips.Any(x=>x.name.StartsWith("Run_")))throw new InvalidOperationException("Directional Run still referenced.");
            return "Forward-only aim sprint configured; directional Run clips are disconnected.";
        }
        private static AnimationClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AnimationClip>(AimRepairSetup.Folder+"/"+name+".anim");
        private static BlendTree GetTree(AnimatorController controller,BlendTree[] all,string name)
        {var tree=all.FirstOrDefault(x=>x.name==name);if(tree==null){tree=new BlendTree{name=name};AssetDatabase.AddObjectToAsset(tree,controller);}return tree;}
        private static void Collect(AnimatorStateMachine machine,HashSet<BlendTree> used)
        {foreach(var item in machine.states)Collect(item.state.motion,used);foreach(var item in machine.stateMachines)Collect(item.stateMachine,used);}
        private static void Collect(Motion motion,HashSet<BlendTree> used)
        {if(!(motion is BlendTree tree)||!used.Add(tree))return;foreach(var child in tree.children)Collect(child.motion,used);}
    }
}