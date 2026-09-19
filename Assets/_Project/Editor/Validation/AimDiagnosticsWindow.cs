using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
using QFramework;
namespace Unomata.Editor.Validation
{
    public sealed class AimDiagnosticsWindow : EditorWindow
    {
        private PlayerAimPresentation _player;
        [MenuItem("UNOMATA/Validation/Aim Diagnostics")]
        public static void Open()=>GetWindow<AimDiagnosticsWindow>("瞄准诊断");
        private void OnEnable(){EditorApplication.update+=Repaint;SceneView.duringSceneGui+=DrawScene;}
        private void OnDisable(){EditorApplication.update-=Repaint;SceneView.duringSceneGui-=DrawScene;}
        private void OnGUI()
        {
            _player=(PlayerAimPresentation)EditorGUILayout.ObjectField("角色",_player,typeof(PlayerAimPresentation),true);
            if(GUILayout.Button("使用选中角色")&&Selection.activeGameObject!=null)_player=Selection.activeGameObject.GetComponentInParent<PlayerAimPresentation>();
            if(!EditorApplication.isPlaying||_player==null){EditorGUILayout.HelpBox("运行 SampleScene 并指定角色，Scene 视图会显示真实枪管、中心目标和握把误差。",MessageType.Info);return;}
            var s=GameApp.Interface.SendQuery(new AimSnapshotQuery(_player.ContextId,_player.PoseFrame));
            EditorGUILayout.LabelField("状态",s.Status+" / "+s.Failure);
            EditorGUILayout.LabelField("姿态 / 镜头帧",_player.PoseFrame+" / "+_player.CameraFrame);
            EditorGUILayout.LabelField("枪口误差",s.AimErrorDegrees.ToString("F5")+"°");
            EditorGUILayout.LabelField("右手 / 左手误差",(100*Vector3.Distance(_player.RightHand.position,_player.RightGrip.position)).ToString("F3")+" / "+(100*Vector3.Distance(_player.LeftHand.position,_player.LeftGrip.position)).ToString("F3")+" cm");
            EditorGUILayout.LabelField("握枪 / 运动混合",_player.AimWeight.ToString("F2")+" / "+_player.MotionBlend);
            EditorGUILayout.HelpBox("黄色：中心视线；青色：实际枪管；红色：遮挡。仅诊断，不产生射击。",MessageType.None);
        }
        private void DrawScene(SceneView scene)
        {
            if(!EditorApplication.isPlaying||_player==null||!_player.IsInitialized)return;
            var s=GameApp.Interface.SendQuery(new AimSnapshotQuery(_player.ContextId,_player.PoseFrame));
            if(s.Status==AimStatus.Inactive||s.Status==AimStatus.Invalid)return;
            if(Camera.main!=null){Handles.color=Color.yellow;Handles.DrawLine(Camera.main.transform.position,s.DesiredTarget);}
            Handles.color=Color.cyan;Handles.DrawLine(_player.Muzzle.position,_player.Muzzle.position+_player.Muzzle.forward*Vector3.Distance(_player.Muzzle.position,s.DesiredTarget));
            if(s.HasObstruction){Handles.color=Color.red;Handles.SphereHandleCap(0,s.ObstructionPoint,Quaternion.identity,0.08f,EventType.Repaint);}
        }
    }
}