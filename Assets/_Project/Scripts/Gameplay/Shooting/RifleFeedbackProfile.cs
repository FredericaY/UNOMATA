using UnityEngine;
namespace Unomata.Gameplay
{
    [CreateAssetMenu(menuName = "UNOMATA/Combat/Rifle feedback")]
    public sealed class RifleFeedbackProfile : ScriptableObject
    {
        [SerializeField] private GameObject _muzzlePrefab, _tracerPrefab, _impactPrefab, _deathPrefab;
        [SerializeField] private AudioClip[] _gunShots, _surfaceHits, _enemyHits;
        [SerializeField] private float _muzzleLifetime = .045f, _tracerLifetime = .045f, _impactLifetime = .6f, _deathLifetime = .8f;
        [SerializeField] private float _muzzleScale = .4f, _tracerWidth = .035f, _impactScale = .35f, _deathScale = 1.2f;
        [SerializeField] private int _muzzleCapacity = 4, _tracerCapacity = 4, _impactCapacity = 16, _deathCapacity = 8;
        [SerializeField, Range(0, 1)] private float _gunVolume = .28f, _surfaceVolume = .2f, _enemyVolume = .25f;
        [SerializeField] private int _audioVoices = 24;
        [SerializeField] private float _audioMinDistance = 2, _audioMaxDistance = 45;
        [SerializeField] private float _recoilDistance = .025f, _recoilRecovery = .09f;
        public GameObject MuzzlePrefab => _muzzlePrefab;
        public GameObject TracerPrefab => _tracerPrefab;
        public GameObject ImpactPrefab => _impactPrefab;
        public GameObject DeathPrefab => _deathPrefab;
        public float MuzzleLifetime => _muzzleLifetime;
        public float TracerLifetime => _tracerLifetime;
        public float ImpactLifetime => _impactLifetime;
        public float DeathLifetime => _deathLifetime;
        public float MuzzleScale => _muzzleScale;
        public float TracerWidth => _tracerWidth;
        public float ImpactScale => _impactScale;
        public float DeathScale => _deathScale;
        public int MuzzleCapacity => _muzzleCapacity;
        public int TracerCapacity => _tracerCapacity;
        public int ImpactCapacity => _impactCapacity;
        public int DeathCapacity => _deathCapacity;
        public float RecoilDistance => _recoilDistance;
        public float RecoilRecovery => _recoilRecovery;
        public CombatAudioSettings AudioSettings => new CombatAudioSettings(_gunShots, _surfaceHits, _enemyHits,
            _gunVolume, _surfaceVolume, _enemyVolume, _audioVoices, _audioMinDistance, _audioMaxDistance);
        public bool TryValidateEffects(out string error)
        {
            error = _muzzlePrefab == null || _tracerPrefab == null || _impactPrefab == null || _deathPrefab == null ? "Missing effect prefab." : null;
            if (error != null) return false;
            foreach (float value in new[] { _muzzleLifetime, _tracerLifetime, _impactLifetime, _deathLifetime,
                _muzzleScale, _tracerWidth, _impactScale, _deathScale, _recoilRecovery })
                if (!CombatNumbers.IsFinite(value) || value <= 0) { error = "Effect durations/scales must be finite and positive."; return false; }
            if (!CombatNumbers.IsNonNegative(_recoilDistance)) { error = "Invalid recoil distance."; return false; }
            foreach (int value in new[] { _muzzleCapacity, _tracerCapacity, _impactCapacity, _deathCapacity })
                if (value < 1 || value > 128) { error = "Effect capacities must be in [1,128]."; return false; }
            return true;
        }
    }
}
