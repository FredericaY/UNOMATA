using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Unomata.Gameplay
{
    /// <summary>Resolves the current PlayerInput instance without modifying or enabling its asset.</summary>
    internal sealed class PlayerInputBinding
    {
        internal const string MapName = "Player";
        internal InputActionAsset Asset;
        internal InputActionMap Map;
        internal InputAction Move, Look, Jump, Sprint, Aim, Fire;

        internal bool IsCurrent(PlayerInput player)
        {
            return player != null && player.isActiveAndEnabled && player.inputIsActive &&
                   player.actions == Asset && player.currentActionMap == Map && Map.enabled &&
                   Move.enabled && Look.enabled && Jump.enabled && Sprint.enabled && Aim.enabled && Fire.enabled;
        }

        internal static bool TryResolve(PlayerInput player, out PlayerInputBinding binding, out string error)
        {
            binding = null;
            var errors = new List<string>();
            if (player == null) errors.Add("PlayerInput reference");
            else if (player.actions == null) errors.Add("PlayerInput.actions asset");
            else
            {
                var asset = player.actions;
                var map = asset.FindActionMap(MapName);
                if (player.defaultActionMap != MapName) errors.Add("defaultActionMap must be Player");
                if (player.notificationBehavior != PlayerNotifications.InvokeCSharpEvents)
                    errors.Add("notificationBehavior must be InvokeCSharpEvents");
                if (map == null) errors.Add("Player action map");
                else
                {
                    binding = new PlayerInputBinding { Asset = asset, Map = map };
                    binding.Move = Resolve(map, "Move", true, false, errors);
                    binding.Look = Resolve(map, "Look", true, false, errors);
                    binding.Jump = Resolve(map, "Jump", false, false, errors);
                    binding.Sprint = Resolve(map, "Sprint", false, true, errors);
                    binding.Aim = Resolve(map, "Aim", false, false, errors);
                    binding.Fire = Resolve(map, "Fire", false, false, errors);
                }
                if (errors.Count > 0) errors.Insert(0, "asset=" + asset.name + ", map=" + MapName);
            }
            error = string.Join("; ", errors);
            if (errors.Count == 0) return true;
            binding = null;
            return false;
        }

        private static InputAction Resolve(InputActionMap map, string name, bool vector, bool passThrough,
            List<string> errors)
        {
            var action = map.FindAction(name);
            if (action == null) { errors.Add("missing " + name); return null; }
            bool valid = vector
                ? action.type == InputActionType.Value && action.expectedControlType == "Vector2"
                : (action.type == InputActionType.Button ||
                   (passThrough && action.type == InputActionType.PassThrough)) &&
                  (string.IsNullOrEmpty(action.expectedControlType) ||
                   action.expectedControlType == "Button" || action.expectedControlType == "Axis");
            if (!valid) errors.Add("incompatible " + name + " (" + action.type + "/" + action.expectedControlType + ")");
            return action;
        }

        internal static bool IsHeld(InputAction action)
        {
            if (action.IsPressed()) return true;
            // Button actions do not necessarily perform an initial-state check on re-enable.
            foreach (var control in action.controls)
                if (control is ButtonControl button && button.isPressed) return true;
            return false;
        }

        internal bool Owns(object changed)
        {
            return ReferenceEquals(changed, Map) || ReferenceEquals(changed, Asset) ||
                   (changed is InputAction action && ReferenceEquals(action.actionMap, Map));
        }
    }
}
