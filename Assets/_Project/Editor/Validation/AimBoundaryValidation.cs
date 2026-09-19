using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cinemachine;
using Newtonsoft.Json;
using QFramework;
using StarterAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unomata.Gameplay;
using Object=UnityEngine.Object;

namespace Unomata.Editor.Validation
{
    public static class AimBoundaryValidation
    {
        private static string _state="idle",_error,_phase;
        private static int _targetFps=60,_startFrame;
        private static double _startTime;
        private static float _entryPeakSpeed,_entryMaxStep,_entryMinStep;
        private static readonly List<string> Checks=new List<string>();
        private static readonly List<string> ExpectedDiagnostics=new List<string>();
        private static bool _running,_cleaned;
        private static PlayerAimPresentation _view;
        private static PlayerMotor _motor;
        private static StarterAssetsInputs _input;
        private static IArchitecture _app;
        private static GameObject _fixture;
        private static Transform _target;
        private static Camera _camera;
        private static float _distance;
        public static string Status()=>JsonConvert.SerializeObject(new{state=_state,phase=_phase,error=_error,checks=Checks.Count,cleanupComplete=_cleaned});
        public static string Start(int targetFps=60){if(!EditorApplication.isPlaying||_running)throw new InvalidOperationException("Requires idle Play Mode.");_targetFps=targetFps;_startFrame=Time.frameCount;_startTime=Time.realtimeSinceStartupAsDouble;_entryPeakSpeed=_entryMaxStep=_entryMinStep=0;_running=true;_cleaned=false;_state="running";_error=null;Checks.Clear();ExpectedDiagnostics.Clear();Run();return Status();}
        public static void Cancel(){_running=false;}
        private static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);Checks.Add(label);}
        private static AimSnapshot Read()=>_app.GetModel<AimModel>().Snapshot;
        private static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        private static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
        private static async Task Frames(int n)
        {
            int frame=Time.frameCount+Mathf.CeilToInt(n*_targetFps/60f);double deadline=EditorApplication.timeSinceStartup+10;
            while(Time.frameCount<frame)
            {
                if(!_running||!EditorApplication.isPlaying)throw new OperationCanceledException();
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("No player frames.");
                await Task.Delay(10);
            }
        }
        private static void CameraUpdated(CinemachineBrain brain)
        {
            if(_target==null||brain.OutputCamera!=_camera)return;
            _target.SetPositionAndRotation(_camera.transform.position+_camera.transform.forward*(_distance+0.01f),_camera.transform.rotation);
            Physics.SyncTransforms();
        }
        private static async void Run()
        {
            bool background=Application.runInBackground;int fps=Application.targetFrameRate,vSync=QualitySettings.vSyncCount;
            PlayerController pc=null;SAInputAdapter adapter=null;bool pcEnabled=false,adapterEnabled=false;
            var layers=new List<(GameObject Object,int Layer)>();object profile=null,muzzle=null;int groundMask=0;
            Cinemachine3rdPersonFollow aimBody=null;LayerMask aimMask=0;int cameraMask=0;
            GameObject obstacle=null;Vector3 startPosition=default;Quaternion startRotation=default;float yaw=0,pitch=0;
            Application.LogCallback log=(message,stack,type)=>{if(message.StartsWith("[PlayerAimPresentation] Missing"))ExpectedDiagnostics.Add(message);};
            try
            {
                _view=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PlayerAimPresentation>()).Single();
                _motor=_view.Motor;_input=_motor.GetComponent<StarterAssetsInputs>();_app=GameApp.Interface;_camera=Camera.main;
                pc=_motor.GetComponent<PlayerController>();adapter=_motor.GetComponent<SAInputAdapter>();pcEnabled=pc.enabled;adapterEnabled=adapter.enabled;
                pc.enabled=false;adapter.enabled=false;_input.move=_input.look=Vector2.zero;_input.jump=_input.sprint=false;
                startPosition=_motor.transform.position;startRotation=_motor.transform.rotation;yaw=_motor.OrbitYaw;pitch=_motor.OrbitPitch;
                typeof(PlayerMotor).GetProperty("OrbitPitch").SetValue(_motor,0f);
                cameraMask=_camera.cullingMask;aimBody=((CinemachineVirtualCamera)Get(_view,"_aimCamera")).GetCinemachineComponent<Cinemachine3rdPersonFollow>();aimMask=aimBody.CameraCollisionFilter;
                profile=Get(_view,"_profile");muzzle=Get(_view,"_muzzle");groundMask=((LayerMask)Get(_motor,"_groundLayers")).value;
                Application.runInBackground=true;Application.targetFrameRate=_targetFps;QualitySettings.vSyncCount=0;
                foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Collider>(true))
                {if(c.transform.IsChildOf(_motor.transform)||layers.Any(x=>x.Object==c.gameObject))continue;layers.Add((c.gameObject,c.gameObject.layer));c.gameObject.layer=30;}
                Set(_motor,"_groundLayers",(LayerMask)(1<<30));
                _fixture=new GameObject("AimBoundaryValidation_Fixture");_target=new GameObject("BoundaryTarget").transform;_target.SetParent(_fixture.transform);
                var targetCollider=_target.gameObject.AddComponent<BoxCollider>();targetCollider.size=new Vector3(500,500,0.02f);
                Physics.IgnoreCollision(_motor.GetComponent<CharacterController>(),targetCollider,true);
                _distance=50;CinemachineCore.CameraUpdatedEvent.AddListener(CameraUpdated);Application.logMessageReceived+=log;
                _view.SendMessage("OnApplicationFocus",true);_app.SendCommand(new SetAimStateCommand(true));await Frames(35);
                _phase="safe geometry";Check(Read().Status==AimStatus.Ready,"baseline ready");
                _target.gameObject.SetActive(false);await Frames(4);
                var s=Read();Check(s.Status==AimStatus.Ready&&Mathf.Abs(Vector3.Distance(s.DesiredTarget,_camera.transform.position)-200)<0.1f,"no hit uses configured far point");
                _target.gameObject.SetActive(true);_distance=0.2f;await Frames(5);Check(Read().Status!=AimStatus.Ready,"target behind muzzle is not Ready");
                _distance=50;await Frames(5);Check(Read().Status==AimStatus.Ready,"near target recovery");
                obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="MuzzleObstruction";obstacle.transform.SetParent(_fixture.transform);
                obstacle.transform.localScale=Vector3.one*0.15f;obstacle.transform.position=_view.Muzzle.position+_view.Muzzle.forward*0.1f;
                Physics.IgnoreCollision(_motor.GetComponent<CharacterController>(),obstacle.GetComponent<Collider>(),true);Physics.SyncTransforms();await Frames(5);
                Check(Read().Status==AimStatus.Blocked,"actual muzzle path blocked");
                obstacle.transform.position=_view.Muzzle.position;Physics.SyncTransforms();await Frames(5);
                Check(Read().Status==AimStatus.Blocked,"actual muzzle internal origin blocked");
                Object.Destroy(obstacle);obstacle=null;await Frames(5);Check(Read().Status==AimStatus.Ready,"muzzle obstruction recovery");
                _phase="lifecycle";
                for(int i=0;i<3;i++)
                {
                    var old=_view.ContextId;_view.enabled=false;Check(Read().Status!=AimStatus.Ready,"disable invalidates snapshot "+i);
                    await Frames(3);_view.enabled=true;await Frames(25);
                    Check(_view.ContextId!=old&&Read().Status==AimStatus.Ready,"re-enable reads held aim and creates fresh context "+i);
                    _motor.gameObject.SetActive(false);Check(Read().Status!=AimStatus.Ready,"whole player disable invalidates "+i);await Frames(3);
                    _motor.gameObject.SetActive(true);pc.enabled=false;adapter.enabled=false;_view.SendMessage("OnApplicationFocus",true);
                    _app.SendCommand(new SetAimStateCommand(true));await Frames(25);Check(Read().Status==AimStatus.Ready,"whole player re-enable "+i);
                }
                _phase="configuration recovery";
                _view.enabled=false;Set(_view,"_muzzle",null);_view.enabled=true;await Frames(10);
                Check(!_view.IsInitialized&&Read().Status!=AimStatus.Ready,"missing muzzle fails safely");
                Check(ExpectedDiagnostics.Count==1,"missing reference reports once");
                _view.enabled=false;Set(_view,"_muzzle",muzzle);_view.enabled=true;await Frames(25);Check(Read().Status==AimStatus.Ready,"reference repair recovery");
                _phase="focus and transition";
                _view.SendMessage("OnApplicationFocus",false);Check(Read().Status!=AimStatus.Ready,"synthetic focus loss invalidates immediately");await Frames(3);
                _view.SendMessage("OnApplicationFocus",true);await Frames(25);Check(Read().Status==AimStatus.Ready,"synthetic focus recovery");
                for(int i=0;i<4;i++){_app.SendCommand(new SetAimStateCommand(false));await Frames(3);_app.SendCommand(new SetAimStateCommand(true));await Frames(3);}
                await Frames(21);Check(Read().Status==AimStatus.Ready,"rapid aim toggles settle within 0.35 seconds");
                _app.SendCommand(new SetAimStateCommand(false));await Frames(21);Check(Read().Status==AimStatus.Inactive&&_view.AimWeight==0,"aim exit settles within 0.35 seconds");
                _motor.transform.rotation=Quaternion.Euler(0,_motor.OrbitYaw+180,0);
                _app.SendCommand(new SetAimStateCommand(true));await Frames(21);
                Check(Read().Status==AimStatus.Ready&&Mathf.Abs(Mathf.DeltaAngle(_motor.transform.eulerAngles.y,_motor.OrbitYaw))<5,"180-degree entry settles within 0.35 seconds");
                _app.SendCommand(new SetAimStateCommand(false));await Frames(21);
                _motor.transform.rotation=Quaternion.Euler(0,_motor.OrbitYaw-150,0);
                float previousBody=_motor.transform.eulerAngles.y;
                MagicaCloth2.MagicaManager.UpdateMethod trackEntry=()=>{float now=_motor.transform.eulerAngles.y;float step=Mathf.DeltaAngle(previousBody,now);_entryMaxStep=Mathf.Max(_entryMaxStep,Mathf.Abs(step));_entryMinStep=Mathf.Min(_entryMinStep,step);_entryPeakSpeed=Mathf.Max(_entryPeakSpeed,Mathf.Abs(step)/Mathf.Max(Time.deltaTime,0.0001f));previousBody=now;};
                MagicaCloth2.MagicaManager.afterLateUpdateDelegate+=trackEntry;
                try{_input.look=new Vector2(180f/_targetFps,0);_app.SendCommand(new SetAimStateCommand(true));await Frames(21);}
                finally{_input.look=Vector2.zero;MagicaCloth2.MagicaManager.afterLateUpdateDelegate-=trackEntry;}
                Check(_entryPeakSpeed<1200&&_entryMinStep>=-0.1f&&Read().Status==AimStatus.Ready,"aim entry crossing opposite yaw has no interpolation reversal (speed="+_entryPeakSpeed+", maxStep="+_entryMaxStep+", minStep="+_entryMinStep+", state="+Read().Status+")");
                _phase="turn hysteresis";float body=_motor.transform.eulerAngles.y;
                typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,body+8);await Frames(10);
                Check(Mathf.Abs(Mathf.DeltaAngle(body,_motor.transform.eulerAngles.y))<0.1f&&Read().Status==AimStatus.Ready,"small turn keeps feet and accurate muzzle");
                typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,body+45);await Frames(15);
                Check(Mathf.Abs(Mathf.DeltaAngle(_motor.transform.eulerAngles.y,_motor.OrbitYaw))<=5.1f,"large turn settles within stop threshold");
                body=_motor.transform.eulerAngles.y;
                foreach(float offset in new[]{14f,13f,14.5f,12f,14f})
                {typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,body+offset);await Frames(3);Check(Mathf.Abs(Mathf.DeltaAngle(body,_motor.transform.eulerAngles.y))<0.1f,"subthreshold yaw does not chatter "+offset);}
                typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,179f);await Frames(40);
                typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,181f);await Frames(5);
                Check(Mathf.Abs(Mathf.DeltaAngle(_motor.transform.eulerAngles.y,181))<15&&Read().Status==AimStatus.Ready,"stable yaw wraps across 180 continuously");
                _phase="camera collision and visibility";
                var beforeCamera=_camera.transform.position;
                var anchor=_motor.transform.Find("PlayerCameraRoot").position;
                var outward=Vector3.ProjectOnPlane(beforeCamera-anchor,Vector3.up).normalized;
                obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="CameraWall";obstacle.transform.SetParent(_fixture.transform);
                obstacle.transform.SetPositionAndRotation(Vector3.Lerp(anchor,beforeCamera,0.65f),Quaternion.LookRotation(outward));
                obstacle.transform.localScale=new Vector3(4,4,0.15f);Physics.SyncTransforms();await Frames(35);
                Check(Vector3.Distance(_camera.transform.position,beforeCamera)>0.1f,"aim camera moves away from wall");
                Check(Vector3.Distance(obstacle.GetComponent<Collider>().ClosestPoint(_camera.transform.position),_camera.transform.position)>0.01f,"aim camera remains outside wall");
                _app.SendCommand(new SetAimStateCommand(false));await Frames(30);
                Check(Vector3.Distance(obstacle.GetComponent<Collider>().ClosestPoint(_camera.transform.position),_camera.transform.position)>0.01f,"follow camera remains outside wall");
                Object.Destroy(obstacle);obstacle=null;_app.SendCommand(new SetAimStateCommand(true));await Frames(35);
                Check(Read().Status==AimStatus.Ready,"camera collision recovery uses final camera");
                aimBody.CameraCollisionFilter=0;
                obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="CameraInternalOrigin";obstacle.transform.SetParent(_fixture.transform);
                obstacle.transform.position=_camera.transform.position;obstacle.transform.localScale=Vector3.one*0.8f;Physics.SyncTransforms();await Frames(3);
                Check(Read().Failure==AimFailure.CameraInsideObstacle,"actual camera internal origin rejected");
                Object.Destroy(obstacle);obstacle=null;aimBody.CameraCollisionFilter=aimMask;await Frames(25);Check(Read().Status==AimStatus.Ready,"camera internal origin recovery");
                _camera.cullingMask=0;int hiddenFrame=_view.PoseFrame;await Frames(12);
                Check(_view.PoseFrame>=hiddenFrame+Mathf.CeilToInt(10*_targetFps/60f)&&Read().Status==AimStatus.Ready,"hidden character continues pose and snapshot updates");
                _camera.cullingMask=cameraMask;await Frames(3);Check(Read().Status==AimStatus.Ready,"visibility restoration has no stale result");
                _phase="blocked movement";
                obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="MovementWall";obstacle.transform.SetParent(_fixture.transform);
                var travel=Quaternion.Euler(0,_motor.OrbitYaw,0)*Vector3.forward;
                obstacle.transform.SetPositionAndRotation(_motor.transform.position+travel*1.5f+Vector3.up,Quaternion.LookRotation(travel));
                obstacle.transform.localScale=new Vector3(3,3,0.2f);Physics.SyncTransforms();
                _app.SendCommand(new SetMoveInputCommand(Vector2.up));_input.move=Vector2.up;await Frames(90);
                Check(_motor.PlanarSpeed<0.05f&&_view.MotionBlend.magnitude<0.1f,"physical wall stops movement animation");
                var collisionAudio=SceneManager.GetActiveScene().GetRootGameObjects().Select(x=>x.GetComponent<AudioBridge>()).Single(x=>x!=null);
                int blockedSteps=collisionAudio.FootstepPlayCount;await Frames(20);Check(collisionAudio.FootstepPlayCount==blockedSteps,"physical wall has no false movement footsteps");
                _input.move=Vector2.zero;_app.SendCommand(new SetMoveInputCommand(Vector2.zero));Object.Destroy(obstacle);obstacle=null;await Frames(25);
                _phase="audio lifecycle";
                var audio=SceneManager.GetActiveScene().GetRootGameObjects().Select(x=>x.GetComponent<AudioBridge>()).Single(x=>x!=null);
                for(int i=0;i<3;i++)
                {
                    int count=audio.FootstepPlayCount;audio.enabled=false;_app.SendCommand(new PlayFootstepCommand(_motor.transform.position));
                    Check(audio.FootstepPlayCount==count&&!audio.FootstepIsPlaying&&!audio.LandIsPlaying,"disabled audio has no old callback "+i);
                    audio.enabled=true;_app.SendCommand(new PlayFootstepCommand(_motor.transform.position));
                    Check(audio.FootstepPlayCount==count+1,"re-enabled audio plays exactly once "+i);
                }
                Check((_targetFps==30||_targetFps==60||_targetFps==120),"supported measured frame-rate category");
                _state="passed";
            }
            catch(Exception ex){_state=ex is OperationCanceledException?"cancelled":"failed";_error=ex.ToString();}
            finally
            {
                _running=false;CinemachineCore.CameraUpdatedEvent.RemoveListener(CameraUpdated);Application.logMessageReceived-=log;
                if(_view!=null){_view.enabled=false;if(muzzle!=null)Set(_view,"_muzzle",muzzle);if(profile!=null)Set(_view,"_profile",profile);}
                foreach(var item in layers)if(item.Object!=null)item.Object.layer=item.Layer;
                if(_motor!=null)
                {
                    Set(_motor,"_groundLayers",(LayerMask)groundMask);var capsule=_motor.GetComponent<CharacterController>();capsule.enabled=false;
                    _motor.transform.SetPositionAndRotation(startPosition,startRotation);capsule.enabled=true;
                    typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,yaw);typeof(PlayerMotor).GetProperty("OrbitPitch").SetValue(_motor,pitch);
                    _input.move=_input.look=Vector2.zero;_input.jump=_input.sprint=false;_app.SendCommand(new ResetPlayerInputCommand());
                    _motor.gameObject.SetActive(true);
                }
                if(pc!=null)pc.enabled=pcEnabled;if(adapter!=null)adapter.enabled=adapterEnabled;
                if(_view!=null){_view.enabled=true;_view.SendMessage("OnApplicationFocus",Application.isFocused);}
                if(aimBody!=null)aimBody.CameraCollisionFilter=aimMask;if(_camera!=null)_camera.cullingMask=cameraMask;
                if(_fixture!=null)Object.Destroy(_fixture);
                Application.runInBackground=background;Application.targetFrameRate=fps;QualitySettings.vSyncCount=vSync;
                float actualFps=(float)((Time.frameCount-_startFrame)/Math.Max(0.001,Time.realtimeSinceStartupAsDouble-_startTime));
                if(_state=="passed"&&(actualFps<_targetFps*0.85f||actualFps>_targetFps*1.15f)){_state="failed";_error="Measured FPS outside target category: "+actualFps;}
                _cleaned=true;
                Directory.CreateDirectory(".utmp/aim-repair");File.WriteAllText(".utmp/aim-repair/boundary-report.json",JsonConvert.SerializeObject(new{state=_state,phase=_phase,error=_error,checks=Checks,targetFps=_targetFps,actualFps,entryPeakSpeed=_entryPeakSpeed,entryMaxStep=_entryMaxStep,entryMinStep=_entryMinStep,expectedDiagnostics=ExpectedDiagnostics,cleanupComplete=_cleaned},Formatting.Indented));
                File.Copy(".utmp/aim-repair/boundary-report.json",".utmp/aim-repair/boundary-"+_targetFps+".json",true);
            }
        }
    }
}