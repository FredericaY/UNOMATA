using UnityEngine;
namespace Unomata.Gameplay
{
    [CreateAssetMenu(menuName = "UNOMATA/Combat/Enemy profile")]
    public sealed class EnemyProfile : ScriptableObject
    {
        [SerializeField] private float _maxHp = 100;
        [SerializeField, Range(0, 1)] private float _baseDamageReduction = 0.95f;
        [SerializeField] private float _hackFactor;
        public EnemySettings Settings => new EnemySettings(_maxHp, _baseDamageReduction, _hackFactor);
    }
}
