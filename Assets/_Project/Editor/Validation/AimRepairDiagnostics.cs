using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unomata.Editor.Validation
{
    /// <summary>Explicit, isolated pose sampling. Never changes or saves the source scene.</summary>
    public static class AimRepairDiagnostics
    {
        private const string Output = ".utmp/aim-repair";
        private const string AimFolder = "Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Aiming/";
        private static readonly string[] Clips =
        {
            "R_AimIdle", "R_AimWalk_F", "R_AimWalk_B", "R_AimWalk_FL",
            "R_AimWalk_FR", "R_AimWalk_BL", "R_AimWalk_BR", "R_AimJog",
            "R_AimTurn_L90", "R_AimTurn_R90"
        };

        public static string SampleClips(bool images = true)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before isolated sampling.");
            var sourceScene = SceneManager.GetActiveScene();
            var source = sourceScene.GetRootGameObjects().Single(x => x.name == "PlayerArmature");
            var scene = EditorSceneManager.NewPreviewScene();
            var results = new List<object>();
            try
            {
                Directory.CreateDirectory(Output + "/poses");
                foreach (var clipName in Clips)
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath(AimFolder + clipName + ".fbx")
                        .OfType<AnimationClip>().First(x => !x.name.StartsWith("__preview__"));
                    GameObject clone = null;
                    PlayableGraph graph = default;
                    try
                    {
                        clone = CreateClone(source, scene);
                        var animator = clone.GetComponent<Animator>();
                        graph = PlayableGraph.Create("AimRepairPose_" + clipName);
                        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var playable = AnimationClipPlayable.Create(graph, clip);
                        playable.SetApplyFootIK(false);
                        playable.SetApplyPlayableIK(false);
                        var output = AnimationPlayableOutput.Create(graph, "Pose", animator);
                        output.SetSourcePlayable(playable);
                        graph.Play();
                        var rows = new List<object>();
                        for (int i = 0; i < 17; i++)
                        {
                            double time = clip.length * i / 16.0;
                            playable.SetTime(time);
                            graph.Evaluate(0);
                            var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                            var lf = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                            var rf = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                            rows.Add(new
                            {
                                phase = i / 16f,
                                chestLocal = V(chest.localEulerAngles),
                                handRelativeToChest = V((Quaternion.Inverse(chest.rotation) * hand.rotation).eulerAngles),
                                leftFoot = V(clone.transform.InverseTransformPoint(lf.position)),
                                rightFoot = V(clone.transform.InverseTransformPoint(rf.position))
                            });
                            if (images && (i == 0 || i == 4 || i == 8 || i == 12))
                                Render(clone, scene, Output + "/poses/" + clipName + "_" + i.ToString("D2") + ".png",
                                    new Vector3(2.6f, 1.65f, 3.0f), new Vector3(0, 1.0f, 0));
                        }
                        var warning = new SerializedObject(animator).FindProperty("m_WarningMessage");
                        results.Add(new
                        {
                            clip = clipName, human = clip.humanMotion, length = clip.length,
                            averageSpeed = V(clip.averageSpeed),
                            warning = warning == null ? "" : warning.stringValue, samples = rows
                        });
                    }
                    finally
                    {
                        if (graph.IsValid()) graph.Destroy();
                        if (clone != null) Object.DestroyImmediate(clone);
                    }
                }
                var report = new { sourceScene = sourceScene.path, sourceDirty = sourceScene.isDirty, clips = results };
                File.WriteAllText(Output + "/clip-sampling.json", JsonConvert.SerializeObject(report, Formatting.Indented));
                return JsonConvert.SerializeObject(new { count = results.Count, report = Output + "/clip-sampling.json", sourceDirty = sourceScene.isDirty });
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        public static string RenderWeapon()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Prefab/Prefab_Parts/Weapon_Rifle.prefab");
                var go = Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (var c in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(c);
                foreach (var c in go.GetComponentsInChildren<ParentConstraint>(true)) Object.DestroyImmediate(c);
                Directory.CreateDirectory(Output + "/weapon");
                Render(go, scene, Output + "/weapon/side.png", new Vector3(2, 0.1f, 0), Vector3.zero, true);
                Render(go, scene, Output + "/weapon/top.png", new Vector3(0, 2, 0.001f), Vector3.zero, true);
                var rows = go.GetComponentsInChildren<Transform>(true).Select(t => new
                {
                    name=t.name, parent=t.parent == null ? null : t.parent.name,
                    position=V(t.localPosition), rotation=V(t.localEulerAngles),
                    mesh=t.GetComponent<MeshFilter>() == null ? null : t.GetComponent<MeshFilter>().sharedMesh.name
                }).ToArray();
                File.WriteAllText(Output + "/weapon/transforms.json", JsonConvert.SerializeObject(rows, Formatting.Indented));
                return JsonConvert.SerializeObject(rows);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static GameObject CreateClone(GameObject source, Scene scene)
        {
            var clone = Object.Instantiate(source);
            clone.name = "AimRepairPoseClone";
            SceneManager.MoveGameObjectToScene(clone, scene);
            foreach (var motor in clone.GetComponentsInChildren<Unomata.Gameplay.PlayerMotor>(true)) Object.DestroyImmediate(motor);
            foreach (var c in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (c is RigBuilder builder) builder.Clear();
                Object.DestroyImmediate(c);
            }
            foreach (var c in clone.GetComponentsInChildren<ParentConstraint>(true)) Object.DestroyImmediate(c);
            foreach (var c in clone.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            var animator = clone.GetComponent<Animator>();
            foreach (var c in clone.GetComponentsInChildren<Animator>(true))
                if (c != animator) Object.DestroyImmediate(c);
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            animator.runtimeAnimatorController = null;
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.fireEvents = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            return clone;
        }

        private static void Render(GameObject subject, Scene scene, string file, Vector3 position, Vector3 target, bool orthographic = false)
        {
            var cameraGo = new GameObject("DiagnosticCamera");
            var lightGo = new GameObject("DiagnosticLight");
            RenderTexture rt = null;
            Texture2D texture = null;
            var old = RenderTexture.active;
            try
            {
                SceneManager.MoveGameObjectToScene(cameraGo, scene);
                SceneManager.MoveGameObjectToScene(lightGo, scene);
                var camera = cameraGo.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.16f, 0.18f, 0.23f);
                camera.transform.position = position;
                camera.transform.LookAt(target);
                camera.fieldOfView = 38f;
                camera.orthographic = orthographic;
                camera.orthographicSize = 0.65f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 30f;
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.3f;
                lightGo.transform.rotation = Quaternion.Euler(45, -30, 0);
                rt = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                texture = new Texture2D(768, 768, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
                texture.Apply();
                File.WriteAllBytes(file, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = old;
                if (texture != null) Object.DestroyImmediate(texture);
                if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(lightGo);
            }
        }

        private static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
    }
}