using UnityEngine;
namespace Unomata.Gameplay
{
    [CreateAssetMenu(menuName = "UNOMATA/Combat/Rifle profile")]
    public sealed class RifleProfile : ScriptableObject
    {
        [SerializeField] private float _damage = 20;
        [SerializeField] private float _shotsPerSecond = 8;
        [SerializeField] private float _range = 200;
        [SerializeField] private LayerMask _hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float _originRadius = 0.01f;
        public WeaponSettings Settings => new WeaponSettings(_damage, _shotsPerSecond, _range, _hitMask, _originRadius);
    }
}
