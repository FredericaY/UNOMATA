using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using Unomata.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unomata.EditorValidation
{
    public static class EnemyPresentationSavedSceneAudit
    {
        public static string Run()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
            var scene=SceneManager.GetActiveScene();
            var checks=new List<string>();
            Action<bool,string> check=(ok,name)=>{if(!ok)throw new InvalidOperationException(name); checks.Add(name);};
            bool sample=scene.path==EnemyPresentationSetup.SamplePath;
            check((sample||scene.path==EnemyPresentationSetup.SandboxPath)&&!scene.isDirty,"clean designated mech scene");
            var roots=scene.GetRootGameObjects();
            var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single(c=>c.CompareTag("MainCamera"));
            var enemies=roots.SelectMany(g=>g.GetComponentsInChildren<EnemyController>(true)).ToArray();
            check(enemies.Length==(sample?3:4),"expected number of mechs");
            foreach(var enemy in enemies)
            {
                check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject)==EnemyPresentationSetup.PrefabPath,enemy.name+" prefab");
                check(enemy.Id==Guid.Empty,enemy.name+" no saved runtime identity");
                check(enemy.Profile!=null&&enemy.Profile.Settings.TryValidate(out _),enemy.name+" business profile");
                check(enemy.transform.position.y==0,enemy.name+" ground origin");
                check(enemy.GetComponentsInChildren<Rigidbody>(true).Length==0,enemy.name+" no vendor physics driver");
                var colliders=enemy.GetComponentsInChildren<Collider>(true);
                check(colliders.Length==1&&colliders[0].enabled&&!colliders[0].isTrigger&&colliders[0].gameObject==enemy.gameObject,enemy.name+" one owned live root hit shape");
                var view=enemy.GetComponent<EnemyPresentationView>();
                var ui=enemy.GetComponent<EnemyStatusView>();
                check(view!=null&&view.enabled&&ui!=null&&ui.enabled,enemy.name+" active views");
                var serialized=new SerializedObject(view);
                check(serialized.FindProperty("_enemy").objectReferenceValue==enemy,enemy.name+" animation identity binding");
                var profile=(EnemyPresentationProfile)serialized.FindProperty("_profile").objectReferenceValue;
                check(profile!=null&&profile.TryValidate(out _),enemy.name+" valid animation profile");
                var animator=enemy.GetComponentInChildren<Animator>(true);
                check(animator!=null&&animator.avatar!=null&&animator.runtimeAnimatorController!=null&&!animator.applyRootMotion,enemy.name+" one animation owner without root motion");
                check(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController).StartsWith("Assets/_Project/"),enemy.name+" project animation controller");
                var uiData=new SerializedObject(ui);
                check(uiData.FindProperty("_enemy").objectReferenceValue==enemy&&uiData.FindProperty("_camera").objectReferenceValue==camera,enemy.name+" UI identity and scene camera");
                check(uiData.FindProperty("_anchor").objectReferenceValue!=null&&uiData.FindProperty("_canvas").objectReferenceValue!=null&&
                    uiData.FindProperty("_label").objectReferenceValue!=null&&uiData.FindProperty("_fill").objectReferenceValue!=null,enemy.name+" complete UI wiring");
                check(enemy.GetComponentsInChildren<Canvas>(true).Single().renderMode==RenderMode.WorldSpace,enemy.name+" world space UI");
                check(enemy.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),enemy.name+" UI never intercepts input");
                var text=enemy.GetComponentInChildren<TMP_Text>(true);
                check(text.font!=null&&text.font.material!=null&&text.font.material.shader.isSupported,enemy.name+" font and shader");
                foreach(var clip in new[]{profile.Idle,profile.Hit,profile.Death})
                {
                    string path=AssetDatabase.GetAssetPath(clip);
                    check(path.StartsWith("Assets/_Project/")&&File.Exists(path+".meta")&&clip.events.Length==0,enemy.name+" clean project clip "+clip.name);
                }
            }
            check(AssetDatabase.FindAssets("t:TMP_Settings").Length==1&&TMP_Settings.instance!=null,"unique resolvable TMP settings");
            var transforms=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            check(transforms.All(t=>t.GetComponents<Component>().All(c=>c!=null)),"no missing scripts");
            check(transforms.All(t=>!t.name.Contains("Runtime")&&!t.name.Contains("fixture")&&!t.name.Contains("validation")),"no runtime validation objects saved");
            check(roots.SelectMany(g=>g.GetComponentsInChildren<AudioListener>(true)).Count(a=>a.enabled)==1,"one audio listener");
            check(roots.SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Any(l=>l.type==LightType.Directional),"directional light");
            var report=new{state="passed",scene=scene.path,assertions=checks.Count,checks};
            Directory.CreateDirectory(".utmp/enemy-presentation");
            File.WriteAllText(".utmp/enemy-presentation/saved-"+scene.name+".json",JsonConvert.SerializeObject(report,Formatting.Indented));
            return JsonConvert.SerializeObject(new{report.state,report.scene,report.assertions});
        }
    }
}
