using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QFramework;
using Unomata.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Unomata.EditorValidation
{
    public static class ShootingDomainValidation
    {
        [MenuItem("UNOMATA/Validation/Shooting domain")]
        public static void RunMenu() { Debug.Log(Run()); }

        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run domain validation in Edit Mode.");
            var checks = new List<string>();
            var diagnostics = new List<string>();
            Action<bool, string> check = (ok, name) => { if (!ok) throw new InvalidOperationException(name); checks.Add(name); };
            Application.LogCallback log = (message, stack, type) =>
            { if (message.StartsWith("[EnemySystem]") || message.StartsWith("[ShootingSystem]")) diagnostics.Add(message); };
            Application.logMessageReceived += log;
            try
            {
                float[] factors = { 0, 0.5f, 1, 1.2f, 1.6f };
                float[] expected = { 5, 52.5f, 100, 120, 160 };
                for (int i = 0; i < factors.Length; i++)
                {
                    check(DamageCalculation.TryCalculate(100, 0.95f, factors[i], out var result), "formula accepted " + i);
                    check(Math.Abs(result.ResolvedDamage - expected[i]) < 0.0001f, "formula oracle " + i);
                }
                check(DamageCalculation.TryCalculate(100, 0, 0, out var noArmor) && noArmor.ResolvedDamage == 100, "no armor");
                check(DamageCalculation.TryCalculate(100, 1, 0, out var fullArmor) && fullArmor.ResolvedDamage == 0, "full armor");
                check(DamageCalculation.TryCalculate(0, 0.95f, 10, out var zero) && zero.ResolvedDamage == 0, "zero damage");
                foreach (float bad in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                {
                    check(!DamageCalculation.TryCalculate(bad, .95f, 0, out _), "bad damage " + bad);
                    check(!DamageCalculation.TryCalculate(1, .95f, bad, out _), "bad factor " + bad);
                    check(!new EnemySettings(bad, .95f, 0).TryValidate(out _), "bad hp " + bad);
                }
                check(!DamageCalculation.TryCalculate(float.MaxValue, 0, float.MaxValue, out _), "overflow rejected");
                check(!DamageCalculation.TryCalculate(1, 1.01f, 0, out _), "reduction over one");
                check(!DamageCalculation.TryCalculate(1, -.01f, 0, out _), "negative reduction");
                check(!new WeaponSettings(1, 0, 10, -1).TryValidate(out _), "zero fire rate");
                check(!new WeaponSettings(1, 1, -1, -1).TryValidate(out _), "negative range");
                check(!new WeaponSettings(1, 1, 10, 0).TryValidate(out _), "empty collision mask");

                using (var fixture = new Fixture())
                {
                    var enemies = fixture.App.GetSystem<EnemySystem>();
                    var id = Guid.NewGuid(); var other = Guid.NewGuid();
                    check(enemies.Register(id, new EnemySettings(100, .95f, 0), new[] { 101, 102 }), "register first");
                    check(enemies.Register(other, new EnemySettings(300, 0, 0), new[] { 103 }), "register second");
                    check(!enemies.Register(Guid.NewGuid(), new EnemySettings(100, 0, 0), new[] { 101 }), "duplicate collider refused");
                    check(enemies.ReadCollider(102).Id == id, "multiple collider mapping");
                    int damaged = 0, died = 0;
                    var subscriptions = new List<IUnRegister>
                    {
                        fixture.App.RegisterEvent<EnemyDamagedEvent>(e =>
                        {
                            damaged++;
                            check(enemies.Read(e.Result.EnemyId).Hp == e.Result.Hp, "state committed before damage event " + damaged);
                            check(!enemies.ApplyDamage(e.Result.EnemyId, e.Result.Shot, 100, Vector3.zero).Accepted, "reentrant same shot ignored " + damaged);
                        }),
                        fixture.App.RegisterEvent<EnemyDiedEvent>(e => { died++; check(!enemies.Read(e.Result.EnemyId).IsAlive, "death state committed"); })
                    };
                    try
                    {
                        var shot = new ShotId(fixture.Context, 1);
                        var damage = enemies.ApplyDamage(id, shot, 100, Vector3.zero);
                        check(damage.Accepted && Math.Abs(damage.AppliedDamage - 5) < .0001f, "first hit");
                        check(enemies.Read(other).Hp == 300, "other target isolated");
                        check(!enemies.ApplyDamage(id, shot, 100, Vector3.zero).Accepted && damaged == 1, "duplicate hit ignored");
                        fixture.App.SendCommand(new SetEnemyHackFactorCommand(id, .5f));
                        fixture.App.SendCommand(new SetEnemyHackFactorCommand(id, 1.2f));
                        check(fixture.App.SendQuery(new EnemySnapshotQuery(id)).HackFactor == 1.2f, "factor overwrites");
                        check(!enemies.SetHackFactor(id, -1) && enemies.Read(id).HackFactor == 1.2f, "invalid factor preserves state");
                        damage = enemies.ApplyDamage(id, new ShotId(fixture.Context, 2), 100, Vector3.zero);
                        check(damage.Killed && Math.Abs(damage.ResolvedDamage - 120) < .0001f && damage.AppliedDamage == 95 && damage.Hp == 0, "overkill clamped");
                        check(!enemies.ApplyDamage(id, new ShotId(fixture.Context, 3), 100, Vector3.zero).Accepted && died == 1, "dead target ignores next hit");
                        enemies.Unregister(id);
                        var replacement = Guid.NewGuid();
                        check(enemies.Register(replacement, new EnemySettings(100, 0, 0), new[] { 101 }), "collider can register new identity");
                        check(!enemies.ApplyDamage(id, new ShotId(fixture.Context, 4), 100, Vector3.zero).Accepted, "old enemy identity invalid");
                        enemies.ReleaseShotContext(fixture.Context);
                        check(!enemies.ApplyDamage(replacement, new ShotId(fixture.Context, 5), 100, Vector3.zero).Accepted, "released shot context invalid");
                        check(!enemies.Register(Guid.NewGuid(), new EnemySettings(0, 0, 0), new[] { 999 }), "bad registration rejected");
                    }
                    finally { foreach (var subscription in subscriptions) subscription.UnRegister(); }
                }

                foreach (int fps in new[] { 30, 60, 120 })
                {
                    using (var fixture = new Fixture())
                    {
                        fixture.Input(true, true);
                        for (int i = 0; i < fps * 10; i++) fixture.Step(1f / fps);
                        check(Math.Abs(fixture.Model.ShotCount - 80) <= 1, "cadence count " + fps);
                        long count = fixture.Model.ShotCount;
                        fixture.System.Tick(fixture.Context, fixture.Frame, 1);
                        check(fixture.Model.ShotCount == count, "same frame dedup " + fps);
                        fixture.Step(1);
                        check(fixture.Model.ShotCount == count + 1, "long frame one shot " + fps);
                        fixture.Input(false, true); fixture.Step(1);
                        check(fixture.Model.ShotCount == count + 1, "right release stops " + fps);
                        fixture.Input(true, false); fixture.Step(1);
                        check(fixture.Model.ShotCount == count + 1, "left release stops " + fps);
                    }
                }

                using (var fixture = new Fixture())
                {
                    fixture.Input(true, true);
                    fixture.Step(.2f, transition: true);
                    check(fixture.Model.ShotCount == 0, "transition blocks fire");
                    fixture.AimWorld.Obstructed = true;
                    fixture.Step(.2f, transition: true);
                    check(fixture.App.GetModel<AimModel>().Snapshot.Status == AimStatus.Blocked && fixture.Model.ShotCount == 0, "blocked transition still blocks fire");
                    fixture.ShotWorld.Hit = new ShotHit(Vector3.forward * 2, Vector3.back, 2, 401);
                    fixture.Step(.2f);
                    check(fixture.Model.ShotCount == 1 && fixture.Model.LastShot.HitKind == ShotHitKind.Surface, "stable occlusion shoots wall");
                    fixture.AimWorld.CameraInside = true; fixture.Step(.2f);
                    check(fixture.Model.ShotCount == 1, "camera inside blocks");
                    fixture.AimWorld.CameraInside = false; fixture.AimWorld.MuzzleInside = true; fixture.Step(.2f);
                    check(fixture.Model.ShotCount == 1, "muzzle inside blocks");
                    fixture.AimWorld.MuzzleInside = false; fixture.AimWorld.Obstructed = false; fixture.ShotWorld.Inside = true; fixture.Step(.2f);
                    check(fixture.Model.ShotCount == 1, "fresh shot origin safety");
                    fixture.ShotWorld.Inside = false;
                    fixture.Prepare(false, false, false);
                    fixture.System.Tick(fixture.Context, fixture.Frame, .2f);
                    check(fixture.Model.ShotCount == 1 && fixture.App.GetModel<AimModel>().Snapshot.Failure == AimFailure.InvalidPose, "invalid grips block fire");
                    fixture.Prepare(false, true, true);
                    fixture.System.Tick(fixture.Context, fixture.Frame, .2f);
                    check(fixture.Model.ShotCount == 1 && fixture.App.GetModel<AimModel>().Snapshot.Failure == AimFailure.TargetUnreachable, "unreachable direction blocks fire");
                    fixture.System.Tick(fixture.Context, ++fixture.Frame, .2f);
                    check(fixture.Model.ShotCount == 1, "stale pose blocks");
                    fixture.Step(.2f);
                    check(fixture.Model.ShotCount == 2, "recovery fresh shot");
                    fixture.System.Tick(Guid.NewGuid(), ++fixture.Frame, .2f);
                    check(fixture.Model.ShotCount == 2, "wrong context blocks");
                    fixture.System.End(fixture.Context);
                    fixture.Step(.2f);
                    check(fixture.Model.ShotCount == 2 && !fixture.Model.IsActive, "end context blocks");
                }

                using (var fixture = new Fixture())
                {
                    var enemies = fixture.App.GetSystem<EnemySystem>();
                    var id = Guid.NewGuid();
                    enemies.Register(id, new EnemySettings(100, .95f, .5f), new[] { 501, 502 });
                    fixture.ShotWorld.Hit = new ShotHit(Vector3.forward * 5, Vector3.back, 5, 502);
                    fixture.Input(true, true); fixture.Step(.02f);
                    check(fixture.Model.LastShot.HitKind == ShotHitKind.Enemy && Math.Abs(enemies.Read(id).Hp - 89.5f) < .0001f, "full command to target chain");
                    fixture.ShotWorld.Hit = default; fixture.Step(.2f);
                    check(fixture.Model.LastShot.HitKind == ShotHitKind.Miss && Math.Abs(fixture.Model.LastShot.EndPoint.z - 200) < .001f, "miss at range");
                    fixture.Input(true, false); fixture.Step(.01f);
                    long count = fixture.Model.ShotCount;
                    fixture.Input(true, true); fixture.Step(.01f);
                    check(fixture.Model.ShotCount == count, "click cannot bypass cooldown");
                }
                var report = new { state = "passed", assertions = checks.Count, checks, expectedDiagnostics = diagnostics };
                Directory.CreateDirectory(".utmp/shooting");
                File.WriteAllText(".utmp/shooting/domain-validation.json", JsonConvert.SerializeObject(report, Formatting.Indented));
                return JsonConvert.SerializeObject(new { report.state, report.assertions, expectedDiagnostics = diagnostics.Count });
            }
            finally { Application.logMessageReceived -= log; }
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly IArchitecture App = TestArchitecture.Interface;
            internal readonly Guid Context = Guid.NewGuid(), AimContext = Guid.NewGuid();
            internal readonly ShootingSystem System;
            internal readonly ShootingModel Model;
            internal readonly TestAimWorld AimWorld;
            internal readonly TestShotWorld ShotWorld;
            internal int Frame;
            internal Fixture()
            {
                System = App.GetSystem<ShootingSystem>(); Model = App.GetModel<ShootingModel>();
                AimWorld = (TestAimWorld)App.GetUtility<IAimWorldQuery>();
                ShotWorld = (TestShotWorld)App.GetUtility<IShotWorldQuery>();
                App.SendCommand(new BeginAimContextCommand(AimContext, new[] { 99 }));
                App.SendCommand(new BeginShootingCommand(Context, AimContext, new WeaponSettings(20, 8, 200, -1), new[] { 99 }));
            }
            internal void Input(bool aim, bool fire)
            { App.SendCommand(new SetAimStateCommand(aim)); App.SendCommand(new SetFireInputCommand(fire)); }
            internal void Prepare(bool transition = false, bool grips = true, bool reversed = false)
            {
                int frame = ++Frame;
                App.SendCommand(new PrepareAimFrameCommand(new AimFrameInput(AimContext, frame, true, transition,
                    Vector3.back * 2, Vector3.forward, Vector3.up, Vector3.zero, Vector3.zero, Quaternion.identity, 200, -1, -1)));
                App.SendCommand(new CompleteAimFrameCommand(AimContext, frame, Vector3.zero, reversed ? Vector3.back : Vector3.forward, grips));
            }
            internal void Step(float dt, bool transition = false)
            {
                Prepare(transition);
                App.SendCommand(new TickShootingCommand(Context, Frame, dt));
            }
            public void Dispose() { App.Deinit(); }
        }
        public sealed class TestArchitecture : Architecture<TestArchitecture>
        {
            protected override void Init()
            {
                RegisterModel(new PlayerInputModel()); RegisterModel(new PlayerModel());
                RegisterModel(new AimModel()); RegisterModel(new EnemyModel());
                RegisterModel(new ShootingModel()); RegisterModel(new AudioModel());
                RegisterUtility<IAimWorldQuery>(new TestAimWorld());
                RegisterUtility<IShotWorldQuery>(new TestShotWorld());
                RegisterSystem(new PlayerSystem()); RegisterSystem<IAimSystem>(new AimSystem());
                RegisterSystem(new EnemySystem()); RegisterSystem(new AudioSystem()); RegisterSystem(new ShootingSystem());
            }
        }
        private sealed class TestAimWorld : IAimWorldQuery
        {
            internal bool CameraInside, MuzzleInside, Obstructed;
            public void SetIgnoredColliders(int[] ids) { }
            public bool IsInsideObstacle(Vector3 origin, float radius, int mask) => origin.z < 0 ? CameraInside : MuzzleInside;
            public bool Raycast(Vector3 origin, Vector3 direction, float distance, int mask, out Vector3 point)
            { point = Vector3.forward * 2; return origin.z >= 0 && Obstructed; }
            public void Clear() { }
        }
        private sealed class TestShotWorld : IShotWorldQuery
        {
            internal bool Inside;
            internal ShotHit Hit;
            public void SetIgnoredColliders(int[] ids) { }
            public bool IsInsideObstacle(Vector3 origin, float radius, int mask) => Inside;
            public ShotHit Raycast(Vector3 origin, Vector3 direction, float range, int mask) => Hit;
            public void Clear() { }
        }
    }
}
