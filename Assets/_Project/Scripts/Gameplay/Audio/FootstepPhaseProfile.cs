using System;
using UnityEngine;

namespace Unomata.Gameplay
{
    [CreateAssetMenu(menuName="UNOMATA/Footstep Phase Profile")]
    public sealed class FootstepPhaseProfile : ScriptableObject
    {
        [Serializable] private sealed class Entry
        {
            [SerializeField] private AnimationClip _clip;
            [SerializeField,Range(0,1)] private float _left;
            [SerializeField,Range(0,1)] private float _right;
            public AnimationClip Clip => _clip;
            public float Left => _left;
            public float Right => _right;
        }
        [SerializeField] private Entry[] _entries=Array.Empty<Entry>();
        public bool TryGet(AnimationClip clip,out float left,out float right)
        {
            foreach(var entry in _entries)
                if(entry!=null && entry.Clip==clip){left=entry.Left;right=entry.Right;return true;}
            left=right=0;return false;
        }
    }
}