using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cinemachine;
using Newtonsoft.Json;
using QFramework;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using Unomata.Gameplay;
using Object=UnityEngine.Object;

namespace Unomata.Editor.Validation
{

    public sealed class AimRuntimeInputDriver
    {
        public GameObject Fixture { get; }
        public AimRuntimeInputDriver(GameObject fixture) { Fixture=fixture; }
        private sealed class Case
        {
            public string Name;
            public Vector2 Move;
            public bool Sprint,Jump,Drop;
            public bool Aim=true;
            public int Pattern;
            public float Pitch,Distance,YawRate;
        }
        [Serializable] public sealed class CaseResult
        {
            public string name;
            public int samples,invalid,frameMismatch,doubleEvaluation;
            public float maxRestrictedSpeed;
            public int unexpectedSprintClip,rawSprintChanges;
            public float maxAim,maxPublishedError,maxRightPosition,maxLeftPosition,maxRightRotation,maxLeftRotation;
            public float actualFps,maxSpeed;
            public int airSamples,steps,lands,airborneSteps,firstRecordingFrame,lastRecordingFrame;
            public float takeoffPoseDelta,maxEarlyKneeDrop,maxJumpHeight;
            public float maxYawRate;
            public bool passed;
            public string firstFailure;
            public List<JumpPoseSample> jumpPose=new List<JumpPoseSample>();
        }
        [Serializable] public sealed class JumpPoseSample
        {
            public float time,rootY,verticalVelocity,leftFootY,rightFootY,leftKnee,rightKnee,stateTime,nextTime,transition;
            public string state,next;public bool grounded,triggered;public int lands,frame,recordingFrame;
        }
        [Serializable] public sealed class RunResult
        {
            public string state="idle",mode,currentCase,error;
            public int targetFps,completed,total;
            public bool cleanupComplete;
            public List<CaseResult> cases=new List<CaseResult>();
        }

        private Vector2 _activeMove;private bool _activeAim,_activeSprint;
        private readonly List<AnimatorClipInfo> _sampleClips=new List<AnimatorClipInfo>();
        private readonly List<Case> _cases=new List<Case>();
        private readonly List<(GameObject Object,int Layer)> _layers=new List<(GameObject,int)>();
        private PlayerAimPresentation _view;
        private PlayerMotor _motor;
        private StarterAssetsInputs _input;
        private PlayerController _controller;
        private SAInputAdapter _adapter;
        private CharacterController _capsule;
        private Camera _camera,_inspectionCamera;
        private AudioBridge _audio;
        private int _stepStart,_landStart,_previousSteps;
        private float _previousYaw;
        private Material _gridMaterial;
        private Texture2D _gridTexture;
        private CinemachineBrain _brain;
        private IArchitecture _app;
        private PlayerInputModel _inputModel;
        private Transform _target;
        private bool _controllerEnabled,_adapterEnabled,_background,_running,_cleaning,_jumpSent,_record,_environmentOwned;
        private int _vSync,_frameRate,_groundMask,_index=-1,_previousEval,_previousFrame=-1,_recordIndex;
        private float _age,_duration,_startYaw,_startPitch,_jumpAge=-1;
        private const float Warmup=0.5f;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private double _sampleStart;
        private CaseResult _current;
        private string _folder;
        private RenderTexture _recordTexture;
        private Texture2D _recordPixels;
        public RunResult Result { get; private set; }

