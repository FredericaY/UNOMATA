using UnityEngine;

namespace Unomata.Gameplay
{
    [CreateAssetMenu(menuName = "UNOMATA/Combat/Enemy status UI")]
    public sealed class EnemyStatusProfile : ScriptableObject
    {
        [SerializeField, Min(1)] private float _maxDistance = 40;
        [SerializeField] private LayerMask _occlusionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private Color _healthColor = new Color(.25f, .95f, .72f);
        [SerializeField] private Color _resistColor = new Color(.55f, .8f, 1);
        [SerializeField] private Color _vulnerableColor = new Color(1, .6f, .23f);
        public float MaxDistance => _maxDistance;
        public int OcclusionMask => _occlusionMask;
        public Color HealthColor => _healthColor;
        public Color ResistColor => _resistColor;
        public Color VulnerableColor => _vulnerableColor;
        public bool IsValid => CombatNumbers.IsFinite(_maxDistance) && _maxDistance > 0 && _occlusionMask != 0;
    }
}
