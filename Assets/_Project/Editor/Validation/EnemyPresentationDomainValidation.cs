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
    public static class EnemyPresentationDomainValidation
    {
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
            var app = TestArchitecture.Interface;
            var system = app.GetSystem<EnemySystem>();
            var checks = new List<string>();
            var expected = new List<string>();
            Action<bool, string> check = (ok, text) => { if (!ok) throw new InvalidOperationException(text); checks.Add(text); };
            Application.LogCallback log = (text, stack, type) => { if (text.StartsWith("[EnemySystem]")) expected.Add(text); };
            Application.logMessageReceived += log;
            int registered = 0, unregistered = 0;
            var subscriptions = new[]
            {
                app.RegisterEvent<EnemyRegisteredEvent>(e => {
                    registered++;
                    check(system.Read(e.State.Id).Hp == e.State.Hp && system.Read(e.State.Id).Exists, "registration committed " + registered);
                    check(system.ReadCollider(201).Id == e.State.Id, "collider committed " + registered);
                }),
                app.RegisterEvent<EnemyUnregisteredEvent>(e => {
                    unregistered++;
                    check(!system.Read(e.EnemyId).Exists && !system.ReadCollider(201).Exists, "unregister committed " + unregistered);
                })
            };
            try
            {
                var id = Guid.NewGuid();
                check(system.Register(id, new EnemySettings(100, .95f, 0), new[] {201}), "register");
                check(!system.Register(id, new EnemySettings(100, 0, 0), new[] {202}) && registered == 1, "duplicate identity no fact");
                check(!system.Register(Guid.NewGuid(), new EnemySettings(100, 0, 0), new[] {201}) && registered == 1, "occupied collider no fact");
                check(!system.Register(Guid.NewGuid(), new EnemySettings(0, 0, 0), new[] {202}) && registered == 1, "invalid settings no fact");
                system.Unregister(id); system.Unregister(id);
                check(unregistered == 1, "idempotent unregister");
                var replacement = Guid.NewGuid();
                check(system.Register(replacement, new EnemySettings(100, .95f, 0), new[] {201}) && registered == 2, "new registration fact");
                var shotContext = Guid.NewGuid(); system.BeginShotContext(shotContext);
                check(!system.ApplyDamage(id, new ShotId(shotContext, 1), 20, Vector3.zero).Accepted, "old identity cannot damage replacement");
                float[] factors = {0, .5f, 1, 1.2f, 1.6f};
                string[] labels = {"RESIST 95%", "RESIST 47.5%", "RESIST 0%", "VULNERABLE +20%", "VULNERABLE +60%"};
                for (int i = 0; i < factors.Length; i++)
                {
                    var snapshot = new EnemySnapshot(replacement, 100, 75, .95f, factors[i]);
                    var data = new EnemyStatusData(snapshot);
                    check(data.Label == labels[i], "label oracle " + i + ": " + data.Label);
                    check(data.HpFraction == .75f && data.IsAlive, "hp oracle " + i);
                }
                var huge = new EnemyStatusData(new EnemySnapshot(replacement, 100, 100, .95f, float.MaxValue));
                check(!huge.Label.Contains("Infinity") && !huge.Label.Contains("NaN") && huge.Label.Contains("E+"), "finite large percentage");
                check(!new EnemyStatusData(default).IsAlive && new EnemyStatusData(default).HpFraction == 0, "missing target projection");
                check(!new EnemyStatusData(new EnemySnapshot(replacement, 100, 0, 0, 0)).IsAlive, "dead target projection");
                var result = new {state = "passed", assertions = checks.Count, checks, expectedDiagnostics = expected};
                Directory.CreateDirectory(".utmp/enemy-presentation");
                File.WriteAllText(".utmp/enemy-presentation/domain.json", JsonConvert.SerializeObject(result, Formatting.Indented));
                return JsonConvert.SerializeObject(result);
            }
            finally
            {
                foreach (var sub in subscriptions) sub.UnRegister();
                Application.logMessageReceived -= log;
                app.Deinit();
            }
        }
        public sealed class TestArchitecture : Architecture<TestArchitecture>
        {
            protected override void Init() { RegisterModel(new EnemyModel()); RegisterSystem(new EnemySystem()); }
        }
    }
}
