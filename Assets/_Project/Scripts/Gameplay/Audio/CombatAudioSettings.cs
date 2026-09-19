using UnityEngine;
namespace Unomata.Gameplay
{
    public sealed class CombatAudioSettings
    {
        private readonly AudioClip[][] _clips;
        private readonly float[] _volumes;
        public int VoiceCount { get; }
        public float MinDistance { get; }
        public float MaxDistance { get; }
        public CombatAudioSettings(AudioClip[] gun, AudioClip[] surface, AudioClip[] enemy,
            float gunVolume, float surfaceVolume, float enemyVolume, int voices, float minDistance, float maxDistance)
        {
            _clips = new[] { Copy(gun), Copy(surface), Copy(enemy) };
            _volumes = new[] { gunVolume, surfaceVolume, enemyVolume };
            VoiceCount = voices; MinDistance = minDistance; MaxDistance = maxDistance;
        }
        private static AudioClip[] Copy(AudioClip[] clips) => clips == null ? new AudioClip[0] : (AudioClip[])clips.Clone();
        private static int Index(SoundId id) => id == SoundId.GunShot ? 0 : id == SoundId.HitSurface ? 1 : id == SoundId.HitEnemy ? 2 : -1;
        public int ClipCount(SoundId id) { int i = Index(id); return i < 0 ? 0 : _clips[i].Length; }
        public AudioClip GetClip(SoundId id, int variant) { int i = Index(id); return i < 0 || _clips[i].Length == 0 ? null : _clips[i][variant % _clips[i].Length]; }
        public float Volume(SoundId id) { int i = Index(id); return i < 0 ? 0 : _volumes[i]; }
        public bool TryValidate(out string error)
        {
            error = null;
            if (VoiceCount < 1 || VoiceCount > 128) error = "Voice count must be in [1,128].";
            else if (!CombatNumbers.IsFinite(MinDistance) || MinDistance <= 0 ||
                !CombatNumbers.IsFinite(MaxDistance) || MaxDistance < MinDistance) error = "Invalid sound distance range.";
            for (int i = 0; error == null && i < _clips.Length; i++)
            {
                if (_clips[i].Length == 0) error = "Missing combat clip group " + i + ".";
                else foreach (var clip in _clips[i]) if (clip == null) { error = "Missing combat clip in group " + i + "."; break; }
                if (!CombatNumbers.IsFinite(_volumes[i]) || _volumes[i] < 0 || _volumes[i] > 1) error = "Combat volume must be in [0,1].";
            }
            return error == null;
        }
    }
}
