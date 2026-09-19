using System;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unomata.Gameplay
{
    public sealed class ShootingFeedbackView : MonoBehaviour, IController
    {
        [SerializeField] private RifleFeedbackProfile _profile;
        [SerializeField] private PlayerAimPresentation _pose;
        private IArchitecture _architecture;
        private ShootingModel _model;
        private PlayerInputModel _input;
        private IUnRegister _shotSubscription, _deathSubscription;
        private GameObject _effectsRoot;
        private EffectPool _muzzles, _tracers, _impacts, _deaths;
        private ShotId _lastShot;
        private bool _focused, _reported;
        private float _shotTime = -10, _hitUntil, _killUntil;
        public int ShotFeedbackCount { get; private set; }
        public int ImpactFeedbackCount { get; private set; }
        public int DeathFeedbackCount { get; private set; }
        public int ActiveEffectCount => Count(_muzzles) + Count(_tracers) + Count(_impacts) + Count(_deaths);
        public int TotalEffectCount => Capacity(_muzzles) + Capacity(_tracers) + Capacity(_impacts) + Capacity(_deaths);
        IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;
        private bool ContextAlive => _architecture != null && _model != null && ReferenceEquals(_architecture.GetModel<ShootingModel>(), _model);
        private static int Count(EffectPool pool) => pool == null ? 0 : pool.ActiveCount;
        private static int Capacity(EffectPool pool) => pool == null ? 0 : pool.Capacity;
        private void Awake() { _focused = Application.isFocused; }
        private void OnEnable() { _reported = false; Bind(); }
        private void Bind()
        {
            if ((_shotSubscription != null && ContextAlive) || _reported) return;
            string error = null;
            if (_pose == null || _profile == null || !_profile.TryValidateEffects(out error))
            {
                if (!_reported) Debug.LogError("[ShootingFeedbackView] " + (error ?? "Missing pose or feedback profile.") + " on " + name, this);
                _reported = true; return;
            }
            Unsubscribe();
            _architecture = GameApp.Interface;
            _model = _architecture.GetModel<ShootingModel>();
            _input = _architecture.GetModel<PlayerInputModel>();
            if (_effectsRoot == null)
            {
                _effectsRoot = new GameObject("Shooting FX Runtime");
                SceneManager.MoveGameObjectToScene(_effectsRoot, gameObject.scene);
                _effectsRoot.hideFlags = HideFlags.DontSave;
                _muzzles = new EffectPool(_profile.MuzzlePrefab, _profile.MuzzleCapacity, _effectsRoot.transform);
                _tracers = new EffectPool(_profile.TracerPrefab, _profile.TracerCapacity, _effectsRoot.transform);
                _impacts = new EffectPool(_profile.ImpactPrefab, _profile.ImpactCapacity, _effectsRoot.transform);
                _deaths = new EffectPool(_profile.DeathPrefab, _profile.DeathCapacity, _effectsRoot.transform);
            }
            _shotSubscription = _architecture.RegisterEvent<ShotFiredEvent>(OnShot);
            _deathSubscription = _architecture.RegisterEvent<EnemyDiedEvent>(OnDeath);
        }
        private bool CanShow(Guid aimContext) => _focused && isActiveAndEnabled && ContextAlive &&
            _pose != null && _pose.isActiveAndEnabled && _pose.IsInitialized && _pose.ContextId == aimContext;
        private void OnShot(ShotFiredEvent shot)
        {
            if (!CanShow(shot.AimContext) || shot.Id.Equals(_lastShot) || _muzzles == null) return;
            _lastShot = shot.Id; _shotTime = Time.time; ShotFeedbackCount++;
            _muzzles.Spawn(shot.Origin, Quaternion.LookRotation(shot.Direction), _profile.MuzzleScale,
                _profile.MuzzleLifetime, _pose.Muzzle);
            _tracers.SpawnTrace(shot.Origin, shot.EndPoint, _profile.TracerWidth, _profile.TracerLifetime);
            if (shot.HitKind == ShotHitKind.Miss) return;
            var normal = shot.Normal.sqrMagnitude > .0001f ? shot.Normal : -shot.Direction;
            _impacts.Spawn(shot.EndPoint, Quaternion.LookRotation(normal), _profile.ImpactScale, _profile.ImpactLifetime);
            ImpactFeedbackCount++;
            if (shot.HitKind == ShotHitKind.Enemy) _hitUntil = Time.time + .12f;
        }
        private void OnDeath(EnemyDiedEvent fact)
        {
            if (!ContextAlive || _deaths == null || fact.Result.Shot.Context != _model.Context || !CanShow(_model.AimContext)) return;
            _deaths.Spawn(fact.Result.Position, Quaternion.identity, _profile.DeathScale, _profile.DeathLifetime);
            DeathFeedbackCount++; _killUntil = Time.time + .3f;
        }
        private void Update()
        {
            if (!ContextAlive) { ClearEffects(); Bind(); return; }
            if (!_focused || _pose == null || !_pose.isActiveAndEnabled || !_pose.IsInitialized)
            { ClearEffects(); return; }
            _muzzles?.Update(); _tracers?.Update(); _impacts?.Update(); _deaths?.Update();
        }
        private void OnGUI()
        {
            if (!_focused || !ContextAlive || _input == null || !_input.IsAiming.Value || _pose == null || !_pose.IsInitialized) return;
            var previousColor = GUI.color;
            var center = new Vector2(Screen.width * .5f, Screen.height * .5f);
            float kick = Mathf.Clamp01(1 - (Time.time - _shotTime) / .1f);
            float gap = 4 + kick * 3;
            GUI.color = new Color(.7f, .95f, 1, .9f);
            DrawRect(center.x - 1, center.y - 1, 2, 2);
            DrawRect(center.x - gap - 5, center.y - 1, 5, 2);
            DrawRect(center.x + gap, center.y - 1, 5, 2);
            DrawRect(center.x - 1, center.y - gap - 5, 2, 5);
            DrawRect(center.x - 1, center.y + gap, 2, 5);
            if (Time.time < _hitUntil || Time.time < _killUntil)
            {
                var matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(45, center);
                GUI.color = Time.time < _killUntil ? new Color(1, .45f, .1f, 1) : Color.white;
                DrawRect(center.x - 14, center.y - 1, 5, 2); DrawRect(center.x + 9, center.y - 1, 5, 2);
                DrawRect(center.x - 1, center.y - 14, 2, 5); DrawRect(center.x - 1, center.y + 9, 2, 5);
                GUI.matrix = matrix;
            }
            GUI.color = previousColor;
        }
        private static void DrawRect(float x, float y, float width, float height) =>
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        private void ClearEffects()
        {
            _muzzles?.Clear(); _tracers?.Clear(); _impacts?.Clear(); _deaths?.Clear();
            _hitUntil = _killUntil = 0; _shotTime = -10;
        }
        private void Unsubscribe()
        {
            _shotSubscription?.UnRegister(); _deathSubscription?.UnRegister();
            _shotSubscription = _deathSubscription = null;
        }
        private void OnApplicationFocus(bool focused) { _focused = focused; if (!focused) ClearEffects(); }
        private void OnDisable() { Unsubscribe(); ClearEffects(); }
        private void OnDestroy()
        {
            Unsubscribe(); ClearEffects();
            if (_effectsRoot != null) Destroy(_effectsRoot);
        }

        private sealed class EffectPool
        {
            private sealed class Slot
            {
                internal GameObject Object;
                internal ParticleSystem[] Particles;
                internal Vector3 BaseScale, Center, Size;
                internal float End, Started;
                internal bool Active;
            }
            private readonly Slot[] _slots;
            private readonly Transform _root;
            public int Capacity => _slots.Length;
            public int ActiveCount
            {
                get { int count = 0; foreach (var slot in _slots) if (slot.Active) count++; return count; }
            }
            public EffectPool(GameObject prefab, int capacity, Transform root)
            {
                _root = root; _slots = new Slot[capacity];
                for (int i = 0; i < capacity; i++)
                {
                    var obj = UnityEngine.Object.Instantiate(prefab, root);
                    obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    obj.name = prefab.name + " pooled " + i;
                    var renderers = obj.GetComponentsInChildren<Renderer>(true);
                    var bounds = new Bounds(obj.transform.position, Vector3.one * .01f);
                    if (renderers.Length > 0)
                    {
                        bounds = renderers[0].bounds;
                        for (int r = 1; r < renderers.Length; r++) bounds.Encapsulate(renderers[r].bounds);
                    }
                    _slots[i] = new Slot
                    {
                        Object = obj, Particles = obj.GetComponentsInChildren<ParticleSystem>(true),
                        BaseScale = obj.transform.localScale, Center = obj.transform.InverseTransformPoint(bounds.center), Size = bounds.size
                    };
                    obj.SetActive(false);
                }
            }
            private Slot Acquire(float duration)
            {
                Slot chosen = null;
                foreach (var slot in _slots) if (!slot.Active) { chosen = slot; break; }
                if (chosen == null)
                {
                    chosen = _slots[0];
                    foreach (var slot in _slots) if (slot.Started < chosen.Started) chosen = slot;
                    Stop(chosen);
                }
                chosen.Active = true; chosen.Started = Time.time; chosen.End = Time.time + duration;
                return chosen;
            }
            private static void Play(Slot slot)
            {
                slot.Object.SetActive(true);
                foreach (var particle in slot.Particles)
                {
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particle.Play(false);
                }
            }
            public void Spawn(Vector3 position, Quaternion rotation, float scale, float lifetime, Transform parent = null)
            {
                var slot = Acquire(lifetime);
                slot.Object.transform.SetParent(parent != null ? parent : _root, false);
                slot.Object.transform.SetPositionAndRotation(position, rotation);
                slot.Object.transform.localScale = slot.BaseScale * scale;
                Play(slot);
            }
            public void SpawnTrace(Vector3 origin, Vector3 endpoint, float width, float lifetime)
            {
                Vector3 delta = endpoint - origin;
                if (delta.sqrMagnitude < .000001f) return;
                var slot = Acquire(lifetime);
                var rotation = Quaternion.LookRotation(delta);
                Vector3 factors = new Vector3(width / Mathf.Max(slot.Size.x, .001f),
                    width / Mathf.Max(slot.Size.y, .001f), delta.magnitude / Mathf.Max(slot.Size.z, .001f));
                var scale = Vector3.Scale(slot.BaseScale, factors);
                slot.Object.transform.SetParent(_root, false);
                slot.Object.transform.localScale = scale;
                slot.Object.transform.SetPositionAndRotation((origin + endpoint) * .5f - rotation * Vector3.Scale(slot.Center, scale), rotation);
                Play(slot);
            }
            public void Update()
            {
                foreach (var slot in _slots) if (slot.Active && Time.time >= slot.End) Stop(slot);
            }
            private void Stop(Slot slot)
            {
                if (slot.Object == null) { slot.Active = false; return; }
                foreach (var particle in slot.Particles) if (particle != null)
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                slot.Object.SetActive(false);
                slot.Object.transform.SetParent(_root, false);
                slot.Active = false;
            }
            public void Clear() { foreach (var slot in _slots) if (slot.Active) Stop(slot); }
        }
    }
}
