using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unomata.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unomata.EditorValidation
{
    public static class ShootingSavedSceneAudit
    {
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = SceneManager.GetActiveScene();
            var checks = new List<string>();
            Action<bool, string> check = (ok, name) => { if (!ok) throw new InvalidOperationException(name); checks.Add(name); };
            bool mechs = scene.path == EnemyPresentationSetup.SamplePath;
            check((mechs || scene.path == ShootingSceneSetup.SandboxPath) && !scene.isDirty, "clean saved designated combat scene");
            var roots = scene.GetRootGameObjects();
            var player = roots.SelectMany(r => r.GetComponentsInChildren<PlayerAimPresentation>()).Single();
            var shooting = player.GetComponent<ShootingController>();
            var effects = player.GetComponent<ShootingFeedbackView>();
            var audio = player.GetComponent<CombatAudioView>();
            check(shooting != null && shooting.enabled && effects != null && effects.enabled && audio != null && audio.enabled, "all combat components saved");
            foreach (var component in new Component[] { shooting, effects, audio })
            {
                var data = new SerializedObject(component);
                check(data.FindProperty("_pose").objectReferenceValue == player, component.GetType().Name + " correct pose reference");
                check(data.FindProperty("_profile").objectReferenceValue != null, component.GetType().Name + " profile saved");
            }
            check(shooting.Profile.Settings.TryValidate(out _), "rifle configuration valid");
            var feedback = (RifleFeedbackProfile)new SerializedObject(effects).FindProperty("_profile").objectReferenceValue;
            check(feedback.TryValidateEffects(out _) && feedback.AudioSettings.TryValidate(out _), "all effect and sound assets valid");
            check(new SerializedObject(player).FindProperty("_shootingFeedback").objectReferenceValue == feedback, "recoil profile matches effects");
            check(new SerializedObject(audio).FindProperty("_profile").objectReferenceValue == feedback, "audio profile matches effects");
            check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player.gameObject) == ShootingSceneSetup.PlayerPath, "project player prefab connected");
            var targets = roots.SelectMany(r => r.GetComponentsInChildren<EnemyController>(true)).ToArray();
            check(targets.Length == 3, mechs ? "three saved mech targets" : "three saved capsule targets");
            foreach (var target in targets)
            {
                check(target.Profile != null && target.Profile.Settings.TryValidate(out _), target.name + " valid profile");
                check(target.gameObject.layer == LayerMask.NameToLayer("Enemy") &&
                    (shooting.Profile.Settings.HitMask & (1 << target.gameObject.layer)) != 0, target.name + " target layer included");
                check(target.GetComponentsInChildren<Collider>().All(c => c.enabled && !c.isTrigger), target.name + " live colliders saved");
                check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target.gameObject) == (mechs ? EnemyPresentationSetup.PrefabPath : ShootingSceneSetup.EnemyPath), target.name + " designated project prefab connected");
            }
            check((shooting.Profile.Settings.HitMask & 1) != 0, "environment layer remains hittable");
            check(roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)).Count(l => l.enabled) == 1, "one audio listener");
            var locomotionAudio = roots.SelectMany(r => r.GetComponentsInChildren<AudioBridge>()).Single();
            check(locomotionAudio.GetComponents<AudioSource>().Length == 2, "original two locomotion audio sources retained");
            var transforms = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            check(transforms.All(t => t.GetComponents<Component>().All(c => c != null)), "no missing script slots");
            check(transforms.All(t => !t.name.Contains("Runtime") && !t.name.Contains("pooled") && !t.name.Contains("validation obstruction")), "no runtime effects or test objects saved");
            foreach (var asset in new UnityEngine.Object[] { shooting.Profile, feedback, feedback.MuzzlePrefab, feedback.TracerPrefab, feedback.ImpactPrefab })
            {
                string path = AssetDatabase.GetAssetPath(asset);
                check(path.StartsWith("Assets/_Project/") && File.Exists(path + ".meta"), "project asset and meta " + path);
            }
            check(File.Exists(ShootingSceneSetup.SandboxPath) && File.Exists(ShootingSceneSetup.SandboxPath + ".meta"), "independent shooting scene and meta");
            Directory.CreateDirectory(".utmp/shooting");
            var report = new { state = "passed", assertions = checks.Count, checks };
            File.WriteAllText(mechs ? ".utmp/enemy-presentation/saved-shooting-audit.json" : ".utmp/shooting/saved-scene-audit.json", JsonConvert.SerializeObject(report, Formatting.Indented));
            return JsonConvert.SerializeObject(new { report.state, report.assertions });
        }
    }
}
