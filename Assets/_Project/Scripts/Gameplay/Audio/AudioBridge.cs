using UnityEngine;
using QFramework;

namespace Unomata.Gameplay
{
    /// <summary>
    /// 音频 MonoBehaviour 桥接层（QFramework IController）。
    ///
    /// 职责：
    ///   1. Awake() — 把 Inspector 上的 AudioClip 引用注入到 <see cref="AudioModel"/>
    ///   2. Start()  — 订阅 QF Event（SoundPlayedEvent，供未来枪声/命中音扩展）
    ///   3. Update() — 相位驱动脚步音：每帧读 Animator normalizedTime，越过相位阈值时触发
    ///   4. 落地检测：每帧检测 Animator 是否进入 JumpLand 状态，进入首帧触发落地音
    ///
    /// 相位阈值（B1c.1 程序化标定）：
    ///   Walk: LF=0.2864  RF=0.7990
    ///   Run:  LF=0.2714  RF=0.7889
    ///
    /// Inspector 配置要求：
    ///   _playerAnimator : PlayerArmature 上的 Animator
    ///   _footstepClips  : SA 10 段脚步 wav
    ///   _landingClip    : 落地 wav
    ///   _footstepSrc    : 脚步 AudioSource（loop=false, playOnAwake=false）
    ///   _landSrc        : 落地 AudioSource（loop=false, playOnAwake=false）
    /// </summary>
    public class AudioBridge : MonoBehaviour, IController
    {
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;

        // ── Inspector 引用 ────────────────────────────────────────
        [SerializeField] private Animator     _playerAnimator;
        [SerializeField] private AudioClip[]  _footstepClips;
        [SerializeField] private AudioClip    _landingClip;
        [SerializeField] private AudioSource  _footstepSrc;
        [SerializeField] private AudioSource  _landSrc;

        // ── 相位阈值常量（B1c.1 程序化标定） ──────────────────────
        // Walk clip normalizedTime 脚步相位
        private static readonly float[] WalkPhases = { 0.2864f, 0.7990f };
        // Run clip normalizedTime 脚步相位
        private static readonly float[] RunPhases  = { 0.2714f, 0.7889f };

        // 脚步音最大时长过滤：≤0.313s（排除与 Run 触发间距过近的长片段）
        private const float MaxFootstepLength = 0.313f;

        // Animator 状态 hash（避免每帧字符串比较）
        private static readonly int WalkRunBlendHash = Animator.StringToHash("Idle Walk Run Blend");
        private static readonly int JumpLandHash     = Animator.StringToHash("JumpLand");

        // ── 运行时状态 ────────────────────────────────────────────
        private AudioClip[] _filteredFootstepClips;
        private float       _prevNormalizedTime;
        private bool        _wasInLandState;
        private bool        _wasInWalkRunBlend;   // 上帧是否在 WalkRunBlend 状态

        // ── Unity 生命周期 ────────────────────────────────────────
        private void Awake()
        {
            // 注入 AudioModel
            var model = this.GetModel<AudioModel>();
            model.FootstepClips = _footstepClips;
            model.LandingClip   = _landingClip;

            // 预筛脚步音
            if (_footstepClips != null && _footstepClips.Length > 0)
            {
                var list = new System.Collections.Generic.List<AudioClip>();
                foreach (var c in _footstepClips)
                    if (c != null && c.length <= MaxFootstepLength) list.Add(c);
                _filteredFootstepClips = list.Count > 0 ? list.ToArray() : _footstepClips;
                Debug.Log($"[AudioBridge] 脚步音池: {_footstepClips.Length} → {_filteredFootstepClips.Length} 段 (≤{MaxFootstepLength}s)");
            }
            else
            {
                _filteredFootstepClips = _footstepClips;
            }
        }

        private void Start()
        {
            // 预留：通用音效 SoundPlayedEvent（枪声/命中音/UI 音 Phase B2/3 接入）
            this.RegisterEvent<SoundPlayedEvent>(OnSoundPlayed)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (_playerAnimator == null) return;

            UpdateFootstep();
            UpdateLanding();
        }

        // ── 相位驱动脚步音 ────────────────────────────────────────
        private void UpdateFootstep()
        {
            // 只在 WalkRunBlend 状态处理脚步
            var info = _playerAnimator.GetCurrentAnimatorStateInfo(0);
            bool inWalkRun = info.IsName("Idle Walk Run Blend");

            if (!inWalkRun)
            {
                _wasInWalkRunBlend = false;
                return;
            }

            float curr = info.normalizedTime % 1f;

            // 刚切入 WalkRunBlend（从其他状态过来）：重置 prev，跳过本帧相位检测，防误触发
            if (!_wasInWalkRunBlend)
            {
                _prevNormalizedTime = curr;
                _wasInWalkRunBlend  = true;
                return;
            }

            float prev = _prevNormalizedTime;
            _prevNormalizedTime = curr;

            // 获取主导 clip
            var clipInfos = _playerAnimator.GetCurrentAnimatorClipInfo(0);
            if (clipInfos == null || clipInfos.Length == 0) return;

            AnimatorClipInfo dominant = clipInfos[0];
            foreach (var ci in clipInfos)
                if (ci.weight > dominant.weight) dominant = ci;

            // 只有权重 > 0.5 的主导 clip 才触发
            if (dominant.weight < 0.5f) return;

            // 选相位表：只有 Walk 或 Run clip 才触发脚步音，Idle 跳过
            float[] phases;
            if (dominant.clip.name == "Run")        phases = RunPhases;
            else if (dominant.clip.name == "Walk")  phases = WalkPhases;
            else return; // Idle 或其他 clip，不触发脚步音

            // 检测是否越过任意相位（处理循环回绕）
            foreach (var phase in phases)
            {
                bool crossed;
                if (prev <= curr)
                    // 正常前进
                    crossed = prev < phase && curr >= phase;
                else
                    // 循环回绕（curr 跨过 1 归零）
                    crossed = prev < phase || curr >= phase;

                if (crossed)
                {
                    PlayFootstep();
                    break; // 同帧只触发一次
                }
            }
        }

        // ── 落地检测 ─────────────────────────────────────────────
        private void UpdateLanding()
        {
            var info = _playerAnimator.GetCurrentAnimatorStateInfo(0);
            bool inLandState = info.IsName("JumpLand");

            // 检测进入落地状态的首帧
            if (inLandState && !_wasInLandState)
                PlayLand();

            _wasInLandState = inLandState;
        }

        // ── 播放方法 ──────────────────────────────────────────────
        private void PlayFootstep()
        {
            if (_footstepSrc == null || _filteredFootstepClips == null || _filteredFootstepClips.Length == 0) return;
            var clip = _filteredFootstepClips[Random.Range(0, _filteredFootstepClips.Length)];
            _footstepSrc.clip = clip;
            _footstepSrc.Play();
        }

        private void PlayLand()
        {
            if (_landSrc == null || _landingClip == null) return;
            _landSrc.PlayOneShot(_landingClip);
        }

        // ── 通用音效（预留） ──────────────────────────────────────
        private void OnSoundPlayed(SoundPlayedEvent e)
        {
            // TODO B2a/B2b: 按 SoundId 分发到对应 AudioSource
        }
    }
}
