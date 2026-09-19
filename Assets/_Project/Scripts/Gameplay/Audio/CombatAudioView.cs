using System;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Unomata.Gameplay
{
    public sealed class CombatAudioView : MonoBehaviour, IController
    {
        [SerializeField] private RifleFeedbackProfile _profile;
        [SerializeField] private PlayerAimPresentation _pose;
        private IArchitecture _architecture;
        private AudioModel _model;
        private ShootingModel _shooting;
        private IUnRegister _subscription;
        private AudioSource[] _voices;
        private GameObject _voiceRoot;
        private SoundId[] _kinds;
        private double[] _starts;
        private int _variant;
        private bool _focused, _reported;
        private Guid _aimContext;
        public int PlayCount { get; private set; }
        public int VoiceCount => _voices == null ? 0 : _voices.Length;
        public int PlayingCount
        {
            get { int count = 0; if (_voices != null) foreach (var voice in _voices) if (voice != null && voice.isPlaying) count++; return count; }
        }
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;
        private bool ContextAlive => _architecture != null && _model != null && ReferenceEquals(_architecture.GetModel<AudioModel>(), _model);
        private void Awake() { _focused = Application.isFocused; }
        private void OnEnable() { _reported = false; Bind(); }
        private void Bind()
        {
            if ((_subscription != null && ContextAlive) || _reported) return;
            string error = null;
            var settings = _profile != null ? _profile.AudioSettings : null;
            if (_pose == null || settings == null || !settings.TryValidate(out error))
            {
                if (!_reported) Debug.LogError("[CombatAudioView] " + (error ?? "Missing pose or feedback profile.") + " on " + name, this);
                _reported = true; return;
            }
            _subscription?.UnRegister();
            _architecture = GameApp.Interface;
            _model = _architecture.GetModel<AudioModel>();
            _shooting = _architecture.GetModel<ShootingModel>();
            _architecture.SendCommand(new ConfigureCombatAudioCommand(settings));
            if (_voices == null || _voices.Length != settings.VoiceCount) CreateVoices(settings);
            _subscription = _architecture.RegisterEvent<SoundPlayedEvent>(OnSound);
        }
        private void CreateVoices(CombatAudioSettings settings)
        {
            DestroyVoices();
            _voiceRoot = new GameObject("Combat audio runtime");
            SceneManager.MoveGameObjectToScene(_voiceRoot, gameObject.scene);
            _voiceRoot.hideFlags = HideFlags.DontSave;
            _voices = new AudioSource[settings.VoiceCount]; _kinds = new SoundId[_voices.Length]; _starts = new double[_voices.Length];
            for (int i = 0; i < _voices.Length; i++)
            {
                var voice = new GameObject("Combat voice " + i);
                voice.transform.SetParent(_voiceRoot.transform, false);
                var source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 1;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = settings.MinDistance; source.maxDistance = settings.MaxDistance;
                _voices[i] = source;
            }
        }
        private void Update()
        {
            if (!ContextAlive) { StopAll(); Bind(); return; }
            if (_pose == null || !_pose.isActiveAndEnabled || !_pose.IsInitialized || _aimContext != _pose.ContextId)
            { StopAll(); _aimContext = _pose != null ? _pose.ContextId : Guid.Empty; }
            if (_voices == null || _model.Combat == null) return;
            for (int i = 0; i < _voices.Length; i++) if (_voices[i] != null)
                _voices[i].volume = _model.Combat.Volume(_kinds[i]) * Mathf.Clamp01(_model.MasterVolume.Value);
        }
        private void OnSound(SoundPlayedEvent sound)
        {
            if (!_focused || !isActiveAndEnabled || !ContextAlive || _pose == null || !_pose.IsInitialized ||
                !_pose.isActiveAndEnabled || !_shooting.IsActive || _shooting.AimContext != _pose.ContextId ||
                _voices == null || !CombatNumbers.IsFinite(sound.Position)) return;
            var settings = _model.Combat;
            if (settings == null || settings.ClipCount(sound.Id) == 0) return;
            int selected = -1; double oldest = double.PositiveInfinity;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (!_voices[i].isPlaying) { selected = i; break; }
                if (_kinds[i] == sound.Id && _starts[i] < oldest) { selected = i; oldest = _starts[i]; }
            }
            if (selected < 0)
            {
                selected = 0;
                for (int i = 1; i < _voices.Length; i++) if (_starts[i] < _starts[selected]) selected = i;
            }
            var source = _voices[selected];
            source.Stop(); source.transform.position = sound.Position;
            source.clip = settings.GetClip(sound.Id, _variant++ & int.MaxValue);
            source.volume = settings.Volume(sound.Id) * Mathf.Clamp01(_model.MasterVolume.Value);
            _kinds[selected] = sound.Id; _starts[selected] = Time.realtimeSinceStartupAsDouble;
            _aimContext = _pose.ContextId;
            source.Play(); PlayCount++;
        }
        private void StopAll() { if (_voices != null) foreach (var voice in _voices) if (voice != null) voice.Stop(); }
        private void DestroyVoices()
        {
            StopAll();
            if (_voices != null) foreach (var voice in _voices) if (voice != null) Destroy(voice.gameObject);
            _voices = null;
            if (_voiceRoot != null) Destroy(_voiceRoot);
            _voiceRoot = null;
        }
        private void OnApplicationFocus(bool focused) { _focused = focused; if (!focused) StopAll(); }
        private void OnDisable() { _subscription?.UnRegister(); _subscription = null; StopAll(); }
        private void OnDestroy() { _subscription?.UnRegister(); _subscription = null; DestroyVoices(); }
    }
}
