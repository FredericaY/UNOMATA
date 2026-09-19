using System;
namespace Unomata.Gameplay
{
    public readonly struct ShotId : IEquatable<ShotId>
    {
        public Guid Context { get; }
        public long Sequence { get; }
        public bool IsValid => Context != Guid.Empty && Sequence > 0;
        public ShotId(Guid context, long sequence) { Context = context; Sequence = sequence; }
        public bool Equals(ShotId other) => Context == other.Context && Sequence == other.Sequence;
        public override bool Equals(object obj) => obj is ShotId other && Equals(other);
        public override int GetHashCode() => Context.GetHashCode() ^ Sequence.GetHashCode();
        public override string ToString() => Context.ToString("N").Substring(0, 8) + ":" + Sequence;
    }
}
