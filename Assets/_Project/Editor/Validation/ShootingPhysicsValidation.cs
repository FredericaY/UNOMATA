using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unomata.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unomata.EditorValidation
{
    public static class ShootingPhysicsValidation
    {
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
            var checks = new List<string>();
            Action<bool, string> check = (ok, name) => { if (!ok) throw new InvalidOperationException(name); checks.Add(name); };
            var original = SceneManager.GetActiveScene();
            var fixture = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            var query = new UnityShotWorldQuery();
            try
            {
                SceneManager.SetActiveScene(fixture);
                Vector3 origin = new Vector3(1000, 100, 1000);
                var self = Box("Self", origin, Vector3.one);
                var trigger = Box("Trigger", origin + Vector3.forward, Vector3.one * .2f);
                trigger.isTrigger = true;
                var wall = Box("Wall", origin + Vector3.forward * 4, new Vector3(1, 1, .2f));
                var target = Box("Target", origin + Vector3.forward * 6, Vector3.one);
                query.SetIgnoredColliders(new[] { self.GetInstanceID() });
                Physics.SyncTransforms();
                var hit = query.Raycast(origin, Vector3.forward, 10, -1);
                check(hit.HasHit && hit.ColliderId == wall.GetInstanceID(), "nearest wall blocks target");
                check(Math.Abs(hit.Distance - 3.9f) < .001f && Vector3.Dot(hit.Normal, Vector3.back) > .99f, "hit distance and normal");
                check(!query.IsInsideObstacle(origin, .01f, -1), "self internal origin filtered");
                check(query.IsInsideObstacle(wall.transform.position, .01f, -1), "wall internal origin blocked");
                check(!query.Raycast(origin, Vector3.forward, 3, -1).HasHit, "range limit and trigger filtering");
                wall.enabled = false; Physics.SyncTransforms();
                check(query.Raycast(origin, Vector3.forward, 10, -1).ColliderId == target.GetInstanceID(), "target after obstacle removed");
                target.enabled = false;
                for (int i = 0; i < 90; i++) Box("Dense " + i, origin + Vector3.forward * (2 + i * .08f), new Vector3(.2f, .2f, .04f));
                Physics.SyncTransforms();
                hit = query.Raycast(origin, Vector3.forward, 15, -1);
                check(hit.HasHit && Math.Abs(hit.Distance - 1.98f) < .001f, "saturated hit buffer retains nearest");
                for (int i = 0; i < 70; i++) Box("Overlap " + i, origin, Vector3.one * .01f);
                Physics.SyncTransforms();
                check(query.IsInsideObstacle(origin, .01f, -1), "saturated overlap buffer still blocks");
                query.Clear();
                check(query.IsInsideObstacle(origin, .01f, -1), "clear releases self exclusion");
            }
            finally
            {
                query.Clear(); SceneManager.SetActiveScene(original);
                EditorSceneManager.CloseScene(fixture, true);
            }
            Directory.CreateDirectory(".utmp/shooting");
            File.WriteAllText(".utmp/shooting/physics-validation.json", JsonConvert.SerializeObject(new { state = "passed", checks }, Formatting.Indented));
            return JsonConvert.SerializeObject(new { state = "passed", assertions = checks.Count });
        }
        private static BoxCollider Box(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name); go.transform.position = position;
            var collider = go.AddComponent<BoxCollider>(); collider.size = size; return collider;
        }
    }
}
