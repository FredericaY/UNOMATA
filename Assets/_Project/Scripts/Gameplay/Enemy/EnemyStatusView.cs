using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unomata.Gameplay
{
    [DefaultExecutionOrder(700), DisallowMultipleComponent]
    public sealed class EnemyStatusView : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemy;
        [SerializeField] private EnemyStatusProfile _profile;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _anchor;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Collider[] _playerColliders;
        private readonly UnityShotWorldQuery _occlusion = new UnityShotWorldQuery();
        private EnemyViewBinding _binding;
        private Canvas _ownedCanvas;
        private void Awake() { _ownedCanvas = _canvas != null ? _canvas : GetComponentInChildren<Canvas>(true); }
        private bool _reported;
        public EnemyStatusData Data { get; private set; }
        public bool IsVisible => _canvas != null && _canvas.enabled;
        public string DisplayedLabel => _label != null ? _label.text : "";
        private bool Valid => _enemy != null && _profile != null && _profile.IsValid &&
            _camera != null && _anchor != null && _canvas != null && _fill != null && _label != null;

        private void OnEnable()
        {
            _reported = false;
            Hide();
            if (!Valid) { Report(); return; }
            var own = _enemy.GetComponentsInChildren<Collider>(true);
            var ignored = new int[own.Length + (_playerColliders == null ? 0 : _playerColliders.Length)];
            for (int i = 0; i < own.Length; i++) ignored[i] = own[i].GetInstanceID();
            if (_playerColliders != null)
                for (int i = 0; i < _playerColliders.Length; i++)
                    ignored[own.Length + i] = _playerColliders[i] != null ? _playerColliders[i].GetInstanceID() : 0;
            _occlusion.SetIgnoredColliders(ignored);
            _canvas.worldCamera = _camera;
            _binding = new EnemyViewBinding(_enemy, Apply, Apply);
        }
        private void Apply(EnemySnapshot state)
        {
            Data = new EnemyStatusData(state);
            if (_profile == null) { Hide(); Report(); return; }
            if (_fill != null) { _fill.fillAmount = Data.HpFraction; _fill.rectTransform.anchorMax = new Vector2(Data.HpFraction, 1); _fill.color = _profile.HealthColor; }
            if (_label != null)
            {
                _label.text = state.Exists ? Data.Label : "";
                _label.color = Data.Vulnerability > 0 ? _profile.VulnerableColor : _profile.ResistColor;
            }
            if (!Data.IsAlive) Hide();
        }
        private void LateUpdate()
        {
            if (!Valid) { Hide(); Report(); return; }
            if (_binding == null || !_binding.IsContextAlive)
            {
                _binding?.Dispose(); _binding = null;
                Hide(); return;
            }
            if (!Data.IsAlive) { Hide(); return; }
            Vector3 origin = _camera.transform.position;
            Vector3 point = _anchor.position;
            Vector3 offset = point - origin;
            Vector3 screen = _camera.WorldToViewportPoint(point);
            float distance = offset.magnitude;
            bool visible = screen.z > _camera.nearClipPlane && screen.x >= 0 && screen.x <= 1 &&
                screen.y >= 0 && screen.y <= 1 && distance <= _profile.MaxDistance && distance > .001f;
            if (visible)
                visible = !_occlusion.IsInsideObstacle(origin, .001f, _profile.OcclusionMask) &&
                    !_occlusion.Raycast(origin, offset / distance, distance, _profile.OcclusionMask).HasHit;
            _canvas.enabled = visible;
            if (visible)
            {
                _canvas.transform.SetPositionAndRotation(point, _camera.transform.rotation);
            }
        }
        private void Hide()
        {
            if (_canvas != null) _canvas.enabled = false;
            if (_ownedCanvas != null) _ownedCanvas.enabled = false;
        }
        private void Report()
        {
            if (_reported) return;
            _reported = true;
            Debug.LogWarning("[EnemyStatusView] Missing enemy/camera/anchor/graphics or invalid status profile on " + name, this);
        }
        private void OnDisable()
        {
            _binding?.Dispose(); _binding = null;
            _occlusion.Clear(); Data = default; Hide();
        }
        private void OnDestroy() { _binding?.Dispose(); _binding = null; _occlusion.Clear(); }
    }
}
