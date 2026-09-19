using UnityEngine;

namespace Unomata.Gameplay
{
    [CreateAssetMenu(menuName = "UNOMATA/Combat/Enemy presentation")]
    public sealed class EnemyPresentationProfile : ScriptableObject
    {
        [SerializeField] private bool _animated = true;
        [SerializeField] private AnimationClip _idle;
        [SerializeField] private AnimationClip _hit;
        [SerializeField] private AnimationClip _death;
        [SerializeField, Min(0)] private float _transitionSeconds = .05f;
        [SerializeField, Min(.1f)] private float _completionGrace = .75f;
        public bool Animated => _animated;
        public AnimationClip Idle => _idle;
        public AnimationClip Hit => _hit;
        public AnimationClip Death => _death;
        public float TransitionSeconds => _transitionSeconds;
        public float HitTimeout => (_hit != null ? _hit.length : 0) + _transitionSeconds + _completionGrace;
        public float DeathTimeout => (_death != null ? _death.length : 0) + _transitionSeconds + _completionGrace;
        public bool TryValidate(out string error)
        {
            error = !CombatNumbers.IsNonNegative(_transitionSeconds) ||
                !CombatNumbers.IsFinite(_completionGrace) || _completionGrace <= 0 ? "Invalid animation timing." :
                !_animated ? null :
                _idle == null || _hit == null || _death == null ? "Missing Idle/Hit/Death clip." :
                !_idle.isLooping || _hit.isLooping || _death.isLooping ? "Invalid animation loop settings." :
                _idle.length <= 0 || _hit.length <= 0 || _death.length <= 0 ? "Empty animation clip." : null;
            return error == null;
        }
    }
}