        public void Configure(string mode,int fps,bool record,bool resume=false)
        {
            if(!new[]{"smoke","showcase","feedback","turns","matrix","categories","sprint-rule","jump-baseline","jump-check"}.Contains(mode))throw new ArgumentException("Unknown validation mode.");
            BuildCases(mode);
            _folder=".utmp/aim-repair/"+mode+"-"+fps;
            Directory.CreateDirectory(_folder);
            int totalCases=_cases.Count;
            var retained=new List<CaseResult>();
            if(resume)
            {
                var prior=JsonConvert.DeserializeObject<RunResult>(File.ReadAllText(_folder+"/report.json"));
                if(prior.mode!=mode||prior.targetFps!=fps||prior.total!=totalCases)throw new InvalidOperationException("Resume matrix does not match.");
                var hashes=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(".utmp/aim-repair/final-version-hashes.json"));
                using(var sha=System.Security.Cryptography.SHA256.Create())
                foreach(var item in hashes)
                    if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(item.Key))).Replace("-","").ToLowerInvariant()!=item.Value)
                        throw new InvalidOperationException("Cannot retain results from a changed version: "+item.Key);
                retained=prior.cases.Where(x=>x.passed).ToList();
                if(retained.Any(x=>!_cases.Any(c=>c.Name==x.name)))throw new InvalidOperationException("Unknown retained case.");
                _cases.RemoveAll(c=>retained.Any(x=>x.name==c.Name));
            }
            Result=new RunResult{state="running",mode=mode,targetFps=fps,total=totalCases,cases=retained,completed=retained.Count};
            _view=SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(x=>x.GetComponentsInChildren<PlayerAimPresentation>()).Single();
            _motor=_view.Motor;_input=_motor.GetComponent<StarterAssetsInputs>();
            _controller=_motor.GetComponent<PlayerController>();_adapter=_motor.GetComponent<SAInputAdapter>();
            _capsule=_motor.GetComponent<CharacterController>();
            _camera=Camera.main;_brain=_camera.GetComponent<CinemachineBrain>();
            _audio=SceneManager.GetActiveScene().GetRootGameObjects().Select(x=>x.GetComponent<AudioBridge>()).FirstOrDefault(x=>x!=null);
            _app=GameApp.Interface;_inputModel=_app.GetModel<PlayerInputModel>();
            _startPosition=_motor.transform.position;_startRotation=_motor.transform.rotation;
            _startYaw=_motor.OrbitYaw;_startPitch=_motor.OrbitPitch;
            _controllerEnabled=_controller.enabled;_adapterEnabled=_adapter.enabled;
            _background=Application.runInBackground;_frameRate=Application.targetFrameRate;_vSync=QualitySettings.vSyncCount;
            _groundMask=((LayerMask)Get("_groundLayers")).value;
            _environmentOwned=true;
            _controller.enabled=false;_adapter.enabled=false;
            _view.SendMessage("OnApplicationFocus",true);
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=fps;
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var collider in root.GetComponentsInChildren<Collider>(true))
                {
                    if(collider.transform.IsChildOf(_motor.transform))continue;
                    if(_layers.Any(x=>x.Object==collider.gameObject))continue;
                    _layers.Add((collider.gameObject,collider.gameObject.layer));
                    collider.gameObject.layer=30;
                }
            Set("_groundLayers",(LayerMask)(1<<30));
            var target=new GameObject("AimValidationTarget");_target=target.transform;
            var targetCollider=target.AddComponent<BoxCollider>();targetCollider.size=new Vector3(500,500,0.02f);
            Physics.IgnoreCollision(_capsule,targetCollider,true);
            target.transform.position=new Vector3(0,0,500);
            _target.SetParent(Fixture.transform);
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
            _duration=mode=="smoke"?0.6f:3.0f;
            _record=record;
            if(record)
            {
                Directory.CreateDirectory(_folder+"/frames");
                _recordTexture=new RenderTexture(640,360,24);
                _recordPixels=new Texture2D(640,360,TextureFormat.RGB24,false);
                CreateInspectionView();
            }
            _running=true;
            NextCase();
        }

        private void BuildCases(string mode)
        {
            var directions=new[]{Vector2.up,new Vector2(1,1).normalized,Vector2.right,new Vector2(1,-1).normalized,
                Vector2.down,new Vector2(-1,-1).normalized,Vector2.left,new Vector2(-1,1).normalized};
            if(mode.StartsWith("jump-"))
            {
                foreach(bool aim in new[]{false,true})
                {
                    _cases.Add(new Case{Name=(aim?"Aim":"Free")+"IdleJump",Aim=aim,Jump=true,Distance=50});
                    _cases.Add(new Case{Name=(aim?"Aim":"Free")+"RunJump",Aim=aim,Move=Vector2.up,Sprint=true,Jump=true,Distance=50});
                    _cases.Add(new Case{Name=(aim?"Aim":"Free")+"SideJump",Aim=aim,Move=Vector2.right,Jump=true,Distance=50});
                    if(mode=="jump-check")_cases.Add(new Case{Name=(aim?"Aim":"Free")+"Drop",Aim=aim,Drop=true,Distance=50});
                }
                return;
            }
            if(mode=="smoke")
            {
                foreach(float distance in new[]{3f,200f})
                    foreach(float pitch in new[]{-30f,0f,35f,70f})
                        _cases.Add(new Case{Name="Idle d"+distance+" p"+pitch,Pitch=pitch,Distance=distance});
                for(int i=0;i<8;i++)
                    foreach(bool sprint in new[]{false,true})
                        _cases.Add(new Case{Name=(sprint?"Run":"Walk")+i,Move=directions[i],Sprint=sprint,Distance=50});
                _cases.Add(new Case{Name="JumpIdle",Jump=true,Distance=50});
                _cases.Add(new Case{Name="JumpMove",Jump=true,Move=Vector2.right,Sprint=true,Distance=50});
                return;
            }
            if(mode=="sprint-rule")
            {
                foreach(bool aim in new[]{true,false})for(int i=0;i<8;i++)
                    _cases.Add(new Case{Name=(aim?"AimShift":"FreeShift")+i,Aim=aim,Move=directions[i],Sprint=true,Distance=50});
                _cases.Add(new Case{Name="HeldShiftDirectionChanges",Pattern=5,Move=Vector2.up,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="AimToggleSide",Pattern=4,Move=Vector2.left,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="AimToggleBack",Pattern=4,Move=Vector2.down,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="AimJumpSideShift",Move=Vector2.left,Sprint=true,Jump=true,Distance=50});
                return;
            }
            if(mode=="turns")
            {
                _cases.Add(new Case{Name="TurnRight",Distance=50,YawRate=180});
                _cases.Add(new Case{Name="TurnLeft",Distance=50,YawRate=-180});
                _cases.Add(new Case{Name="TurnNearUp",Distance=3,Pitch=-30,YawRate=180});
                _cases.Add(new Case{Name="TurnNearDown",Distance=3,Pitch=70,YawRate=-180});
                return;
            }
            if(mode=="feedback")
            {
                _cases.Add(new Case{Name="LoweredIdle",Aim=false,Distance=50});
                _cases.Add(new Case{Name="LoweredRun",Aim=false,Move=Vector2.up,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="AimStartStop",Pattern=1,Distance=50});
                _cases.Add(new Case{Name="AimWalkRun",Pattern=2,Move=Vector2.up,Distance=50});
                _cases.Add(new Case{Name="AimReverse",Pattern=3,Move=Vector2.left,Distance=50});
                _cases.Add(new Case{Name="RunAimToggle",Pattern=4,Move=Vector2.up,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="RunAimToggleBack",Pattern=4,Move=Vector2.down,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="RunAimToggleSide",Pattern=4,Move=Vector2.left,Sprint=true,Distance=50});
                for(int i=0;i<8;i++)_cases.Add(new Case{Name="RunDirection"+i,Move=directions[i],Sprint=true,Distance=50});
                return;
            }
            if(mode=="showcase")
            {
                _cases.Add(new Case{Name="Idle",Distance=50});
                _cases.Add(new Case{Name="WalkLeft",Move=Vector2.left,Distance=50});
                _cases.Add(new Case{Name="RunLeft",Move=Vector2.left,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="RunBack",Move=Vector2.down,Sprint=true,Distance=50});
                _cases.Add(new Case{Name="JumpRight",Move=Vector2.right,Sprint=true,Jump=true,Distance=50});
                _cases.Add(new Case{Name="LookDown",Pitch=70,Distance=50});
                return;
            }
            if(mode=="categories")
            {
                foreach(float pitch in new[]{-30f,0f,35f,70f})_cases.Add(new Case{Name="IdleLimit "+pitch,Pitch=pitch,Distance=3});
                for(int i=0;i<8;i++)
                {
                    _cases.Add(new Case{Name="Walk"+i,Move=directions[i],Distance=50});
                    _cases.Add(new Case{Name="Run"+i,Move=directions[i],Sprint=true,Distance=3,Pitch=-30});
                    _cases.Add(new Case{Name="JumpMove"+i,Move=directions[i],Sprint=true,Jump=true,Distance=200,Pitch=70});
                }
                _cases.Add(new Case{Name="JumpIdle",Jump=true,Distance=3,Pitch=-30});
                _cases.Add(new Case{Name="ContinuousYaw",Distance=50,YawRate=180});
                return;
            }
            var distances=mode=="matrix"?new[]{3f,10f,50f,200f}:new[]{3f,200f};
            foreach(float distance in distances)
            foreach(float pitch in new[]{-30f,0f,35f,70f})
            {
                _cases.Add(new Case{Name="Idle d"+distance+" p"+pitch,Distance=distance,Pitch=pitch});
                for(int i=0;i<8;i++)
                foreach(bool sprint in new[]{false,true})
                    _cases.Add(new Case{Name=(sprint?"Run":"Walk")+i+" d"+distance+" p"+pitch,Move=directions[i],Sprint=sprint,Distance=distance,Pitch=pitch});
                _cases.Add(new Case{Name="JumpIdle d"+distance+" p"+pitch,Jump=true,Distance=distance,Pitch=pitch});
                for(int i=0;i<8;i++)
                    _cases.Add(new Case{Name="JumpMove"+i+" d"+distance+" p"+pitch,Move=directions[i],Sprint=(i%2==0),Jump=true,Distance=distance,Pitch=pitch});
            }
            _cases.Add(new Case{Name="ContinuousYaw",Distance=50,YawRate=180});
        }

        private void NextCase()
        {
            _index++;
            if(_index>=_cases.Count){Finish();return;}
            var c=_cases[_index];
            if(Result.mode=="matrix"||Result.mode=="categories")_duration=c.Jump?2f:c.Move==Vector2.zero?3f:1.4f;
            _age=0;_jumpAge=-1;_jumpSent=false;_previousFrame=-1;
            _current=new CaseResult{name=c.Name,firstRecordingFrame=_recordIndex};
            Result.currentCase=c.Name;
            _capsule.enabled=false;
            var delta=_startPosition-_motor.transform.position;
            _motor.transform.SetPositionAndRotation(_startPosition,Quaternion.identity);
            _capsule.enabled=true;
            Set("_verticalVelocity",-2f);Set("_jumpTimer",0f);Set("_grounded",true);Set("_wasOnGround",true);
            Set("_speed",0f);Set("_wasAiming",false);
            typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,0f);
            typeof(PlayerMotor).GetProperty("OrbitPitch").SetValue(_motor,c.Pitch);
            foreach(var vcam in SceneManager.GetActiveScene().GetRootGameObjects().Select(x=>x.GetComponent<CinemachineVirtualCamera>()).Where(x=>x!=null))
            {
                vcam.OnTargetObjectWarped(vcam.Follow,delta);
                vcam.PreviousStateIsValid=false;
            }
            _sampleStart=0;
            Physics.SyncTransforms();
        }

        public void Tick()
        {
            if(!_running)return;
            try
            {
                var c=_cases[_index];
                // This fixture owns synthetic input/focus; physical focus recovery is tested separately.
                _view.SendMessage("OnApplicationFocus",true);
                _age+=Time.deltaTime;
                bool jump=c.Jump && !_jumpSent && _age>=Warmup+0.05f;
                if(jump)_jumpSent=true;
                if(c.Drop && !_jumpSent && _age>=Warmup+0.05f)
                {
                    _jumpSent=true;_capsule.enabled=false;_motor.transform.position+=Vector3.up*2;_capsule.enabled=true;
                    Set("_verticalVelocity",0f);Set("_grounded",false);Set("_wasOnGround",false);
                    foreach(var camera in SceneManager.GetActiveScene().GetRootGameObjects().Select(x=>x.GetComponent<CinemachineVirtualCamera>()).Where(x=>x!=null))
                        camera.OnTargetObjectWarped(camera.Follow,Vector3.up*2);
                    Physics.SyncTransforms();
                }
                var move=c.Move;bool sprint=c.Sprint;float phase=Mathf.Max(0,_age-Warmup);
                if(c.Pattern==1)move=phase<0.5f?Vector2.zero:phase<1.2f?Vector2.up:phase<1.7f?Vector2.zero:phase<2.5f?Vector2.right:Vector2.zero;
                if(c.Pattern==2)sprint=phase>=1&&phase<2;
                if(c.Pattern==3)move=phase<1.5f?Vector2.left:Vector2.right;
                if(c.Pattern==5)move=phase<0.6f?Vector2.up:phase<1.2f?Vector2.left:phase<1.8f?Vector2.down:Vector2.up;
                bool aim=c.Pattern==4?(phase<0.7f||phase>=1.7f):c.Aim;
                _activeMove=move;_activeAim=aim;_activeSprint=sprint;
                _app.SendCommand(new SetAimStateCommand(aim));
                _app.SendCommand(new SetMoveInputCommand(move));
                _app.SendCommand(new SetSprintInputCommand(sprint));
                _app.SendCommand(new SetJumpInputCommand(jump));
                _input.move=move;_input.sprint=sprint;_input.jump=jump;
                _input.look=new Vector2(c.YawRate*(_motor.GetComponent<UnityEngine.InputSystem.PlayerInput>().currentControlScheme=="KeyboardMouse"?Time.deltaTime:1f),0);
            }
            catch(Exception ex){Fail(ex);}
        }

        private void OnCameraUpdated(CinemachineBrain brain)
        {
            if(!_running || brain!=_brain)return;
            _target.SetPositionAndRotation(_camera.transform.position+_camera.transform.forward*(_cases[_index].Distance+0.01f),_camera.transform.rotation);
            Physics.SyncTransforms();
        }

        public void Sample()
        {
            if(!_running)return;
            try
            {
                if(_age<Warmup)return;
                if(_sampleStart==0)
                {
                    _sampleStart=Time.realtimeSinceStartupAsDouble;_previousYaw=_motor.OrbitYaw;
                    _stepStart=_previousSteps=_audio==null?0:_audio.FootstepPlayCount;
                    _landStart=_audio==null?0:_audio.LandPlayCount;
                }
                var s=_view.Snapshot;
                bool restricted=_activeAim&&_activeMove.sqrMagnitude>0.0001f&&(_activeMove.y<=0||Mathf.Abs(_activeMove.x)>0.0001f);
                if(restricted)_current.maxRestrictedSpeed=Mathf.Max(_current.maxRestrictedSpeed,_motor.PlanarSpeed);
                if(_inputModel.Sprint.Value!=_activeSprint)_current.rawSprintChanges++;
                if(restricted && _view.AimWeight>0.999f)
                {
                    _view.GetMovementClips(_sampleClips);
                    if(_sampleClips.Any(x=>x.weight>0.0001f&&(x.clip.name=="Run"||x.clip.name.StartsWith("Run_"))))_current.unexpectedSprintClip++;
                }
                _current.samples++;
                _current.maxSpeed=Mathf.Max(_current.maxSpeed,_motor.PlanarSpeed);
                if(!_motor.IsGrounded)_current.airSamples++;
                if(_current.samples>1)_current.maxYawRate=Mathf.Max(_current.maxYawRate,Mathf.Abs(Mathf.DeltaAngle(_previousYaw,_motor.OrbitYaw))/Mathf.Max(Time.deltaTime,0.0001f));
                _previousYaw=_motor.OrbitYaw;
                if(_audio!=null)
                {
                    if(!_motor.IsGrounded)_current.airborneSteps+=_audio.FootstepPlayCount-_previousSteps;
                    _previousSteps=_audio.FootstepPlayCount;
                    _current.steps=_audio.FootstepPlayCount-_stepStart;_current.lands=_audio.LandPlayCount-_landStart;
                }
                var activeCase=_cases[_index];
                bool aiming=activeCase.Aim;
                bool expected;
                if(activeCase.Pattern==4)
                {
                    float phase=_age-Warmup;
                    aiming=phase<0.7f||phase>=1.7f;
                    float since=phase<0.7f?phase+Warmup:phase<1.7f?phase-0.7f:phase-1.7f;
                    expected=s.Status==(aiming?AimStatus.Ready:AimStatus.Inactive)||(since<0.35f&&s.Status==AimStatus.Transition);
                }
                else expected=s.Status==(aiming?AimStatus.Ready:AimStatus.Inactive);
                aiming &= activeCase.Pattern!=4 || s.Status==AimStatus.Ready;
                if(!expected)
                {
                    _current.invalid++;
                    if(_current.firstFailure==null)_current.firstFailure=s.Status+"/"+s.Failure+" left="+Vector3.Distance(_view.LeftHand.position,_view.LeftGrip.position)+" right="+Vector3.Distance(_view.RightHand.position,_view.RightGrip.position);
                }
                if(s.FrameId!=Time.frameCount || _view.PoseFrame!=Time.frameCount || _view.CameraFrame!=Time.frameCount || _motor.MovementFrame!=Time.frameCount)
                    _current.frameMismatch++;
                if(_previousFrame==Time.frameCount-1 && _view.EvaluationCount-_previousEval!=1)_current.doubleEvaluation++;
                if(Mathf.Abs(_view.AnimationTimeAfterRig-_view.AnimationTimeBeforeRig)>0.00001f)_current.doubleEvaluation++;
                _previousFrame=Time.frameCount;_previousEval=_view.EvaluationCount;
                if(aiming){_current.maxAim=Mathf.Max(_current.maxAim,s.AimErrorDegrees);_current.maxPublishedError=Mathf.Max(_current.maxPublishedError,AimGeometry.AngleDegrees(s.BarrelDirection,_view.Muzzle.forward));}
                _current.maxRightPosition=Mathf.Max(_current.maxRightPosition,Vector3.Distance(_view.RightHand.position,_view.RightGrip.position));
                if(aiming)_current.maxLeftPosition=Mathf.Max(_current.maxLeftPosition,Vector3.Distance(_view.LeftHand.position,_view.LeftGrip.position));
                _current.maxRightRotation=Mathf.Max(_current.maxRightRotation,Quaternion.Angle(_view.RightHand.rotation,_view.RightGrip.rotation));
                if(aiming)_current.maxLeftRotation=Mathf.Max(_current.maxLeftRotation,Quaternion.Angle(_view.LeftHand.rotation,_view.LeftGrip.rotation));
                if(Result.mode.StartsWith("jump-"))SampleJump();
                if(_record && (Result.mode.StartsWith("jump-")||_current.samples%2==0))Capture();
                if(_age>=Warmup+_duration)
                {
                    var c=_cases[_index];
                    _current.actualFps=(float)((_current.samples-1)/Math.Max(0.0001,Time.realtimeSinceStartupAsDouble-_sampleStart));
                    float limit=c.Move==Vector2.zero&&!c.Jump&&c.YawRate==0?0.1f:0.5f;
                    _current.passed=_current.invalid==0&&_current.frameMismatch==0&&_current.doubleEvaluation==0&&
                        _current.maxAim<=limit&&_current.maxPublishedError<=0.05f&&_current.maxRightPosition<=0.01f&&_current.maxLeftPosition<=0.01f&&
                        _current.maxRightRotation<=2&&_current.maxLeftRotation<=2;
                    bool permittedRun=c.Sprint&&(!c.Aim||(c.Move.y>0&&Mathf.Abs(c.Move.x)<0.0001f));
                    if(c.Move!=Vector2.zero)_current.passed &= _current.maxSpeed>=(permittedRun?5.335f:2f)*0.85f;
                    _current.passed &= _current.maxRestrictedSpeed<=2.002f && _current.rawSprintChanges==0 && _current.unexpectedSprintClip==0;
                    if(Result.mode!="smoke"&&c.Move!=Vector2.zero&&!c.Jump&&c.Pattern==0)_current.passed &= _current.steps>=2;
                    if(c.Jump||c.Drop)_current.passed &= _current.airSamples>0 && (Result.mode=="smoke"||_current.lands==1);
                    if(c.YawRate!=0)_current.passed &= _current.maxYawRate>=Mathf.Abs(c.YawRate)*0.95f && _current.steps>=2;
                    _current.passed &= _current.airborneSteps==0;
                    _current.passed &= _current.actualFps>=Result.targetFps*0.85f && _current.actualFps<=Result.targetFps*1.15f;
                    if(c.Pattern==0&&c.Move==Vector2.zero&&!c.Jump&&c.YawRate==0)_current.passed &= _current.steps==0;
                    if(Result.mode=="jump-check")
                    {
                        var frames=_current.jumpPose;_current.maxJumpHeight=frames.Max(x=>x.rootY);
                        if(c.Jump)
                        {
                            int launch=frames.FindIndex(x=>x.triggered);
                            bool firstPose=launch>0&&(frames[launch].state=="JumpStart"||(frames[launch].next=="JumpStart"&&frames[launch].transition>0));
                            if(launch>0)_current.takeoffPoseDelta=Mathf.Max(Mathf.Abs(frames[launch].leftKnee-frames[launch-1].leftKnee),Mathf.Abs(frames[launch].rightKnee-frames[launch-1].rightKnee));
                            for(int i=launch+1;i<frames.Count;i++)if(frames[i].time>=.08f&&frames[i].verticalVelocity>=0&&!frames[i].grounded)
                                _current.maxEarlyKneeDrop=Mathf.Max(_current.maxEarlyKneeDrop,frames[i-1].leftKnee-frames[i].leftKnee,frames[i-1].rightKnee-frames[i].rightKnee);
                            _current.passed &= firstPose&&frames.Count(x=>x.triggered)==1&&_current.maxEarlyKneeDrop<15&&_current.maxJumpHeight>1.05f&&_current.maxJumpHeight<1.25f;
                            if(c.Move==Vector2.zero)_current.passed &= _current.takeoffPoseDelta>5;
                        }
                        else _current.passed &= frames.All(x=>!x.triggered&&x.state!="JumpStart"&&x.next!="JumpStart"&&x.verticalVelocity<=0);
                    }
                    _current.lastRecordingFrame=_recordIndex-1;
                    Result.cases.Add(_current);Result.completed=Result.cases.Count;
                    Save();NextCase();
                }
            }
            catch(Exception ex){Fail(ex);}
        }

        private void SampleJump()
        {
            if(_motor.JumpTriggered)_jumpAge=_age;
            var animator=_motor.GetComponent<Animator>();
            var animation=(AnimatorControllerPlayable)typeof(PlayerAimPresentation).GetField("_animation",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(_view);
            var state=animation.GetCurrentAnimatorStateInfo(0);var next=animation.GetNextAnimatorStateInfo(0);
            var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            _current.jumpPose.Add(new JumpPoseSample{time=_jumpAge<0?_age-Warmup-0.05f:_age-_jumpAge,
                rootY=_motor.transform.position.y-_startPosition.y,verticalVelocity=_motor.VerticalVelocity,
                leftFootY=_motor.transform.InverseTransformPoint(left.position).y,rightFootY=_motor.transform.InverseTransformPoint(right.position).y,
                leftKnee=Knee(animator,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot),
                rightKnee=Knee(animator,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot),
                state=StateName(state),next=StateName(next),stateTime=state.normalizedTime,nextTime=next.normalizedTime,
                transition=animation.IsInTransition(0)?animation.GetAnimatorTransitionInfo(0).normalizedTime:-1,
                grounded=_motor.IsGrounded,triggered=_motor.JumpTriggered,lands=_current.lands,frame=Time.frameCount,recordingFrame=_recordIndex});
        }
        private static string StateName(AnimatorStateInfo state)
        {
            foreach(var name in new[]{"Ground","AimGround","JumpStart","InAir","Land","TurnLeft","TurnRight"})
                if(state.shortNameHash==Animator.StringToHash(name))return name;
            return state.shortNameHash.ToString();
        }
        private static float Knee(Animator animator,HumanBodyBones hip,HumanBodyBones knee,HumanBodyBones ankle)
        {var h=animator.GetBoneTransform(hip).position;var k=animator.GetBoneTransform(knee).position;var a=animator.GetBoneTransform(ankle).position;return 180-Vector3.Angle(h-k,a-k);}

        private void Capture()
        {
            var previousTarget=_camera.targetTexture;
            var previousActive=RenderTexture.active;
            try
            {
                _camera.targetTexture=_recordTexture;_camera.Render();RenderTexture.active=_recordTexture;
                _recordPixels.ReadPixels(new Rect(0,0,640,360),0,0);_recordPixels.Apply();
                File.WriteAllBytes(_folder+"/frames/"+_recordIndex.ToString("D5")+".jpg",_recordPixels.EncodeToJPG(80));
                _inspectionCamera.transform.position=_motor.transform.position+_motor.transform.rotation*new Vector3(2.6f,1.6f,3);
                _inspectionCamera.transform.LookAt(_motor.transform.position+Vector3.up);
                if(Result.mode.StartsWith("jump-"))
                {
                    var center=new Vector3(_motor.transform.position.x,_startPosition.y+1.5f,_motor.transform.position.z);
                    _inspectionCamera.transform.position=center+_motor.transform.right*5.3f;
                    _inspectionCamera.transform.LookAt(center);
                }
                _inspectionCamera.targetTexture=_recordTexture;_inspectionCamera.Render();RenderTexture.active=_recordTexture;
                _recordPixels.ReadPixels(new Rect(0,0,640,360),0,0);_recordPixels.Apply();
                File.WriteAllBytes(_folder+"/frames/front-"+(_recordIndex++).ToString("D5")+".jpg",_recordPixels.EncodeToJPG(80));
            }
            finally{_camera.targetTexture=previousTarget;RenderTexture.active=previousActive;}
        }

        private void CreateInspectionView()
        {
            var go=new GameObject("AimInspectionCamera");go.transform.SetParent(Fixture.transform);
            _inspectionCamera=go.AddComponent<Camera>();_inspectionCamera.enabled=false;
            _inspectionCamera.scene=SceneManager.GetActiveScene();_inspectionCamera.fieldOfView=38;
            _inspectionCamera.clearFlags=CameraClearFlags.SolidColor;_inspectionCamera.backgroundColor=new Color(0.16f,0.18f,0.23f);
            var grid=GameObject.CreatePrimitive(PrimitiveType.Plane);grid.name="AimInspectionGround";grid.transform.SetParent(Fixture.transform);
            grid.layer=30;grid.transform.position=new Vector3(_startPosition.x,0.005f,_startPosition.z);grid.transform.localScale=new Vector3(6.4f,1,6.4f);
            Object.Destroy(grid.GetComponent<Collider>());
            _gridTexture=new Texture2D(256,256,TextureFormat.RGB24,false);_gridTexture.filterMode=FilterMode.Point;_gridTexture.wrapMode=TextureWrapMode.Repeat;
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)pixels[y*256+x]=((x/4+y/4)%2)==0?new Color(0.4f,0.44f,0.49f):new Color(0.65f,0.68f,0.72f);
            _gridTexture.SetPixels(pixels);_gridTexture.Apply();
            _gridMaterial=new Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
            _gridMaterial.SetTexture("_BaseMap",_gridTexture);grid.GetComponent<Renderer>().sharedMaterial=_gridMaterial;
        }
        private object Get(string field)=>typeof(PlayerMotor).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(_motor);
        private void Set(string field,object value)=>typeof(PlayerMotor).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(_motor,value);
        private void Save()
        {
            var json=JsonConvert.SerializeObject(Result,Formatting.Indented);
            File.WriteAllText(_folder+"/report.json",json);
            if(Result.state!="running") { Directory.CreateDirectory(".utmp/aim-repair/history"); File.WriteAllText(".utmp/aim-repair/history/"+Result.mode+"-"+Result.targetFps+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json",json); }
        }
        private void Finish(){Result.state=Result.cases.All(x=>x.passed)?"passed":"failed";Cleanup();Save();}
        private void Fail(Exception ex){Result.state="failed";Result.error=ex.ToString();Cleanup();Save();}
        public void Cancel(){if(!_running)return;Result.state="cancelled";Cleanup();Save();}
        public void AbortSetup(Exception error)
        {
            if(Result==null)Result=new RunResult();Result.state="failed";Result.error=error.ToString();
            if(_environmentOwned)Cleanup();else{if(Fixture!=null)Object.Destroy(Fixture);Result.cleanupComplete=true;}
            if(_folder!=null)Save();
        }


        private void Cleanup()
        {
            if(_cleaning)return;_cleaning=true;_running=false;
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
            foreach(var item in _layers)if(item.Object!=null)item.Object.layer=item.Layer;
            _layers.Clear();
            if(_motor!=null)
            {
                Set("_groundLayers",(LayerMask)_groundMask);
                _capsule.enabled=false;
                var delta=_startPosition-_motor.transform.position;
                _motor.transform.SetPositionAndRotation(_startPosition,_startRotation);
                _capsule.enabled=true;
                Set("_verticalVelocity",-2f);Set("_speed",0f);
                typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_motor,_startYaw);
                typeof(PlayerMotor).GetProperty("OrbitPitch").SetValue(_motor,_startPitch);
                foreach(var vcam in SceneManager.GetActiveScene().GetRootGameObjects().Select(x=>x.GetComponent<CinemachineVirtualCamera>()).Where(x=>x!=null))vcam.OnTargetObjectWarped(vcam.Follow,delta);
                _input.move=_input.look=Vector2.zero;_input.jump=_input.sprint=false;
                if(_app!=null && ReferenceEquals(_app.GetModel<PlayerInputModel>(),_inputModel))
                    _app.SendCommand(new ResetPlayerInputCommand());
            }
            if(_controller!=null)_controller.enabled=_controllerEnabled;
            if(_adapter!=null)_adapter.enabled=_adapterEnabled;
            if(_view!=null)_view.SendMessage("OnApplicationFocus",Application.isFocused);
            Application.runInBackground=_background;
            Application.targetFrameRate=_frameRate;QualitySettings.vSyncCount=_vSync;
            if(Fixture!=null)Object.Destroy(Fixture);
            if(_recordTexture!=null){_recordTexture.Release();Object.Destroy(_recordTexture);}
            if(_recordPixels!=null)Object.Destroy(_recordPixels);
            if(_gridMaterial!=null)Object.Destroy(_gridMaterial);
            if(_gridTexture!=null)Object.Destroy(_gridTexture);
            Result.cleanupComplete=true;
            AimRuntimeValidation.Detach();
            _cleaning=false;
        }
    }
}