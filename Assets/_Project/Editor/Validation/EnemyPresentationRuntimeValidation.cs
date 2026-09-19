using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QFramework;
using TMPro;
using Unomata.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unomata.EditorValidation
{
    public static class EnemyPresentationRuntimeValidation
    {
        private static bool _running;
        private static string _state = "idle", _error;
        private static int _fps;
        private static long _sequence;
        private static Guid _context;
        private static readonly List<string> Checks = new List<string>();
        private static readonly List<string> Diagnostics = new List<string>();
        private static EnemySystem _system;
        private static Camera _camera;
        public static string Status() => JsonConvert.SerializeObject(new {state=_state,error=_error,checks=Checks.Count,fps=_fps});
        public static string Start(int fps = 60)
        {
            if (!EditorApplication.isPlaying || _running) throw new InvalidOperationException("Requires idle Play Mode.");
            _fps=fps; _state="running"; _error=null; Checks.Clear(); Diagnostics.Clear(); _running=true;
            Run(); return Status();
        }
        private static void Check(bool ok, string text)
        { if (!ok) throw new InvalidOperationException(text); Checks.Add(text); }
        private static async Task Frames(int count)
        {
            int target=Time.frameCount+count;
            double deadline=EditorApplication.timeSinceStartup+Math.Max(15,count/15d);
            while(Time.frameCount<target)
            {
                if(!EditorApplication.isPlaying || !_running) throw new OperationCanceledException();
                if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("No frame progression.");
                EditorApplication.QueuePlayerLoopUpdate(); await Task.Delay(5);
            }
        }
        private static async Task Seconds(float seconds) { await Frames(Mathf.CeilToInt(_fps*seconds)); }
        private static void Reset(EnemyController enemy) { enemy.enabled=false; enemy.enabled=true; }
        private static EnemyDamageResult Damage(EnemyController enemy,float damage) =>
            _system.ApplyDamage(enemy.Id,new ShotId(_context,++_sequence),damage,enemy.transform.position+Vector3.up);
        private static void Field(object instance,string field,object value) =>
            instance.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(instance,value);
        private static T Read<T>(object instance,string field) =>
            (T)instance.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(instance);
        private static void Log(string message,string stack,LogType type)
        { if(message.StartsWith("[EnemyPresentationView]")||message.StartsWith("[EnemyStatusView]")||message.StartsWith("[EnemyController]")) Diagnostics.Add(message); }
        private static async void Run()
        {
            bool background=Application.runInBackground;
            int rate=Application.targetFrameRate, vsync=QualitySettings.vSyncCount;
            EnemyController[] enemies=null;
            PlayerAimPresentation pose=null;
            Vector3 cameraPosition=default; Quaternion cameraRotation=default;
            bool poseEnabled=true, brainEnabled=true;
            Cinemachine.CinemachineBrain brain=null;
            GameObject wall=null, temp=null;
            EnemyPresentationProfile temporaryProfile=null;
            try
            {
                Directory.CreateDirectory(".utmp/enemy-presentation");
                Application.runInBackground=true; Application.targetFrameRate=_fps; QualitySettings.vSyncCount=0;
                var roots=SceneManager.GetActiveScene().GetRootGameObjects();
                enemies=roots.SelectMany(g=>g.GetComponentsInChildren<EnemyController>()).OrderBy(e=>e.transform.position.x).ToArray();
                Check(enemies.Length>=3,"multiple registered enemies");
                pose=roots.SelectMany(g=>g.GetComponentsInChildren<PlayerAimPresentation>()).Single();
                _camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single(c=>c.CompareTag("MainCamera"));
                cameraPosition=_camera.transform.position; cameraRotation=_camera.transform.rotation;
                poseEnabled=pose.enabled;
                brain=_camera.GetComponent<Cinemachine.CinemachineBrain>(); brainEnabled=brain.enabled;
                _system=GameApp.Interface.GetSystem<EnemySystem>();
                _context=Guid.NewGuid(); _sequence=0; _system.BeginShotContext(_context);
                Application.logMessageReceived+=Log;
                foreach(var e in enemies) Reset(e);
                await Seconds(.5f);
                var enemy=enemies[0]; var other=enemies[1];
                var view=enemy.GetComponent<EnemyPresentationView>();
                var ui=enemy.GetComponent<EnemyStatusView>();
                var animator=enemy.GetComponentInChildren<Animator>();
                Check(view.State==EnemyPresentationState.Idle && enemy.Snapshot.Hp==100,"initial idle and hp");
                Check(enemies.All(e=>e.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m!=null && m.shader!=null && m.shader.isSupported))),"supported renderer materials");
                var text=enemy.GetComponentInChildren<TMP_Text>(true);
                Check(text.textInfo!=null && text.textInfo.characterCount>0 && text.mesh.vertexCount>0,"actual TMP glyph mesh");
                Capture("idle");
                int frame=Time.frameCount; double start=Time.realtimeSinceStartupAsDouble;
                var position=enemy.transform.position;
                await Seconds(1);
                double actual=(Time.frameCount-frame)/(Time.realtimeSinceStartupAsDouble-start);
                Check(Math.Abs(actual-_fps)<_fps*.12,"actual frame rate within 12 percent: "+actual.ToString("0.00"));
                Check((enemy.transform.position-position).magnitude<.001f,"idle root does not drift");
                int hits=view.HitPlayCount;
                Damage(enemy,20);
                Check(view.State==EnemyPresentationState.Hit && view.HitPlayCount==hits+1,"nonlethal hit starts once");
                Damage(enemy,20);
                Check(view.HitPlayCount==hits+1 && Math.Abs(enemy.Snapshot.Hp-98)<.001f,"same animation hit keeps taking damage without restart");
                Check(other.Snapshot.Hp==100,"other target isolated");
                Check(Math.Abs(ui.Data.HpFraction-.98f)<.001f,"hp UI updates synchronously");
                await Seconds(.16f); Capture("hit");
                await Seconds(.6f);
                Check(view.State==EnemyPresentationState.Idle,"hit finishes and returns idle");
                hits=view.HitPlayCount; Damage(enemy,0);
                Check(view.State==EnemyPresentationState.Idle && view.HitPlayCount==hits,"zero damage no false hit");
                for(int i=0;i<8;i++){ Damage(enemy,20); await Seconds(.125f); }
                Check(view.HitPlayCount>hits && view.HitPlayCount<hits+8,"continuous hits progress rather than restart every shot");
                await Seconds(.6f);
                Check(view.State==EnemyPresentationState.Idle,"no queued hit tail");
                float[] factors={0,.5f,1,1.2f,1.6f};
                string[] labels={"RESIST 95%","RESIST 47.5%","RESIST 0%","VULNERABLE +20%","VULNERABLE +60%"};
                for(int i=0;i<factors.Length;i++)
                {
                    _system.SetHackFactor(enemy.Id,factors[i]); await Frames(2);
                    Check(ui.DisplayedLabel==labels[i],"live defense label "+i);
                    Check(other.Snapshot.HackFactor==other.Profile.Settings.HackFactor,"factor isolates neighbor "+i);
                }
                ui.enabled=false; Damage(enemy,1); ui.enabled=true; await Frames(2);
                Check(Math.Abs(ui.Data.HpFraction-enemy.Snapshot.Hp/100)<.001f,"late UI reads current hp");
                Damage(enemy,1);
                int deaths=view.DeathPlayCount;
                var death=Damage(enemy,10000);
                Check(death.Killed && view.State==EnemyPresentationState.Death,"death interrupts hit");
                Check(enemy.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled) && !ui.IsVisible,"death immediately disables collision and UI");
                GameApp.Interface.SendEvent(new EnemyDiedEvent(death));
                Check(view.DeathPlayCount==deaths+1,"duplicate death does not replay");
                await Seconds(.7f); Capture("death");
                Check(view.State==EnemyPresentationState.Death,"death remains visible during clip");
                await Seconds(3.5f);
                Check(view.State==EnemyPresentationState.Hidden && enemy.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"death finishes then hides");
                var oldId=enemy.Id; Reset(enemy);
                Check(enemy.Id!=oldId && enemy.Snapshot.Hp==100 && view.State==EnemyPresentationState.Idle,"new identity restores presentation");
                Damage(enemy,10000); await Seconds(.2f);
                var oldDeath=death; Reset(enemy);
                GameApp.Interface.SendEvent(new EnemyDiedEvent(oldDeath));
                await Seconds(4.2f);
                Check(view.State==EnemyPresentationState.Idle && enemy.Snapshot.Hp==100,"old death timer cannot hide reset target");
                enemy.enabled=false; view.enabled=false; ui.enabled=false;
                view.enabled=true; ui.enabled=true;
                Check(view.State==EnemyPresentationState.Hidden && !ui.IsVisible,"views before registration wait safely");
                enemy.enabled=true;
                Check(view.State==EnemyPresentationState.Idle && ui.Data.IsAlive,"registration reaches early views");
                for(int i=0;i<3;i++)
                {
                    enemy.enabled=false;
                    Check(!ui.IsVisible && view.State==EnemyPresentationState.Hidden,"controller disable clears views "+i);
                    enemy.enabled=true;
                    view.enabled=false; ui.enabled=false;
                    view.enabled=true; ui.enabled=true;
                    Check(view.State==EnemyPresentationState.Idle && ui.Data.IsAlive,"three component reenable "+i);
                }
                view.enabled=false; Damage(enemy,10000); view.enabled=true;
                Check(view.State==EnemyPresentationState.Hidden,"late presentation does not replay dead target");
                Reset(enemy);
                pose.enabled=false; brain.enabled=false;
                _camera.transform.position=enemy.transform.position+new Vector3(0,2.4f,-5);
                _camera.transform.LookAt(enemy.transform.position+Vector3.up*2);
                await Frames(3);
                Check(ui.IsVisible,"visible anchor in camera");
                Capture("status-close");
                _camera.transform.rotation=Quaternion.LookRotation(Vector3.back);
                await Frames(2); Check(!ui.IsVisible,"behind camera hidden");
                _camera.transform.LookAt(enemy.transform.position+Vector3.up*2);
                _camera.transform.position=enemy.transform.position+new Vector3(0,2.4f,-45);
                await Frames(2); Check(!ui.IsVisible,"distance limit");
                _camera.transform.position=enemy.transform.position+new Vector3(0,2.4f,-5);
                _camera.transform.LookAt(enemy.transform.position+Vector3.up*2);
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name="Enemy validation wall";
                wall.transform.position=enemy.transform.position+new Vector3(0,2.35f,-2);
                wall.transform.localScale=new Vector3(3,3,.2f);
                Physics.SyncTransforms(); await Frames(3);
                Check(!ui.IsVisible,"wall occludes status"); Capture("occluded");
                wall.GetComponent<Collider>().isTrigger=true;
                Physics.SyncTransforms(); await Frames(3);
                Check(ui.IsVisible,"trigger does not occlude");
                wall.GetComponent<Collider>().isTrigger=false;
                temp=new GameObject("Enemy visibility saturation fixture");
                var ignored=Read<Collider[]>(ui,"_playerColliders");
                var crowded=new List<Collider>(ignored);
                Vector3 rayStart=_camera.transform.position;
                Vector3 rayEnd=Read<Transform>(ui,"_anchor").position;
                for(int i=0;i<70;i++)
                {
                    var probe=GameObject.CreatePrimitive(PrimitiveType.Cube);
                    probe.transform.SetParent(temp.transform);
                    probe.transform.position=Vector3.Lerp(rayStart,rayEnd,.08f+i*.005f);
                    probe.transform.localScale=Vector3.one*.018f;
                    probe.GetComponent<Renderer>().enabled=false;
                    crowded.Add(probe.GetComponent<Collider>());
                }
                ui.enabled=false; Field(ui,"_playerColliders",crowded.ToArray()); ui.enabled=true;
                Physics.SyncTransforms(); await Frames(3);
                Check(!ui.IsVisible,"saturated ignored hits still find wall");
                wall.GetComponent<Collider>().isTrigger=true; Physics.SyncTransforms(); await Frames(3);
                Check(ui.IsVisible,"explicit self colliders ignored after saturation");
                ui.enabled=false; Field(ui,"_playerColliders",ignored); ui.enabled=true;
                Object.Destroy(temp); temp=null;
                Object.Destroy(wall); wall=null; await Frames(2);
                var camera=Read<Camera>(ui,"_camera");
                ui.enabled=false; Field(ui,"_camera",null); ui.enabled=true;
                Damage(enemy,20); await Frames(2);
                Check(!ui.IsVisible && enemy.Snapshot.Hp<100,"missing camera does not stop damage");
                ui.enabled=false; Field(ui,"_camera",camera); ui.enabled=true; await Frames(2);
                Check(ui.IsVisible && ui.Data.HpFraction<1,"repaired UI reads latest state");
                var actualCanvas=Read<Canvas>(ui,"_canvas");
                foreach(string field in new[]{"_anchor","_fill","_label","_canvas","_profile"})
                {
                    object saved=ui.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
                    ui.enabled=false; Field(ui,field,null); ui.enabled=true;
                    float beforeHp=enemy.Snapshot.Hp; Damage(enemy,2); await Frames(2);
                    Check(!actualCanvas.enabled && enemy.Snapshot.Hp<beforeHp,"missing "+field+" hides and preserves damage");
                    ui.enabled=false; Field(ui,field,saved); ui.enabled=true; await Frames(2);
                    Check(ui.IsVisible,"repair "+field+" recovers UI");
                }
                var originalProfile=Read<EnemyPresentationProfile>(view,"_profile");
                temporaryProfile=Object.Instantiate(originalProfile);
                Field(temporaryProfile,"_death",null);
                view.enabled=false; Field(view,"_profile",temporaryProfile); view.enabled=true;
                Damage(enemy,10000); await Frames(2);
                Check(view.State==EnemyPresentationState.Hidden && !enemy.GetComponent<Collider>().enabled,"missing death clip safe fallback");
                view.enabled=false; Field(view,"_profile",originalProfile); view.enabled=true;
                Object.Destroy(temporaryProfile); temporaryProfile=null; Reset(enemy);
                animator.speed=0;
                Damage(enemy,10000); await Seconds(originalProfile.DeathTimeout+.2f);
                Check(view.State==EnemyPresentationState.Hidden,"stalled animator finite deadline");
                animator.speed=1; Reset(enemy);
                ui.enabled=false; view.enabled=false;
                _system.Unregister(enemy.Id);
                ui.enabled=true; view.enabled=true; await Frames(2);
                Check(!ui.IsVisible && view.State==EnemyPresentationState.Hidden && !enemy.GetComponent<Collider>().enabled,"unregistered snapshot hides late view and disables collision");
                Reset(enemy);
                var aPosition=enemy.transform.position; var bPosition=other.transform.position;
                enemy.transform.position=new Vector3(30,0,5); other.transform.position=new Vector3(30,0,8);
                Physics.SyncTransforms();
                var physical=new UnityShotWorldQuery();
                var first=physical.Raycast(new Vector3(30,1,0),Vector3.forward,20,Physics.DefaultRaycastLayers);
                Check(first.HasHit && _system.ReadCollider(first.ColliderId).Id==enemy.Id,"nearest physical target owns hit");
                Damage(enemy,10000); Physics.SyncTransforms();
                var behind=physical.Raycast(new Vector3(30,1,0),Vector3.forward,20,Physics.DefaultRaycastLayers);
                Check(behind.HasHit && _system.ReadCollider(behind.ColliderId).Id==other.Id,"dying model does not block rear target");
                enemy.transform.position=aPosition; other.transform.position=bPosition; Physics.SyncTransforms(); Reset(enemy);
                Check(Diagnostics.Count==8,"exactly eight expected missing/fault diagnostics: "+Diagnostics.Count);
                _state="passed";
            }
            catch(Exception error) { _state="failed"; _error=error.ToString(); }
            finally
            {
                Application.logMessageReceived-=Log;
                if(wall!=null) Object.Destroy(wall);
                if(temp!=null) Object.Destroy(temp);
                if(temporaryProfile!=null) Object.Destroy(temporaryProfile);
                if(_system!=null) _system.ReleaseShotContext(_context);
                if(enemies!=null) foreach(var enemy in enemies) if(enemy!=null) Reset(enemy);
                if(_camera!=null) _camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
                if(brain!=null) brain.enabled=brainEnabled;
                if(pose!=null) pose.enabled=poseEnabled;
                Application.runInBackground=background; Application.targetFrameRate=rate; QualitySettings.vSyncCount=vsync;
                _running=false;
                var report=new {state=_state,error=_error,targetFps=_fps,assertions=Checks.Count,checks=Checks,expectedDiagnostics=Diagnostics};
                File.WriteAllText(".utmp/enemy-presentation/runtime-"+_fps+".json",JsonConvert.SerializeObject(report,Formatting.Indented));
            }
        }
        private static void Capture(string label)
        {
            if(_fps!=60) return;
            var previous=_camera.targetTexture;
            var active=RenderTexture.active;
            var target=RenderTexture.GetTemporary(1280,720,24);
            Texture2D texture=null;
            try
            {
                _camera.targetTexture=target; _camera.Render(); RenderTexture.active=target;
                texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
                File.WriteAllBytes(".utmp/enemy-presentation/"+label+".png",texture.EncodeToPNG());
            }
            finally
            {
                _camera.targetTexture=previous; RenderTexture.active=active;
                RenderTexture.ReleaseTemporary(target); if(texture!=null) Object.Destroy(texture);
            }
        }
    }
}
