using UnityEngine;

namespace Unomata.Gameplay
{
    public enum EnemyPresentationState { Hidden, Idle, Hit, Death }

    [DisallowMultipleComponent]
    public sealed class EnemyPresentationView : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemy;
        [SerializeField] private EnemyPresentationProfile _profile;
        [SerializeField] private Animator _animator;
        [SerializeField] private Renderer[] _renderers;
        private EnemyViewBinding _binding;
        private bool _valid, _reported;
        private float _deadline;
        private static readonly int IdleHash = Animator.StringToHash("Base Layer.Idle");
        private static readonly int HitHash = Animator.StringToHash("Base Layer.Hit");
        private static readonly int DeathHash = Animator.StringToHash("Base Layer.Death");
        public EnemyPresentationState State { get; private set; }
        public int HitPlayCount { get; private set; }
        public int DeathPlayCount { get; private set; }

        private void OnEnable()
        {
            _reported = false;
            string error = null;
            _valid = _enemy != null && _profile != null && _profile.TryValidate(out error) &&
                _renderers != null && _renderers.Length > 0;
            if (_valid)
                foreach (var visual in _renderers) if (visual == null) { _valid = false; error = "Missing Renderer."; }
            if (_valid && _profile.Animated)
            {
                _valid = _animator != null && _animator.runtimeAnimatorController != null &&
                    _animator.HasState(0, IdleHash) && _animator.HasState(0, HitHash) && _animator.HasState(0, DeathHash);
                if (!_valid) error = "Missing Animator or Idle/Hit/Death state.";
            }
            if (!_valid) { Report(error ?? "Missing enemy, profile or renderers."); Hide(); }
            if (_enemy != null)
                _binding = new EnemyViewBinding(_enemy, Rebind, damaged: Damaged, died: Died);
        }
        private void Rebind(EnemySnapshot state)
        {
            _deadline = 0;
            if (!_valid || !state.IsAlive) { Hide(); return; }
            SetVisible(true);
            State = EnemyPresentationState.Idle;
            if (_profile.Animated)
            {
                _animator.enabled = true;
                _animator.applyRootMotion = false;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator.Rebind();
                _animator.Play(IdleHash, 0, 0);
                _animator.Update(0);
            }
        }
        private void Damaged(EnemyDamageResult fact)
        {
            if (!_valid || !_profile.Animated || fact.Killed || fact.AppliedDamage <= 0 ||
                State != EnemyPresentationState.Idle) return;
            State = EnemyPresentationState.Hit;
            _deadline = Time.time + _profile.HitTimeout;
            _animator.CrossFadeInFixedTime(HitHash, _profile.TransitionSeconds, 0, 0);
            HitPlayCount++;
        }
        private void Died(EnemyDamageResult fact)
        {
            if (State == EnemyPresentationState.Death || State == EnemyPresentationState.Hidden) return;
            if (!_valid || !_profile.Animated) { Hide(); return; }
            State = EnemyPresentationState.Death;
            _deadline = Time.time + _profile.DeathTimeout;
            _animator.CrossFadeInFixedTime(DeathHash, _profile.TransitionSeconds, 0, 0);
            DeathPlayCount++;
        }
        private void Update()
        {
            if (_binding != null && !_binding.IsContextAlive)
            { _binding.Dispose(); _binding = null; Hide(); return; }
            if (State != EnemyPresentationState.Hit && State != EnemyPresentationState.Death) return;
            int hash = State == EnemyPresentationState.Hit ? HitHash : DeathHash;
            bool finished = _animator != null && _animator.isActiveAndEnabled &&
                !_animator.IsInTransition(0) && _animator.GetCurrentAnimatorStateInfo(0).fullPathHash == hash &&
                _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
            if (!finished && Time.time < _deadline) return;
            if (!finished) Report("Animation did not finish; applying finite presentation fallback.");
            if (State == EnemyPresentationState.Death) Hide();
            else
            {
                State = EnemyPresentationState.Idle;
                if (_animator != null && _animator.isActiveAndEnabled)
                    _animator.CrossFadeInFixedTime(IdleHash, _profile.TransitionSeconds, 0, 0);
            }
        }
        private void SetVisible(bool visible)
        {
            if (_renderers == null) return;
            foreach (var visual in _renderers) if (visual != null) visual.enabled = visible;
        }
        private void Hide()
        {
            State = EnemyPresentationState.Hidden; _deadline = 0;
            SetVisible(false);
            if (_animator != null) _animator.enabled = false;
        }
        private void Report(string message)
        {
            if (_reported) return;
            _reported = true;
            Debug.LogWarning("[EnemyPresentationView] " + message + " on " + name, this);
        }
        private void OnDisable() { _binding?.Dispose(); _binding = null; Hide(); }
        private void OnDestroy() { _binding?.Dispose(); _binding = null; }
    }
}
