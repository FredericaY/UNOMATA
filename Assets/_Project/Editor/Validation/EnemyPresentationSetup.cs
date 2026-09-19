using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using Unomata.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Unomata.EditorValidation
{
    public static class EnemyPresentationSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Enemies/MechDefender.prefab";
        public const string SandboxPath = "Assets/_Project/Scenes/Sandbox/Sandbox_EnemyPresentation.unity";
        public const string SamplePath = "Assets/_Project/Scenes/SampleScene.unity";
        public const string SettingsPath = "Assets/_Project/Settings/Combat/Enemies";
        private const string AnimPath = "Assets/_Project/Animations/Enemies/MechDefender";
        private const string SourceRoot = "Assets/ThirdParty/Characters/Enemy/MechPack";
        public const string CapsulePresentationPath = SettingsPath + "/CapsulePresentation.asset";

        [MenuItem("UNOMATA/Setup/Enemy presentation")]
        public static void Menu() { Debug.Log(Configure()); }

        public static string Configure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var current = SceneManager.GetActiveScene();
            if (current.path != SamplePath || current.isDirty)
                throw new InvalidOperationException("Requires clean saved SampleScene.");
            BuildAssets();
            WireScene(current, true);
            EditorSceneManager.SaveScene(current);
            var sandbox = EditorSceneManager.OpenScene(ShootingSceneSetup.SandboxPath);
            WireScene(sandbox, false);
            EditorSceneManager.SaveScene(sandbox);
            current = EditorSceneManager.OpenScene(SamplePath);
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxPath))
                EditorSceneManager.SaveScene(current, SandboxPath, true);
            sandbox = EditorSceneManager.OpenScene(SandboxPath);
            WireScene(sandbox, true);
            var range = sandbox.GetRootGameObjects().Single(g => g.name == "ShootingRange");
            if (range.transform.Find("Rear Defender") == null)
            {
                var rear = (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(PrefabPath), sandbox);
                rear.name = "Rear Defender"; rear.transform.SetParent(range.transform);
                rear.transform.position = new Vector3(0, 0, 18);
                Set(rear.GetComponent<EnemyController>(), "_profile", Required<EnemyProfile>(SettingsPath + "/Defender.asset"));
            }
            BindSceneCameras(sandbox);
            EditorSceneManager.SaveScene(sandbox);
            EditorSceneManager.OpenScene(SamplePath);
            AssetDatabase.SaveAssets();
            var result = new {state = "configured", prefab = PrefabPath, sample = SamplePath, sandbox = SandboxPath};
            Directory.CreateDirectory(".utmp/enemy-presentation");
            File.WriteAllText(".utmp/enemy-presentation/setup.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return JsonConvert.SerializeObject(result);
        }
        private static void BuildAssets()
        {
            Folder(SettingsPath); Folder(AnimPath);
            Folder("Assets/_Project/Settings/Resources");
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
            {
                var tmp = Asset<TMP_Settings>("Assets/_Project/Settings/Resources/TMP Settings.asset");
                Set(tmp, "m_defaultFontAsset", Required<TMP_FontAsset>("Assets/ThirdParty/UI/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset"));
                Set(tmp, "m_defaultFontSize", 36f);
                Set(tmp, "m_defaultAutoSizeMinRatio", .5f);
                Set(tmp, "m_defaultAutoSizeMaxRatio", 2f);
            }

            var sourceClips = AssetDatabase.LoadAllAssetsAtPath(SourceRoot + "/Animations/mech_defender@animations.FBX").OfType<AnimationClip>().ToArray();
            var clips = new AnimationClip[3];
            string[] names = {"idle_01", "damage_01", "death_01"};
            string[] states = {"Idle", "Hit", "Death"};
            for (int i = 0; i < clips.Length; i++)
            {
                string path = AnimPath + "/" + states[i] + ".anim";
                clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clips[i] == null)
                {
                    clips[i] = Object.Instantiate(sourceClips.Single(c => c.name == names[i]));
                    clips[i].name = states[i]; AnimationUtility.SetAnimationEvents(clips[i], Array.Empty<AnimationEvent>());
                    AssetDatabase.CreateAsset(clips[i], path);
                }
            }
            string controllerPath = AnimPath + "/MechDefender.controller";
            var animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (animatorController == null) animatorController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = animatorController.layers[0].stateMachine;
            for (int i = 0; i < states.Length; i++)
            {
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == states[i]) ?? machine.AddState(states[i]);
                state.motion = clips[i]; state.writeDefaultValues = true;
                if (i == 0) machine.defaultState = state;
            }
            EditorUtility.SetDirty(animatorController);
            var presentation = Asset<EnemyPresentationProfile>(SettingsPath + "/DefenderPresentation.asset");
            Set(presentation, "_animated", true); Set(presentation, "_idle", clips[0]);
            Set(presentation, "_hit", clips[1]); Set(presentation, "_death", clips[2]);
            var capsulePresentation = Asset<EnemyPresentationProfile>(CapsulePresentationPath);
            Set(capsulePresentation, "_animated", false);
            var status = Asset<EnemyStatusProfile>(SettingsPath + "/DefenderStatus.asset");
            var profile = Asset<EnemyProfile>(SettingsPath + "/Defender.asset");
            Set(profile, "_maxHp", 100f); Set(profile, "_baseDamageReduction", .95f); Set(profile, "_hackFactor", 0f);
            MigrateCapsulePrefab(capsulePresentation);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("MechDefender");
            SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                root.layer = LayerMask.NameToLayer("Enemy");
                var visual = Object.Instantiate(Required<GameObject>(SourceRoot + "/Prefabs/Animated/mech_defender.prefab"), root.transform);
                visual.name = "Visual";
                foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                foreach (var rigidbody in visual.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rigidbody);
                var animator = visual.GetComponent<Animator>();
                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                clips[0].SampleAnimation(visual, 0);
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = 2f / bounds.size.y;
                visual.transform.localScale *= scale;
                visual.transform.localPosition = Vector3.up * (-bounds.min.y * scale);
                var colliderRoot = root.AddComponent<CapsuleCollider>();
                colliderRoot.height = 2; colliderRoot.radius = .58f; colliderRoot.center = Vector3.up;
                var enemy = root.AddComponent<EnemyController>();
                Set(enemy, "_profile", profile); SetArray(enemy, "_colliders", new Collider[] {colliderRoot});
                var view = root.AddComponent<EnemyPresentationView>();
                Set(view, "_enemy", enemy); Set(view, "_profile", presentation); Set(view, "_animator", animator);
                SetArray(view, "_renderers", renderers);
                CreateUi(root, enemy, status);
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null) throw new InvalidOperationException("Failed to save enemy prefab.");
            }
            finally { Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
        }
        private static void CreateUi(GameObject root, EnemyController enemy, EnemyStatusProfile profile)
        {
            var anchor = new GameObject("StatusAnchor"); anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = Vector3.up * 2.35f;
            var ui = new GameObject("EnemyStatus", typeof(RectTransform), typeof(Canvas));
            ui.transform.SetParent(anchor.transform, false);
            var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = ui.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(240, 65);
            rect.localScale = Vector3.one * .006f;
            var track = new GameObject("HealthTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(ui.transform, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(.05f, .15f); trackRect.anchorMax = new Vector2(.95f, .35f);
            trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;
            track.GetComponent<Image>().color = new Color(.03f, .06f, .09f, .95f);
            var fill = new GameObject("Health", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var labelObject = new GameObject("Defense", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(ui.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, .4f); labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = Required<TMP_FontAsset>("Assets/ThirdParty/UI/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            label.fontSize = 22; label.alignment = TextAlignmentOptions.Center; label.enableWordWrapping = false;
            label.text = "RESIST 95%";
            foreach (var graphic in ui.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            var view = root.AddComponent<EnemyStatusView>();
            Set(view, "_enemy", enemy); Set(view, "_profile", profile);
            Set(view, "_anchor", anchor.transform); Set(view, "_canvas", canvas);
            Set(view, "_fill", fill.GetComponent<Image>()); Set(view, "_label", label);
        }
        private static void MigrateCapsulePrefab(EnemyPresentationProfile profile)
        {
            var root = PrefabUtility.LoadPrefabContents(ShootingSceneSetup.EnemyPath);
            try
            {
                var enemy = root.GetComponent<EnemyController>();
                var view = root.GetComponent<EnemyPresentationView>() ?? root.AddComponent<EnemyPresentationView>();
                Set(view, "_enemy", enemy); Set(view, "_profile", profile);
                SetArray(view, "_renderers", root.GetComponentsInChildren<Renderer>(true));
                PrefabUtility.SaveAsPrefabAsset(root, ShootingSceneSetup.EnemyPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void WireScene(Scene scene, bool useMechs)
        {
            var enemies = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EnemyController>(true)).ToArray();
            foreach (var enemy in enemies)
            {
                string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject);
                if (!useMechs)
                {
                    if (source != ShootingSceneSetup.EnemyPath) throw new InvalidOperationException("Unexpected target in shooting baseline: " + source);
                    continue;
                }
                if (source == PrefabPath) continue;
                if (source != ShootingSceneSetup.EnemyPath) throw new InvalidOperationException("Unexpected target: " + source);
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(PrefabPath), scene);
                replacement.name = enemy.name.Replace("Capsule", "Mech Defender");
                replacement.transform.SetParent(enemy.transform.parent, false);
                replacement.transform.position = new Vector3(enemy.transform.position.x, 0, enemy.transform.position.z);
                replacement.transform.rotation = enemy.transform.rotation;
                Set(replacement.GetComponent<EnemyController>(), "_profile", enemy.Profile);
                Object.DestroyImmediate(enemy.gameObject);
            }
            BindSceneCameras(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        private static void BindSceneCameras(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var camera = roots.SelectMany(g => g.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));
            var player = roots.SelectMany(g => g.GetComponentsInChildren<PlayerAimPresentation>(true)).Single();
            foreach (var status in roots.SelectMany(g => g.GetComponentsInChildren<EnemyStatusView>(true)))
            {
                Set(status, "_camera", camera);
                SetArray(status, "_playerColliders", player.GetComponentsInChildren<Collider>(true));
            }
        }
        public static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field) ?? throw new InvalidOperationException("Missing field " + field);
            if (value is bool b) property.boolValue = b;
            else if (value is float f) property.floatValue = f;
            else property.objectReferenceValue = (Object)value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        public static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        private static T Required<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing " + path);
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
