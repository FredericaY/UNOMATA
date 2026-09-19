using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unomata.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unomata.EditorValidation
{
    public static class ShootingSceneSetup
    {
        public const string SettingsRoot = "Assets/_Project/Settings/Combat";
        public const string EffectsRoot = "Assets/_Project/VFX/Shooting";
        public const string PlayerPath = "Assets/_Project/Prefabs/Player/ShoulderAimPlayer.prefab";
        public const string EnemyPath = "Assets/_Project/Prefabs/Enemies/EnemyCapsule.prefab";
        public const string SandboxPath = "Assets/_Project/Scenes/Sandbox/Sandbox_Shooting.unity";

        [MenuItem("UNOMATA/Setup/Shooting scene")]
        public static void ConfigureMenu() { Debug.Log(Configure()); }

        public static string Configure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before setup.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != SandboxPath || scene.isDirty)
                throw new InvalidOperationException("Open clean Sandbox_Shooting for capsule setup. Use Enemy presentation setup for SampleScene.");
            if (LayerMask.NameToLayer("Enemy") < 0) throw new InvalidOperationException("Create the Enemy layer first.");
            Folder(SettingsRoot); Folder(EffectsRoot); Folder("Assets/_Project/Prefabs/Enemies");
            var rifle = Asset<RifleProfile>(SettingsRoot + "/Rifle.asset");
            var feedback = Asset<RifleFeedbackProfile>(SettingsRoot + "/RifleFeedback.asset");
            var muzzle = CopyEffect("vulcan_muzzle", "RifleMuzzle");
            var tracer = CopyEffect("vulcan_projectile", "RifleTracer");
            var impact = CopyEffect("vulcan_impact", "RifleImpact");
            SetObject(feedback, "_muzzlePrefab", muzzle); SetObject(feedback, "_tracerPrefab", tracer);
            SetObject(feedback, "_impactPrefab", impact); SetObject(feedback, "_deathPrefab", impact);
            const string audio = "Assets/ThirdParty/Audio/SciFiWeaponsBulletHell/AUDIO/Shoot/Rifle_Shotgun_Pistol/";
            SetArray(feedback, "_gunShots", Enumerable.Range(1, 3).Select(i =>
                Required<AudioClip>(audio + "SFX_SCIFI_WEAPON_Rifle_Shoot_" + i + ".wav")).ToArray());
            const string hits = "Assets/ThirdParty/VFX/SciFiEffects/Sci-Fi Effects/Sounds/";
            SetArray(feedback, "_surfaceHits", Enumerable.Range(1, 3).Select(i =>
                Required<AudioClip>(hits + "impact_projectile_" + i.ToString("000") + ".wav")).ToArray());
            SetArray(feedback, "_enemyHits", new[] { 1, 2, 4 }.Select(i =>
                Required<AudioClip>(hits + "impact_projectile_metal_" + i.ToString("000") + ".wav")).ToArray());
            if (!feedback.TryValidateEffects(out var error) || !feedback.AudioSettings.TryValidate(out error))
                throw new InvalidOperationException(error);

            var enemyProfiles = new EnemyProfile[3];
            float[] factors = { 0, .5f, 1.2f };
            for (int i = 0; i < factors.Length; i++)
            {
                enemyProfiles[i] = Asset<EnemyProfile>(SettingsRoot + "/Capsule" + i + ".asset");
                SetFloat(enemyProfiles[i], "_maxHp", 100);
                SetFloat(enemyProfiles[i], "_baseDamageReduction", .95f);
                SetFloat(enemyProfiles[i], "_hackFactor", factors[i]);
            }
            var capsuleMaterial = MaterialAsset("Capsule", new Color(.16f, .63f, .67f));
            var wallMaterial = MaterialAsset("Backstop", new Color(.12f, .16f, .21f));
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath) == null)
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                try
                {
                    capsule.name = "EnemyCapsule"; capsule.layer = LayerMask.NameToLayer("Enemy");
                    capsule.GetComponent<Renderer>().sharedMaterial = capsuleMaterial;
                    var controller = capsule.AddComponent<EnemyController>();
                    SetObject(controller, "_profile", enemyProfiles[0]);
                    SetArray(controller, "_colliders", capsule.GetComponentsInChildren<Collider>());
                    var view = capsule.AddComponent<EnemyPresentationView>();
                    SetObject(view, "_enemy", controller);
                    SetObject(view, "_profile", Required<EnemyPresentationProfile>(EnemyPresentationSetup.CapsulePresentationPath));
                    SetArray(view, "_renderers", capsule.GetComponentsInChildren<Renderer>());
                    PrefabUtility.SaveAsPrefabAsset(capsule, EnemyPath);
                }
                finally { Object.DestroyImmediate(capsule); }
            }
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                WirePlayer(prefabRoot, rifle, feedback);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }

            var player = scene.GetRootGameObjects().Single(go => go.name == "PlayerArmature");
            WirePlayer(player, rifle, feedback);
            var root = scene.GetRootGameObjects().SingleOrDefault(go => go.name == "ShootingRange");
            if (root == null) root = new GameObject("ShootingRange");
            Vector3[] positions = { new Vector3(-2.5f, 1, 8), new Vector3(0, 1, 12), new Vector3(2.5f, 1, 16) };
            for (int i = 0; i < positions.Length; i++)
            {
                string name = "Capsule H " + factors[i].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                var target = root.transform.Find(name);
                var go = target != null ? target.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(EnemyPath), scene);
                go.name = name; go.transform.SetParent(root.transform, false); go.transform.position = positions[i];
                var controller = go.GetComponent<EnemyController>();
                SetObject(controller, "_profile", enemyProfiles[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            }
            Wall(root.transform, "Backstop", new Vector3(0, 1.5f, 21), new Vector3(16, 3, .4f), wallMaterial);
            Wall(root.transform, "Near cover", new Vector3(-5, 1.2f, 5), new Vector3(2, 2.4f, .5f), wallMaterial);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("SampleScene save failed.");
            // Capsule setup operates only on its independent scene; never overwrite SampleScene.
            AssetDatabase.Refresh();

            var report = new
            {
                state = "configured", player = PlayerPath, enemy = EnemyPath, sandbox = SandboxPath,
                weapon = new { damage = rifle.Settings.Damage, rate = rifle.Settings.ShotsPerSecond, range = rifle.Settings.Range },
                factors,
                sounds = SoundAudit(feedback),
                effects = new[] { EffectAudit(muzzle), EffectAudit(tracer), EffectAudit(impact) },
                shootingAnimations = AnimationAudit()
            };
            Directory.CreateDirectory(".utmp/shooting");
            File.WriteAllText(".utmp/shooting/setup-report.json", JsonConvert.SerializeObject(report, Formatting.Indented));
            return JsonConvert.SerializeObject(new { report.state, report.weapon, targets = 3 });
        }

        private static void WirePlayer(GameObject player, RifleProfile rifle, RifleFeedbackProfile feedback)
        {
            var pose = player.GetComponent<PlayerAimPresentation>();
            if (pose == null) throw new InvalidOperationException("Player is missing the validated pose coordinator.");
            var shooting = GetOrAdd<ShootingController>(player);
            var effects = GetOrAdd<ShootingFeedbackView>(player);
            var audio = GetOrAdd<CombatAudioView>(player);
            SetObject(shooting, "_pose", pose); SetObject(shooting, "_profile", rifle);
            SetObject(effects, "_pose", pose); SetObject(effects, "_profile", feedback);
            SetObject(audio, "_pose", pose); SetObject(audio, "_profile", feedback);
            SetObject(pose, "_shootingFeedback", feedback);
            if (PrefabUtility.IsPartOfPrefabInstance(player))
                foreach (var component in new Component[] { pose, shooting, effects, audio })
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        private static T GetOrAdd<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var result = AssetDatabase.LoadAssetAtPath<T>(path);
            if (result != null) return result;
            result = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }
        private static T Required<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset " + path);
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            Folder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
        private static GameObject CopyEffect(string sourceName, string name)
        {
            string source = "Assets/ThirdParty/VFX/SciFiEffects/Sci-Fi Effects/Effects/Vulcan/" + sourceName + ".prefab";
            string path = EffectsRoot + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(source, path))
                throw new InvalidOperationException("Could not copy effect " + source);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var behaviour in contents.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour == null || behaviour.GetType().Namespace == "FORGE3D")
                        throw new InvalidOperationException("Unsupported/missing self-driven script in " + path);
                foreach (var collider in contents.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                foreach (var sourceAudio in contents.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(sourceAudio);
                foreach (var particle in contents.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particle.main; main.playOnAwake = false; main.loop = false;
                    main.stopAction = ParticleSystemStopAction.None; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                foreach (var light in contents.GetComponentsInChildren<Light>(true))
                { light.shadows = LightShadows.None; light.range = 2; light.intensity = 1; }
                foreach (var renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported || material.shader.name.Contains("InternalError"))
                            throw new InvalidOperationException("Unsupported material in " + path);
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            return Required<GameObject>(path);
        }
        private static Material MaterialAsset(string name, Color color)
        {
            string path = SettingsRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit missing.");
            material = new Material(shader) { color = color };
            material.SetFloat("_Smoothness", .3f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static void Wall(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            var existing = root.Find(name);
            var go = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(position, Quaternion.identity); go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
        internal static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field) ?? throw new InvalidOperationException("Missing field " + field);
            property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        internal static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetArray<T>(Object target, string field, T[] values) where T : Object
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static object[] SoundAudit(RifleFeedbackProfile profile)
        {
            var settings = profile.AudioSettings; var result = new List<object>();
            foreach (var id in new[] { SoundId.GunShot, SoundId.HitSurface, SoundId.HitEnemy })
                for (int i = 0; i < settings.ClipCount(id); i++)
                {
                    var clip = settings.GetClip(id, i);
                    result.Add(new { type = id.ToString(), path = AssetDatabase.GetAssetPath(clip), clip.length, clip.channels, clip.frequency });
                }
            return result.ToArray();
        }
        private static object EffectAudit(GameObject go) => new
        {
            path = AssetDatabase.GetAssetPath(go), particles = go.GetComponentsInChildren<ParticleSystem>(true).Length,
            renderers = go.GetComponentsInChildren<Renderer>(true).Length,
            shaders = go.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Select(m => m.shader.name).Distinct().ToArray()
        };
        private static object[] AnimationAudit()
        {
            const string root = "Assets/ThirdParty/Characters/Player/CombatGirls/RifleGirl/Animations/Aiming/";
            var clips = new List<object>();
            foreach (var name in new[] { "R_Shoot", "R_AimIdle_AutoShoot" })
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(root + name + ".fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
                clips.Add(new { path = root + name + ".fbx", clip.name, clip.length, clip.frameRate,
                    events = AnimationUtility.GetAnimationEvents(clip).Select(e => e.functionName).ToArray(),
                    curveCount = AnimationUtility.GetCurveBindings(clip).Length });
            return clips.ToArray();
        }
    }
}
