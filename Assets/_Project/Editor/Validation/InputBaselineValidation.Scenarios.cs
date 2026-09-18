using System;
using System.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unomata.Gameplay;

namespace Unomata.Editor.Validation
{
    public static partial class InputBaselineValidation
    {
        private static async Task CheckNormalInput()
        {
            Check("all six runtime actions", _pi.actions.FindActionMap("Player").actions.Count == 6);
            Check("initial neutral state", Neutral());
            var move = _pi.actions.FindAction("Move");
            Check("exactly one Move performed handler", PerformedHandlers(move) == 1);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W, Key.LeftShift));
            await Frames();
            Check("Move action to Model and SA buffer", _model.Move.Value.y > 0 && _sai.move == _model.Move.Value);
            Check("Sprint press", _model.Sprint.Value && _sai.sprint);
            await Release();
            Check("PassThrough Sprint release", !_model.Sprint.Value && !_sai.sprint && _model.Move.Value == Vector2.zero);

            int aimEvents = 0;
            var subscription = GameApp.Interface.RegisterEvent<AimStateChangedEvent>(_ => aimEvents++);
            try
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left));
                await Frames();
                Check("Aim and Fire callbacks", _model.IsAiming.Value && _model.Fire.Value &&
                    GameApp.Interface.GetModel<PlayerModel>().IsAiming.Value && aimEvents == 1);
                await Release();
                Check("Aim and Fire release", !_model.IsAiming.Value && !_model.Fire.Value && aimEvents == 2);
                GameApp.Interface.SendCommand(new SetAimStateCommand(true));
                GameApp.Interface.SendCommand(new SetAimStateCommand(true));
                GameApp.Interface.SendCommand(new SetAimStateCommand(false));
                GameApp.Interface.SendCommand(new SetAimStateCommand(false));
                Check("same-value aim commands are idempotent", aimEvents == 4);
            }
            finally { subscription.UnRegister(); }

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            await Frames();
            Check("Jump request reaches buffer", _model.Jump.Value && _sai.jump);
            _sai.jump = false; // Equivalent to the vendor consuming its request, not a Model write.
            await Frames(3);
            Check("held Jump is not reinjected after consumption", _model.Jump.Value && !_sai.jump);
            await Release();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            await Frames();
            Check("Jump re-press creates a new request", _sai.jump);
            await Release();

            _pi.SwitchCurrentControlScheme("Gamepad", _gamepad);
            await Frames();
            Vector2 observedLook = Vector2.zero;
            var lookAction = _pi.actions.FindAction("Look");
            Action<InputAction.CallbackContext> observeLook = ctx =>
                observedLook = ctx.canceled ? Vector2.zero : ctx.ReadValue<Vector2>();
            lookAction.performed += observeLook;
            lookAction.canceled += observeLook;
            InputSystem.QueueStateEvent(_gamepad, new GamepadState
                { leftStick = new Vector2(.8f, .3f), rightStick = new Vector2(.8f, .4f), leftTrigger = 1 });
            await Frames();
            Check("existing gamepad Move/Sprint bindings", _model.Move.Value.sqrMagnitude > 0 && _model.Sprint.Value);
            Check("Look matches the value observed in its player callback", _sai.look == observedLook && observedLook != Vector2.zero);
            var sameLook = _sai.look;
            await Frames(3);
            Check("held Look is stable", _sai.look == sameLook);
            await Release();
            Check("gamepad Look and Sprint release", _sai.look == Vector2.zero && observedLook == Vector2.zero && !_model.Sprint.Value);
            lookAction.performed -= observeLook;
            lookAction.canceled -= observeLook;
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            await Frames();
            Check("existing gamepad Jump binding", _model.Jump.Value && _sai.jump);
            await Release();
            _pi.SwitchCurrentControlScheme("KeyboardMouse", _keyboard, _mouse);
            await Frames();
            var mouseLook = _pi.actions.FindAction("Look");
            bool sawMouseDelta = false, mouseDeltaMatched = true;
            Action<InputAction.CallbackContext> mouseObserver = ctx =>
            {
                var value = ctx.ReadValue<Vector2>();
                if (value != Vector2.zero)
                {
                    sawMouseDelta = true;
                    mouseDeltaMatched &= _sai.look == value;
                }
            };
            mouseLook.performed += mouseObserver;
            try
            {
                InputSystem.QueueStateEvent(_mouse, new MouseState { delta = new Vector2(40, 20) });
                await Frames(3);
                Check("mouse delta forwarded once in its callback", sawMouseDelta && mouseDeltaMatched);
                Check("mouse delta clears on next input update", _sai.look == Vector2.zero);
            }
            finally { mouseLook.performed -= mouseObserver; }
            Check("normal path produces no errors", Diagnostics.Count == 0);
        }

        private static async Task CheckLifecycle()
        {
            for (int i = 0; i < 3; i++)
            {
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W, Key.Space, Key.LeftShift));
                InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left));
                await Frames();
                _controller.enabled = false;
                Check("controller disable resets owned state " + i, Neutral());
                await Frames();
                Check("controller disabled movement buffer neutral " + i, _sai.move == Vector2.zero && !_sai.jump && !_sai.sprint);
                _controller.enabled = true;
                await Frames();
                Check("held Jump/Fire suppressed after re-enable " + i, !_model.Jump.Value && !_model.Fire.Value);
                Check("single subscription after re-enable " + i, PerformedHandlers(_pi.actions.FindAction("Move")) == 1);
                await Release();
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W, Key.Space));
                await Frames();
                Check("controller accepts fresh input " + i, _model.Move.Value.y > 0 && _model.Jump.Value);
                _adapter.enabled = false;
                Check("adapter clears buffers " + i, _sai.move == Vector2.zero && _sai.look == Vector2.zero && !_sai.jump && !_sai.sprint);
                _adapter.enabled = true;
                await Frames();
                Check("adapter does not replay held Jump " + i, !_sai.jump);
                await Release();
            }

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W));
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Right));
            await Frames();
            _pi.DeactivateInput();
            await Frames();
            Check("deactivated PlayerInput clears state", Neutral() && _sai.look == Vector2.zero);
            _pi.ActivateInput();
            await Release();
            Check("input source recovers", PerformedHandlers(_pi.actions.FindAction("Move")) == 1);
            _pi.currentActionMap.Disable();
            await Frames();
            Check("disabled map clears state", Neutral());
            _pi.currentActionMap.Enable();
            await Release();
            _pi.enabled = false;
            await Frames();
            Check("disabled PlayerInput component clears state", Neutral());
            _pi.enabled = true;
            await Frames();
            _pi.SwitchCurrentControlScheme("KeyboardMouse", _keyboard, _mouse);
            await Release();

            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W));
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Right).WithButton(MouseButton.Left));
            await Frames();
            Focus(false);
            Check("synthetic focus loss immediately clears state", Neutral() && _sai.look == Vector2.zero);
            await Release();
            Focus(true);
            await Frames();
            Check("focus regain has no stale input", Neutral());
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W));
            await Frames();
            Check("focus regain accepts new input", _model.Move.Value.y > 0);
            await Release();

            await CreateFixture(initiallyDisabled: true);
            Check("initially disabled components stay neutral", Neutral());
            _controller.enabled = true; _adapter.enabled = true;
            Focus(true);
            await Frames();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W));
            await Frames();
            Check("first activation after disabled-before-Start works", _model.Move.Value.y > 0);
            await Release();
            Check("lifecycle path produces no errors", Diagnostics.Count == 0);
        }

        private static async Task CheckConfigurationFailures()
        {
            string[] faults = { "reference", "asset", "map",
                "missing:Move", "missing:Look", "missing:Jump", "missing:Sprint", "missing:Aim", "missing:Fire",
                "type:Move", "type:Look", "type:Jump", "type:Sprint", "type:Aim", "type:Fire" };
            foreach (var fault in faults)
            {
                await CreateFixture(fault);
                int expected = Diagnostics.Count;
                Check("actionable diagnostic: " + fault, Diagnostics.Exists(s => s.StartsWith("[PlayerController] Cannot bind")));
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W));
                await Frames(4);
                Check("no partial business subscription: " + fault, Neutral());
                Check("no repeating diagnostic: " + fault, Diagnostics.Count == expected);
                _controller.enabled = false;
                _controller.enabled = true;
                await Frames();
                foreach (var diagnostic in Diagnostics)
                    Check("no unexpected exception: " + fault, diagnostic.StartsWith("[PlayerController] Cannot bind") ||
                        diagnostic.StartsWith("[SAInputAdapter] Cannot bind") ||
                        (fault == "map" && diagnostic.Contains("Cannot find action map")));
                _result.expectedDiagnostics.Add(fault + ": " + string.Join(" | ", Diagnostics));
                int beforeDestroy = Diagnostics.Count;
                DestroyFixture();
                Check("safe cleanup after failed bind: " + fault, Diagnostics.Count == beforeDestroy);
            }
            await CreateFixture("missing:Aim");
            _controller.enabled = false; _adapter.enabled = false;
            var repaired = UnityEngine.Object.Instantiate(
                UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath));
            var old = _sourceAsset;
            _pi.actions = repaired; _sourceAsset = repaired;
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            _pi.SwitchCurrentControlScheme("KeyboardMouse", _keyboard, _mouse);
            _controller.enabled = true; _adapter.enabled = true; Focus(true);
            await Release();
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Right));
            await Frames();
            Check("configuration repaired then re-enabled", _model.IsAiming.Value);
            await Release();
        }

        private sealed class AimingTestArchitecture : Architecture<AimingTestArchitecture>
        {
            public AimingTestArchitecture() { }
            protected override void Init()
            {
                RegisterModel(new PlayerModel());
                RegisterModel(new PlayerInputModel());
                RegisterSystem(new PlayerSystem());
            }
        }

        private static void CheckAimingSystem()
        {
            var architecture = AimingTestArchitecture.Interface;
            var input = architecture.GetModel<PlayerInputModel>();
            int count = 0;
            var subscription = architecture.RegisterEvent<AimStateChangedEvent>(_ => count++);
            architecture.SendCommand(new SetAimStateCommand(true));
            architecture.SendCommand(new SetAimStateCommand(true));
            architecture.SendCommand(new SetAimStateCommand(false));
            architecture.SendCommand(new SetAimStateCommand(false));
            Check("isolated aiming state transitions once", count == 2);
            architecture.Deinit();
            input.IsAiming.Value = true;
            Check("released System has no input subscription", count == 2);
            subscription.UnRegister();
            var next = AimingTestArchitecture.Interface;
            int nextCount = 0;
            var nextSubscription = next.RegisterEvent<AimStateChangedEvent>(_ => nextCount++);
            next.SendCommand(new SetAimStateCommand(true));
            Check("new architecture has exactly one aim subscription", nextCount == 1);
            nextSubscription.UnRegister();
            next.Deinit();
        }
    }
}
