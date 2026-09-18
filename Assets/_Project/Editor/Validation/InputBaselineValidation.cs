using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QFramework;
using StarterAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unomata.Gameplay;
using Object = UnityEngine.Object;

namespace Unomata.Editor.Validation
{
    /// <summary>Explicit, Editor-only regression runner. Never attached to a production scene.</summary>
    public static partial class InputBaselineValidation
    {
        [Serializable] public sealed class Result
        {
            public string state = "idle";
            public string phase;
            public string error;
            public int startFrame, endFrame;
            public List<string> passed = new List<string>();
            public List<string> expectedDiagnostics = new List<string>();
            public bool cleanupComplete;
        }

        private const string AssetPath = "Assets/_Project/Settings/UnomataPlayer.inputactions";
        private static Result _result = new Result();
        private static bool _running, _backgroundBefore;
        private static GameObject _fixture;
        private static InputActionAsset _sourceAsset;
        private static PlayerInput _pi;
        private static PlayerController _controller;
        private static SAInputAdapter _adapter;
        private static StarterAssetsInputs _sai;
        private static PlayerInputModel _model;
        private static Keyboard _keyboard;
        private static Mouse _mouse;
        private static Gamepad _gamepad;
        private static readonly List<Behaviour> Suspended = new List<Behaviour>();
        private static readonly List<string> Diagnostics = new List<string>();
        private static readonly List<Action> WaitCleanup = new List<Action>();

        public static string Status() => JsonUtility.ToJson(_result, true);

        [MenuItem("UNOMATA/Validation/Run Input Baseline")]
        public static void Start()
        {
            if (_running) throw new InvalidOperationException("Validation already running.");
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            _running = true;
            _result = new Result { state = "running", startFrame = Time.frameCount };
            Run();
        }

        public static void Cancel()
        {
            _running = false;
            _result.state = "cancelled";
        }

        private static async void Run()
        {
            _backgroundBefore = Application.runInBackground;
            try
            {
                Application.runInBackground = true; // Restored in finally; not a PlayerSettings change.
                foreach (var pc in Object.FindObjectsOfType<PlayerController>())
                    SuspendScene(pc);
                foreach (var adapter in Object.FindObjectsOfType<SAInputAdapter>())
                    SuspendScene(adapter);
                foreach (var pi in Object.FindObjectsOfType<PlayerInput>())
                    SuspendScene(pi);
                Application.logMessageReceived += CaptureLog;
                _keyboard = InputSystem.AddDevice<Keyboard>();
                _mouse = InputSystem.AddDevice<Mouse>();
                _gamepad = InputSystem.AddDevice<Gamepad>();
                _model = GameApp.Interface.GetModel<PlayerInputModel>();
                _result.phase = "normal input";
                await CreateFixture();
                await CheckNormalInput();
                _result.phase = "lifecycle";
                await CheckLifecycle();
                _result.phase = "configuration failures";
                await CheckConfigurationFailures();
                _result.phase = "aiming system lifecycle";
                CheckAimingSystem();
                _result.state = "passed";
            }
            catch (Exception exception)
            {
                _result.state = _running ? "failed" : "cancelled";
                _result.error = exception.ToString();
            }
            finally
            {
                _running = false;
                DestroyFixture();
                foreach (var cleanup in WaitCleanup.ToArray()) cleanup();
                Application.logMessageReceived -= CaptureLog;
                if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
                if (_mouse != null) InputSystem.RemoveDevice(_mouse);
                if (_gamepad != null) InputSystem.RemoveDevice(_gamepad);
                _keyboard = null; _mouse = null; _gamepad = null;
                // Restore sources before consumers. These changes remain confined to this Play session.
                for (int i = Suspended.Count - 1; i >= 0; --i)
                    if (Suspended[i] != null) Suspended[i].enabled = true;
                Suspended.Clear();
                Application.runInBackground = _backgroundBefore;
                _result.endFrame = Time.frameCount;
                _result.cleanupComplete = _fixture == null && WaitCleanup.Count == 0;
                Directory.CreateDirectory(".utmp/restore-input");
                File.WriteAllText(".utmp/restore-input/validation.json", Status());
                Debug.Log("[InputBaselineValidation] " + _result.state + ": " +
                          _result.passed.Count + " checks; cleanup=" + _result.cleanupComplete);
            }
        }

        private static void SuspendScene(Behaviour component)
        {
            if (!component.enabled) return;
            Suspended.Add(component);
            component.enabled = false;
        }

        private static void CaptureLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            Diagnostics.Add(message);
        }

