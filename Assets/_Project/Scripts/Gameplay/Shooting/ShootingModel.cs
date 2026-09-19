using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class ShootingModel : AbstractModel
    {
        public Guid Context { get; internal set; }
        public Guid AimContext { get; internal set; }
        public bool IsActive => Context != Guid.Empty;
        public long ShotCount { get; internal set; }
        public ShotFiredEvent LastShot { get; internal set; }
        internal WeaponSettings Settings;
        internal double Clock, NextShotTime;
        internal int LastFrame = -1;
        internal bool WasEligible, RequiresFireRelease;
        protected override void OnInit() { }
        protected override void OnDeinit() { Context = AimContext = Guid.Empty; LastShot = default; }
    }
}
