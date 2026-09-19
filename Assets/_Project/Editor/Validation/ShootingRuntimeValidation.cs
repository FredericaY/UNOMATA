using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QFramework;
using StarterAssets;
using Unomata.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unomata.EditorValidation
{
    public static class ShootingRuntimeValidation
    {
        private static bool _running, _cleaned;
        private static string _state = "idle", _error, _mode;
        private static int _fps, _samples, _shotCount, _deaths, _frameMismatch, _badDirection;
        private static float _maxAim, _maxGrip, _maxGripAngle, _peakRecoil, _actualFps;
        private static AimSnapshot _lastAim;
        private static bool _sampleShootingActive, _measuringCadence;
        private static float _cadenceMaxDelta;
        private static int _cadenceLongFrames;
        private static readonly List<string> Checks = new List<string>();
        private static readonly List<string> ExpectedDiagnostics = new List<string>();
        private static readonly List<object> Cases = new List<object>();
        private static readonly List<double> CadenceTimes = new List<double>();
        private static readonly List<float> CadenceDeltas = new List<float>();
        private static PlayerAimPresentation _pose;
        private static ShootingController _shooting;
        private static ShootingFeedbackView _feedback;
        private static CombatAudioView _audio;
        private static IArchitecture _app;
        private static ShootingModel _model;
        private static Keyboard _keyboard;
        private static Mouse _mouse;
        private static bool _record, _captured;
        private static Camera _camera;

        public static string Status() => JsonConvert.SerializeObject(new
        {
            state = _state, error = _error, mode = _mode, targetFps = _fps, checks = Checks.Count,
            samples = _samples, shots = _shotCount, deaths = _deaths, actualFps = _actualFps, cleanupComplete = _cleaned
        });
        public static string Start(string mode = "full", int fps = 60, bool record = true)
        {
            if (!EditorApplication.isPlaying || _running) throw new InvalidOperationException("Requires idle Play Mode.");
            _running = true; _cleaned = false; _state = "running"; _error = null; _mode = mode; _fps = fps; _record = record;
            Checks.Clear(); Cases.Clear(); ExpectedDiagnostics.Clear(); _samples = _shotCount = _deaths = _frameMismatch = _badDirection = 0;
            _maxAim = _maxGrip = _maxGripAngle = _peakRecoil = _actualFps = 0; _captured = false;
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            Run(); return Status();
        }
        public static void Cancel() { _running = false; }
        private static void Check(bool condition, string name)
        { if (!condition) throw new InvalidOperationException(name); Checks.Add(name); }
        private static async Task Frames(int frames)
        {
            int end = Time.frameCount + frames;
            double deadline = EditorApplication.timeSinceStartup + Math.Max(15, frames / 15d);
            while (Time.frameCount < end)
            {
                if (!_running || !EditorApplication.isPlaying) throw new OperationCanceledException();
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Player frame progression stalled.");
                EditorApplication.QueuePlayerLoopUpdate(); await Task.Delay(5);
            }
        }
        private static async Task Seconds(float seconds) { await Frames(Mathf.CeilToInt(_fps * seconds)); }
        private static void Input(bool aim, bool fire, params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            var state = new MouseState();
            if (aim) state = state.WithButton(MouseButton.Right);
            if (fire) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(_mouse, state);
        }
        private static void Focus(bool focused, PlayerController pc, SAInputAdapter adapter)
        {
            foreach (var component in new MonoBehaviour[] { pc, adapter, _pose, _shooting, _feedback, _audio })
                if (component != null) component.SendMessage("OnApplicationFocus", focused, SendMessageOptions.DontRequireReceiver);
        }
        private static void Sample()
        {
            if (!_running || _pose == null) return;
            _samples++;
            if (_measuringCadence) { _cadenceMaxDelta = Mathf.Max(_cadenceMaxDelta, Time.deltaTime); if (Time.deltaTime > .125f) _cadenceLongFrames++; }
            var snapshot = _pose.Snapshot;
            _lastAim = snapshot;
            _sampleShootingActive = _model != null && _model.IsActive;
            if (snapshot.Status == AimStatus.Ready)
            {
                _maxAim = Mathf.Max(_maxAim, snapshot.AimErrorDegrees);
                _maxGrip = Mathf.Max(_maxGrip, Vector3.Distance(_pose.RightHand.position, _pose.RightGrip.position),
                    Vector3.Distance(_pose.LeftHand.position, _pose.LeftGrip.position));
                _maxGripAngle = Mathf.Max(_maxGripAngle, Quaternion.Angle(_pose.RightHand.rotation, _pose.RightGrip.rotation),
                    Quaternion.Angle(_pose.LeftHand.rotation, _pose.LeftGrip.rotation));
            }
            _peakRecoil = Mathf.Max(_peakRecoil, _pose.RecoilOffset);
            if (_record && !_captured && _shotCount >= 2 && _feedback.ActiveEffectCount > 0)
            { Capture(".utmp/shooting/" + _mode + "-" + _fps + ".png"); _captured = true; }
        }
        private static void OnShot(ShotFiredEvent shot)
        {
            _shotCount++;
            if (_measuringCadence) { CadenceTimes.Add(shot.Time); CadenceDeltas.Add(Time.deltaTime); }
            if (shot.Frame != Time.frameCount || _pose.PoseFrame != shot.Frame || shot.AimContext != _pose.ContextId) _frameMismatch++;
            if (Vector3.Distance(shot.Origin, _pose.Muzzle.position) > .0001f ||
                AimGeometry.AngleDegrees(shot.Direction, _pose.Muzzle.forward) > .05f) _badDirection++;
        }
        private static void Capture(string path)
        {
            var texture = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var oldTarget = _camera.targetTexture; var oldActive = RenderTexture.active;
            try
            {
                _camera.targetTexture = texture; _camera.Render(); RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally { _camera.targetTexture = oldTarget; RenderTexture.active = oldActive; texture.Release(); Object.Destroy(texture); Object.Destroy(pixels); }
        }
        private static async void Run()
        {
            bool background = Application.runInBackground; int frameRate = Application.targetFrameRate, vSync = QualitySettings.vSyncCount;
            PlayerInput pi = null; PlayerController pc = null; SAInputAdapter adapter = null;
            InputDevice[] devices = null; string scheme = null; bool auto = false, pcEnabled = true, adapterEnabled = true, inputActive = true;
            Vector3 position = default; Quaternion rotation = default; float yaw = 0, pitch = 0, masterVolume = 1;
            EnemyController[] enemies = null; GameObject obstacle = null;
            IUnRegister shotSubscription = null, deathSubscription = null;
            try
            {
                Directory.CreateDirectory(".utmp/shooting");
                var roots = SceneManager.GetActiveScene().GetRootGameObjects();
                _pose = roots.SelectMany(r => r.GetComponentsInChildren<PlayerAimPresentation>()).Single();
                _shooting = _pose.GetComponent<ShootingController>(); _feedback = _pose.GetComponent<ShootingFeedbackView>(); _audio = _pose.GetComponent<CombatAudioView>();
                Check(_shooting != null && _feedback != null && _audio != null, "scene combat components exist");
                _app = GameApp.Interface; _model = _app.GetModel<ShootingModel>(); _camera = Camera.main;
                masterVolume = _app.GetModel<AudioModel>().MasterVolume.Value;
                pi = _pose.GetComponent<PlayerInput>(); pc = _pose.GetComponent<PlayerController>(); adapter = _pose.GetComponent<SAInputAdapter>();
                enemies = roots.SelectMany(r => r.GetComponentsInChildren<EnemyController>(true)).ToArray();
                foreach (var enemy in enemies) { enemy.enabled = false; enemy.enabled = true; }
                Check(enemies.Length == 3 && _app.GetModel<EnemyModel>().RegisteredCount == 3, "three independent target registrations");
                position = _pose.transform.position; rotation = _pose.transform.rotation; yaw = _pose.Motor.OrbitYaw; pitch = _pose.Motor.OrbitPitch;
                devices = pi.devices.ToArray(); scheme = pi.currentControlScheme; auto = pi.neverAutoSwitchControlSchemes;
                pcEnabled = pc.enabled; adapterEnabled = adapter.enabled; inputActive = pi.inputIsActive;
                Application.runInBackground = true; Application.targetFrameRate = _fps; QualitySettings.vSyncCount = 0;
                _keyboard = InputSystem.AddDevice<Keyboard>(); _mouse = InputSystem.AddDevice<Mouse>();
                pc.enabled = false; adapter.enabled = false; pi.DeactivateInput();
                pi.neverAutoSwitchControlSchemes = true; pi.SwitchCurrentControlScheme("KeyboardMouse", _keyboard, _mouse);
                pi.ActivateInput(); pc.enabled = true; adapter.enabled = true;
                Focus(true, pc, adapter);
                shotSubscription = _app.RegisterEvent<ShotFiredEvent>(OnShot);
                deathSubscription = _app.RegisterEvent<EnemyDiedEvent>(e => _deaths++);
                MagicaCloth2.MagicaManager.afterLateUpdateDelegate += Sample;
                Input(false, false); await Seconds(.6f);
                Input(false, true); await Seconds(.3f); Check(_shotCount == 0, "left only never fires");
                Input(false, false); await Seconds(.1f);
                Input(true, false); await Seconds(.6f);
                Check(_lastAim.Status == AimStatus.Ready && _sampleShootingActive, "real right action reaches ready aim; sampled status=" + _lastAim.Status + " failure=" + _lastAim.Failure + " active=" + _sampleShootingActive);
                if (_mode == "full") await Full(pc, adapter, enemies, go => obstacle = go);
                else if (_mode == "motion") await Motion();
                else if (_mode == "edge") await Edge(pc, adapter);

                Input(true, false); await Seconds(.3f);
                int before = _shotCount; int frameBefore = Time.frameCount;
                double wallStart = Time.realtimeSinceStartupAsDouble, gameStart = Time.timeAsDouble;
                int effectsBefore = _feedback.ShotFeedbackCount, soundBefore = _audio.PlayCount;
                _cadenceMaxDelta = 0; _cadenceLongFrames = 0; CadenceTimes.Clear(); CadenceDeltas.Clear(); _measuringCadence = true;
                Input(true, true);
                await Seconds(_mode == "endurance" ? 60 : 10);
                Input(true, false); await Frames(1);
                _measuringCadence = false;
                double elapsed = Time.timeAsDouble - gameStart;
                _actualFps = (float)((Time.frameCount - frameBefore) / (Time.realtimeSinceStartupAsDouble - wallStart));
                int shots = _shotCount - before;
                double interval = 1d / _shooting.Profile.Settings.ShotsPerSecond, maxIntervalError = 0;
                bool intervalsValid = true;
                for (int i = 1; i < CadenceTimes.Count; i++)
                {
                    double error = Math.Abs(CadenceTimes[i] - CadenceTimes[i - 1] - interval);
                    maxIntervalError = Math.Max(maxIntervalError, error);
                    intervalsValid &= error <= Math.Max(CadenceDeltas[i], CadenceDeltas[i - 1]) + .0001;
                }
                Check(intervalsValid, "every shot interval stays within one observed frame");
                Cases.Add(new { name = "cadence", maxIntervalError, elapsed, actualFps = _actualFps, shots, expected = elapsed * _shooting.Profile.Settings.ShotsPerSecond, maxDelta = _cadenceMaxDelta, longFrames = _cadenceLongFrames });
                Check(Math.Abs(shots - elapsed * _shooting.Profile.Settings.ShotsPerSecond) <= 1.0, "actual runtime cadence count: shots=" + shots + " expected=" + (elapsed * _shooting.Profile.Settings.ShotsPerSecond) + " maxDt=" + _cadenceMaxDelta + " longFrames=" + _cadenceLongFrames);
                Check(Math.Abs(_actualFps - _fps) / _fps < .15f, "measured frame rate reaches requested band");
                Check(_feedback.ShotFeedbackCount - effectsBefore == shots, "one muzzle and tracer feedback per shot");
                Check(_audio.PlayCount - soundBefore >= shots, "every accepted shot requests actual audio playback");
                Check(_frameMismatch == 0 && _badDirection == 0, "all shots use same-frame real muzzle");
                Check(_maxAim <= .5f && _maxGrip <= .0101f && _maxGripAngle <= 2.01f, "aim and grip tolerances remain valid");
                Check(_peakRecoil > .001f, "visible recoil signal occurs");
                await Seconds(2.1f);
                Check(_feedback.ActiveEffectCount == 0 && _audio.PlayingCount == 0, "effect and audio tails return to idle");
                Check(_feedback.TotalEffectCount <= 32 && _audio.VoiceCount == 24, "effect and voice instances stay bounded");
                if (_mode == "full")
                {
                    Input(true, true); await Seconds(.25f);
                    Focus(false, pc, adapter); int stopped = _shotCount; await Seconds(.3f);
                    Check(_shotCount == stopped && _feedback.ActiveEffectCount == 0 && _audio.PlayingCount == 0, "simulated focus loss stops shots and feedback");
                    Focus(true, pc, adapter); Input(false, false); await Seconds(.5f);
                    for (int i = 0; i < 3; i++)
                    {
                        _shooting.enabled = _feedback.enabled = _audio.enabled = false;
                        await Frames(2); _shooting.enabled = _feedback.enabled = _audio.enabled = true;
                        Focus(true, pc, adapter); await Frames(3);
                    }
                    int fresh = _shotCount; Input(true, false); await Seconds(.5f); Input(true, true); await Seconds(.4f);
                    Check(_shotCount > fresh && _shotCount - fresh <= 4, "three enable cycles recover without duplicated fire");
                    Input(false, false); await Seconds(.3f);
                }
                _state = "passed";
            }
            catch (Exception error) { _state = error is OperationCanceledException ? "cancelled" : "failed"; _error = error.ToString(); }
            finally
            {
                MagicaCloth2.MagicaManager.afterLateUpdateDelegate -= Sample;
                shotSubscription?.UnRegister(); deathSubscription?.UnRegister();
                AssemblyReloadEvents.beforeAssemblyReload -= Cancel;
                if (obstacle != null) Object.Destroy(obstacle);
                if (pi != null)
                {
                    pi.DeactivateInput();
                    if (devices != null && devices.Length > 0) pi.SwitchCurrentControlScheme(scheme, devices);
                    pi.neverAutoSwitchControlSchemes = auto;
                    if (inputActive) pi.ActivateInput();
                }
                if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
                if (_mouse != null) InputSystem.RemoveDevice(_mouse);
                _keyboard = null; _mouse = null;
                if (_pose != null)
                {
                    var capsule = _pose.GetComponent<CharacterController>(); capsule.enabled = false;
                    _pose.transform.SetPositionAndRotation(position, rotation); capsule.enabled = true;
                    typeof(PlayerMotor).GetProperty("OrbitYaw").SetValue(_pose.Motor, yaw);
                    typeof(PlayerMotor).GetProperty("OrbitPitch").SetValue(_pose.Motor, pitch);
                    _app.SendCommand(new ResetPlayerInputCommand());
                    _app.SendCommand(new SetMasterVolumeCommand(masterVolume));
                    var buffer = _pose.GetComponent<StarterAssetsInputs>(); buffer.move = buffer.look = Vector2.zero; buffer.jump = buffer.sprint = false;
                    if (pc != null) pc.enabled = pcEnabled;
                    if (adapter != null) adapter.enabled = adapterEnabled;
                    Focus(Application.isFocused, pc, adapter);
                }
                if (enemies != null) foreach (var enemy in enemies) if (enemy != null) { enemy.enabled = false; enemy.enabled = true; }
                Application.runInBackground = background; Application.targetFrameRate = frameRate; QualitySettings.vSyncCount = vSync;
                _running = false; _cleaned = true; _measuringCadence = false;
                File.WriteAllText(".utmp/shooting/runtime-" + _mode + "-" + _fps + ".json", JsonConvert.SerializeObject(new
                {
                    state = _state, error = _error, checks = Checks, cases = Cases, expectedDiagnostics = ExpectedDiagnostics, samples = _samples, shots = _shotCount, deaths = _deaths,
                    maxAim = _maxAim, maxGrip = _maxGrip, maxGripAngle = _maxGripAngle, peakRecoil = _peakRecoil,
                    frameMismatch = _frameMismatch, badDirection = _badDirection, cleanupComplete = _cleaned
                }, Formatting.Indented));
            }
        }
        private static async Task Full(PlayerController pc, SAInputAdapter adapter, EnemyController[] enemies, Action<GameObject> remember)
        {
            var center = enemies.OrderBy(e => Math.Abs(e.transform.position.x)).First();
            float hp = center.Snapshot.Hp;
            Input(true, true); await Seconds(.7f); Input(true, false); await Frames(1);
            Check(_shotCount > 0 && center.Snapshot.Hp < hp, "actual left action damages aimed capsule");
            Check(_app.GetModel<EnemyModel>().LastDamage.EnemyId == center.Id, "damage belongs to aimed target");
            int count = _shotCount; await Seconds(.25f); Check(_shotCount == count, "left release immediately stops");
            var voices = (AudioSource[])typeof(CombatAudioView).GetField("_voices", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_audio);
            var voicePositions = voices.Select(v => v.transform.position).ToArray();
            Input(true, false, Key.W); await Frames(3); Input(true, false);
            Check(voices.Select((v, i) => Vector3.Distance(v.transform.position, voicePositions[i]) < .0001f).All(v => v), "existing sound positions do not follow player movement");
            _app.SendCommand(new SetMasterVolumeCommand(0)); await Frames(2);
            Check(voices.All(v => v.volume == 0), "master volume mutes combat voices");
            _app.SendCommand(new SetMasterVolumeCommand(1)); await Frames(2);
            Input(false, true); await Seconds(.4f); Check(_shotCount == count, "right release blocks held left");
            Input(true, false); await Seconds(.6f);
            var snapshot = _lastAim;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); remember(wall);
            wall.name = "Shooting validation obstruction";
            wall.transform.position = snapshot.MuzzlePosition + snapshot.BarrelDirection * .45f;
            wall.transform.rotation = Quaternion.LookRotation(snapshot.BarrelDirection);
            wall.transform.localScale = new Vector3(.09f, .09f, .08f);
            Physics.IgnoreCollision(_pose.GetComponent<CharacterController>(), wall.GetComponent<Collider>());
            Physics.SyncTransforms(); await Frames(4);
            hp = center.Snapshot.Hp;
            Input(true, true); await Seconds(.3f); Input(true, false); await Frames(1);
            Check(_model.LastShot.HitKind == ShotHitKind.Surface && center.Snapshot.Hp == hp, "gun obstruction hits surface without target damage");
            Check(Vector3.Distance(_model.LastShot.EndPoint, wall.transform.position) < .15f, "feedback ends at near obstruction");
            wall.transform.position = _pose.Muzzle.position; wall.transform.localScale = Vector3.one * .4f;
            Physics.SyncTransforms(); await Frames(4); count = _shotCount;
            Input(true, true); await Seconds(.3f); Input(true, false); await Frames(1);
            Check(_shotCount == count, "embedded muzzle blocks actual fire");
            wall.SetActive(false); Object.Destroy(wall); remember(null); await Seconds(.2f);
            Input(true, true); await Seconds(1.8f); Input(true, false); await Frames(1);
            Check(!center.Snapshot.IsAlive && _deaths == 1, "target dies exactly once");
            Check(!center.GetComponent<Collider>().enabled && _feedback.DeathFeedbackCount == 1, "death removes collision and plays one effect");
            var old = center.Id; center.enabled = false; center.enabled = true;
            Check(center.Id != old && center.Snapshot.Hp == center.Snapshot.MaxHp, "reset creates fresh target identity");
        }
        private static async Task Edge(PlayerController pc, SAInputAdapter adapter)
        {
            var audioModel = _app.GetModel<AudioModel>();
            float volume = audioModel.MasterVolume.Value;
            var profileField = typeof(CombatAudioView).GetField("_profile", BindingFlags.Instance | BindingFlags.NonPublic);
            var audioProfile = profileField.GetValue(_audio);
            var feedbackField = typeof(ShootingFeedbackView).GetField("_profile", BindingFlags.Instance | BindingFlags.NonPublic);
            var feedbackProfile = (RifleFeedbackProfile)feedbackField.GetValue(_feedback);
            Application.LogCallback expected = (message, stack, type) =>
            {
                if (type == LogType.Error && (message.StartsWith("[CombatAudioView]") || message.StartsWith("[ShootingFeedbackView]")))
                    ExpectedDiagnostics.Add(message);
            };
            GameObject poolRoot = null;
            Application.logMessageReceived += expected;
            try
            {
                _app.SendCommand(new SetMasterVolumeCommand(0));
                int before = _audio.PlayCount;
                var audioSystem = _app.GetSystem<AudioSystem>();
                for (int i = 0; i < 30; i++) audioSystem.Play(SoundId.GunShot, new Vector3(i * .01f, 1, 2));
                Check(_audio.PlayCount - before == 30 && _audio.PlayingCount <= _audio.VoiceCount && _audio.VoiceCount == 24, "overloaded audio remains bounded");
                _audio.enabled = false; await Frames(1); _audio.enabled = true;
                Focus(true, pc, adapter); _app.SendCommand(new SetMasterVolumeCommand(volume));

                poolRoot = new GameObject("Shooting validation pool");
                var poolType = typeof(ShootingFeedbackView).GetNestedType("EffectPool", BindingFlags.NonPublic);
                var pool = Activator.CreateInstance(poolType, new object[] { feedbackProfile.ImpactPrefab, 2, poolRoot.transform });
                var spawn = poolType.GetMethod("Spawn");
                for (int i = 0; i < 12; i++) spawn.Invoke(pool, new object[] { new Vector3(10000, 100, 10000), Quaternion.identity, .1f, 1f, null });
                Check((int)poolType.GetProperty("ActiveCount").GetValue(pool) == 2 && poolRoot.transform.childCount == 2, "effect pool replaces oldest without growth");
                poolType.GetMethod("Clear").Invoke(pool, null);
                Check((int)poolType.GetProperty("ActiveCount").GetValue(pool) == 0, "effect pool explicit clear");
                Object.Destroy(poolRoot); poolRoot = null;

                _audio.enabled = false; profileField.SetValue(_audio, null); _audio.enabled = true;
                before = _audio.PlayCount;
                audioSystem.Play(SoundId.GunShot, Vector3.zero); await Frames(5);
                Check(_audio.PlayCount == before && ExpectedDiagnostics.Count == 1, "missing audio reports once and does not play");
                _audio.enabled = false; profileField.SetValue(_audio, audioProfile); _audio.enabled = true;
                Focus(true, pc, adapter);

                _feedback.enabled = false; feedbackField.SetValue(_feedback, null); _feedback.enabled = true;
                int effects = _feedback.ShotFeedbackCount, shots = _shotCount;
                var target = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyController>()).OrderBy(e => Math.Abs(e.transform.position.x)).First();
                float hp = target.Snapshot.Hp;
                Input(true, true); await Seconds(.3f); Input(true, false); await Frames(1);
                Check(_shotCount > shots && _feedback.ShotFeedbackCount == effects && ExpectedDiagnostics.Count == 2, "missing effects report once while shots remain single");
                Check(Math.Abs(hp - target.Snapshot.Hp - (_shotCount - shots) * 10.5f) < .001f, "presentation failure does not duplicate or suppress damage");
                _feedback.enabled = false; feedbackField.SetValue(_feedback, feedbackProfile); _feedback.enabled = true;
                Focus(true, pc, adapter); await Frames(2);

                Input(true, true); await Seconds(.3f);
                _shooting.enabled = _feedback.enabled = _audio.enabled = false;
                int stopped = _shotCount; await Seconds(.3f);
                Check(_shotCount == stopped && _feedback.ActiveEffectCount == 0 && _audio.PlayingCount == 0, "disable while held clears fire and feedback");
                _shooting.enabled = _feedback.enabled = _audio.enabled = true;
                Focus(true, pc, adapter); await Seconds(.3f);
                Check(_shotCount == stopped, "reenable with held fire requires fresh press");
                Input(false, false); await Seconds(.2f); Input(true, false); await Seconds(.5f);
            }
            finally
            {
                Application.logMessageReceived -= expected;
                if (poolRoot != null) Object.Destroy(poolRoot);
                _audio.enabled = false; profileField.SetValue(_audio, audioProfile); _audio.enabled = true;
                _feedback.enabled = false; feedbackField.SetValue(_feedback, feedbackProfile); _feedback.enabled = true;
                _shooting.enabled = true; Focus(true, pc, adapter);
                _app.SendCommand(new SetMasterVolumeCommand(volume));
                Input(true, false);
            }
        }

        private static async Task Motion()
        {
            var keys = new[] { new[] { Key.W }, new[] { Key.W, Key.D }, new[] { Key.D }, new[] { Key.D, Key.S },
                new[] { Key.S }, new[] { Key.S, Key.A }, new[] { Key.A }, new[] { Key.A, Key.W }, new[] { Key.W, Key.LeftShift } };
            foreach (var direction in keys)
            {
                int before = _shotCount; Input(true, true, direction); await Seconds(.6f);
                Check(_shotCount > before, "movement fires " + string.Join("+", direction));
            }
            Input(true, true); await Seconds(.4f);
            foreach (var direction in keys.Take(8))
            {
                int before = _shotCount;
                Input(true, true, direction.Concat(new[] { Key.Space }).ToArray()); await Seconds(.2f);
                Input(true, true, direction); await Seconds(1.1f);
                Check(_shotCount > before, "moving jump fires " + string.Join("+", direction));
            }
            Input(true, false); await Seconds(.5f);
        }
    }
}