        private static void Check(string name, bool condition)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + name);
            _result.passed.Add(name);
        }

        private static Task Frames(int frames = 2)
        {
            var completion = new TaskCompletionSource<bool>();
            int endFrame = Time.frameCount + frames;
            double deadline = EditorApplication.timeSinceStartup + 15;
            EditorApplication.CallbackFunction tick = null;
            Action cleanup = null;
            cleanup = () => { EditorApplication.update -= tick; WaitCleanup.Remove(cleanup); };
            tick = () =>
            {
                if (!_running || !EditorApplication.isPlaying)
                {
                    cleanup();
                    completion.TrySetException(new OperationCanceledException("Play session stopped."));
                }
                else if (Time.frameCount >= endFrame)
                {
                    cleanup();
                    completion.TrySetResult(true);
                }
                else if (EditorApplication.timeSinceStartup > deadline)
                {
                    cleanup();
                    completion.TrySetException(new TimeoutException("No actual player-frame progress."));
                }
            };
            WaitCleanup.Add(cleanup);
            EditorApplication.update += tick;
            return completion.Task;
        }

        private static async Task CreateFixture(string fault = null, bool initiallyDisabled = false)
        {
            DestroyFixture();
            Diagnostics.Clear();
            GameApp.Interface.SendCommand(new ResetPlayerInputCommand());
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            var json = JObject.Parse(source.ToJson());
            var map = (JObject)json["maps"][0];
            if (fault == "map") map["name"] = "MissingPlayer";
            if (fault != null && fault.StartsWith("missing:"))
            {
                string name = fault.Substring(8);
                foreach (var token in ((JArray)map["actions"]).ToArray())
                    if ((string)token["name"] == name) token.Remove();
            }
            if (fault != null && fault.StartsWith("type:"))
            {
                string name = fault.Substring(5);
                foreach (var action in map["actions"])
                    if ((string)action["name"] == name)
                    {
                        action["type"] = name == "Move" || name == "Look" ? "Button" : "Value";
                        action["expectedControlType"] = name == "Move" || name == "Look" ? "Button" : "Vector2";
                    }
            }
            _sourceAsset = InputActionAsset.FromJson(json.ToString());
            _fixture = new GameObject("InputBaselineValidation_Fixture") { hideFlags = HideFlags.DontSave };
            _fixture.SetActive(false);
            _pi = _fixture.AddComponent<PlayerInput>();
            _pi.actions = fault == "asset" ? null : _sourceAsset;
            _pi.defaultActionMap = "Player";
            _pi.defaultControlScheme = "KeyboardMouse";
            _pi.neverAutoSwitchControlSchemes = true;
            _pi.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _sai = _fixture.AddComponent<StarterAssetsInputs>();
            _controller = _fixture.AddComponent<PlayerController>();
            typeof(PlayerController).GetField("_playerInput", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_controller, fault == "reference" ? null : _pi);
            _adapter = _fixture.AddComponent<SAInputAdapter>();
            if (initiallyDisabled) { _controller.enabled = false; _adapter.enabled = false; }
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_mouse, new MouseState());
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            _fixture.SetActive(true);
            if (_pi.actions != null) _pi.SwitchCurrentControlScheme("KeyboardMouse", _keyboard, _mouse);
            Focus(true);
            await Frames();
        }

        private static void DestroyFixture()
        {
            if (_fixture != null)
            {
                var runtimeAsset = _pi != null ? _pi.actions : null;
                Object.DestroyImmediate(_fixture);
                if (runtimeAsset != null && runtimeAsset != _sourceAsset) Object.DestroyImmediate(runtimeAsset);
            }
            if (_sourceAsset != null) Object.DestroyImmediate(_sourceAsset);
            _fixture = null; _sourceAsset = null; _pi = null;
            _controller = null; _adapter = null; _sai = null;
        }

        private static void Focus(bool focused)
        {
            _controller.SendMessage("OnApplicationFocus", focused, SendMessageOptions.RequireReceiver);
            _adapter.SendMessage("OnApplicationFocus", focused, SendMessageOptions.RequireReceiver);
        }

        private static async Task Release()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_mouse, new MouseState());
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            await Frames();
        }

        private static int PerformedHandlers(InputAction action)
        {
            var callbacks = typeof(InputAction).GetField("m_OnPerformed", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(action);
            return (int)callbacks.GetType().GetProperty("length").GetValue(callbacks);
        }

        private static bool Neutral() => _model.Move.Value == Vector2.zero && !_model.Jump.Value &&
            !_model.Sprint.Value && !_model.IsAiming.Value && !_model.Fire.Value;
    }
}
