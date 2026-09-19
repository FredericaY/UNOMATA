using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    /// <summary>Audio view consumes evaluated motion phases; it never owns gameplay input or movement state.</summary>
    [DefaultExecutionOrder(750)]
    public class AudioBridge : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture()=>GameApp.Interface;
        [SerializeField] private Animator _playerAnimator;
        [SerializeField] private AudioClip[] _footstepClips;
        [SerializeField] private AudioClip _landingClip;
        [SerializeField] private AudioSource _footstepSrc;
        [SerializeField] private AudioSource _landSrc;
        [SerializeField] private PlayerAimPresentation _pose;
        [SerializeField] private FootstepPhaseProfile _phaseProfile;
        private const float MaxFootstepLength=0.313f;
        private readonly List<AnimatorClipInfo> _clips=new List<AnimatorClipInfo>(12);
        private AudioClip[] _filtered;
        private IArchitecture _architecture;
        private AudioModel _model;
        private IUnRegister _footSubscription,_landSubscription,_soundSubscription;
        private bool _phaseReady,_landingReady,_legacyLand;
        private int _stateHash,_leftCycle,_rightCycle,_lastLandingSequence;
        private float _leftPhase,_rightPhase,_previousTime;
        public int FootstepPlayCount { get; private set; }
        public int LandPlayCount { get; private set; }
        public int LastFootstepFrame { get; private set; }=-1;
        public int LastLandFrame { get; private set; }=-1;
        public bool FootstepIsPlaying=>_footstepSrc!=null&&_footstepSrc.isPlaying;
        public bool LandIsPlaying=>_landSrc!=null&&_landSrc.isPlaying;

        private void Awake()
        {
            var filtered=new List<AudioClip>();
            if(_footstepClips!=null)foreach(var clip in _footstepClips)
                if(clip!=null&&clip.length<=MaxFootstepLength)filtered.Add(clip);
            _filtered=filtered.Count>0?filtered.ToArray():_footstepClips;
        }

        private void OnEnable(){Bind();ResetPhases();}
        private void Bind()
        {
            if(_architecture!=null && ReferenceEquals(_architecture.GetModel<AudioModel>(),_model))return;
            Unsubscribe();
            _architecture=GameApp.Interface;
            _model=_architecture.GetModel<AudioModel>();
            _architecture.SendCommand(new ConfigureAudioAssetsCommand(_footstepClips,_landingClip));
            _footSubscription=_architecture.RegisterEvent<FootstepPlayedEvent>(e=>PlayFootstep());
            _landSubscription=_architecture.RegisterEvent<LandPlayedEvent>(e=>PlayLand());
            _soundSubscription=_architecture.RegisterEvent<SoundPlayedEvent>(OnSoundPlayed);
        }

        private void LateUpdate()
        {
            Bind();
            if(_pose!=null)
            {
                if(!_pose.IsInitialized || _pose.PoseFrame!=Time.frameCount)return;
                var motor=_pose.Motor;
                if(!_landingReady){_lastLandingSequence=motor.LandingSequence;_landingReady=true;}
                if(motor.LandingSequence!=_lastLandingSequence)
                {
                    _lastLandingSequence=motor.LandingSequence;
                    _architecture.SendCommand(new PlayLandCommand(_pose.transform.position));
                }
                if(!motor.IsGrounded || (motor.PlanarSpeed<0.05f&&Mathf.Abs(_pose.TurnBlend)<5))
                {_phaseReady=false;return;}
                _pose.GetMovementClips(_clips);
                UpdatePhases(_pose.MovementState,_pose.transform.position);
            }
            else if(_playerAnimator!=null)
            {
                var state=_playerAnimator.GetCurrentAnimatorStateInfo(0);
                bool land=state.IsName("JumpLand");
                if(land&&!_legacyLand)_architecture.SendCommand(new PlayLandCommand(_playerAnimator.transform.position));
                _legacyLand=land;
                if(!state.IsName("Idle Walk Run Blend")){_phaseReady=false;return;}
                _playerAnimator.GetCurrentAnimatorClipInfo(0,_clips);
                UpdatePhases(state,_playerAnimator.transform.position);
            }
        }

        private void UpdatePhases(AnimatorStateInfo state,Vector3 position)
        {
            Vector2 left=Vector2.zero,right=Vector2.zero;
            float total=0;bool loops=false;
            foreach(var ci in _clips)
            {
                if(ci.clip==null||ci.weight<=0.0001f)continue;
                float l,r;
                bool has=_phaseProfile!=null&&_phaseProfile.TryGet(ci.clip,out l,out r);
                if(!has)
                {
                    if(_pose!=null)continue;
                    if(ci.clip.name=="Walk"){l=0.2864f;r=0.7990f;}
                    else if(ci.clip.name=="Run"){l=0.2714f;r=0.7889f;}
                    else continue;
                }
                else _phaseProfile.TryGet(ci.clip,out l,out r);
                left+=Circle(l)*ci.weight;right+=Circle(r)*ci.weight;
                total+=ci.weight;loops|=ci.clip.isLooping;
            }
            if(total<=0.0001f){_phaseReady=false;return;}
            float lp=Phase(left),rp=Phase(right);
            float time=loops?state.normalizedTime:Mathf.Clamp01(state.normalizedTime);
            if(!_phaseReady||state.fullPathHash!=_stateHash||time<_previousTime)
            {
                _phaseReady=true;_stateHash=state.fullPathHash;
                _leftPhase=lp;_rightPhase=rp;
                _leftCycle=Mathf.FloorToInt(time-lp);_rightCycle=Mathf.FloorToInt(time-rp);
                _previousTime=time;return;
            }
            _leftPhase+=Mathf.DeltaAngle(_leftPhase*360,lp*360)/360;
            _rightPhase+=Mathf.DeltaAngle(_rightPhase*360,rp*360)/360;
            int lc=Mathf.FloorToInt(time-_leftPhase),rc=Mathf.FloorToInt(time-_rightPhase);
            if(lc>_leftCycle)_architecture.SendCommand(new PlayFootstepCommand(position));
            if(rc>_rightCycle)_architecture.SendCommand(new PlayFootstepCommand(position));
            _leftCycle=lc;_rightCycle=rc;_previousTime=time;
        }
        private static Vector2 Circle(float phase)=>new Vector2(Mathf.Cos(phase*Mathf.PI*2),Mathf.Sin(phase*Mathf.PI*2));
        private static float Phase(Vector2 circle)=>Mathf.Repeat(Mathf.Atan2(circle.y,circle.x)/(Mathf.PI*2),1);
        private void PlayFootstep()
        {
            if(!isActiveAndEnabled||_footstepSrc==null||_filtered==null||_filtered.Length==0)return;
            var clip=_filtered[Random.Range(0,_filtered.Length)];
            if(clip==null)return;
            _footstepSrc.clip=clip;_footstepSrc.Play();
            FootstepPlayCount++;LastFootstepFrame=Time.frameCount;
        }
        private void PlayLand()
        {
            if(!isActiveAndEnabled||_landSrc==null||_landingClip==null)return;
            _landSrc.PlayOneShot(_landingClip);LandPlayCount++;LastLandFrame=Time.frameCount;
        }
        private void OnSoundPlayed(SoundPlayedEvent e)
        {
            // Non-locomotion sound types remain reserved for subsequent gameplay changes.
        }
        private void ResetPhases(){_phaseReady=false;_landingReady=false;_legacyLand=false;}
        private void Unsubscribe()
        {
            _footSubscription?.UnRegister();_landSubscription?.UnRegister();_soundSubscription?.UnRegister();
            _footSubscription=_landSubscription=_soundSubscription=null;
        }
        private void OnDisable()
        {
            Unsubscribe();_architecture=null;_model=null;ResetPhases();
            if(_footstepSrc!=null)_footstepSrc.Stop();
            if(_landSrc!=null)_landSrc.Stop();
        }
        private void OnDestroy(){Unsubscribe();}
    }
}