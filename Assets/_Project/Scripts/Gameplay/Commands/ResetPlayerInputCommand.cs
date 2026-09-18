using QFramework;
using UnityEngine;

namespace Unomata.Gameplay
{
    /// <summary>Clears input intent through the architecture, including the normal aiming event path.</summary>
    public sealed class ResetPlayerInputCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var input = this.GetModel<PlayerInputModel>();
            input.Move.Value = Vector2.zero;
            input.Jump.Value = false;
            input.Sprint.Value = false;
            input.Fire.Value = false;
            input.IsAiming.Value = false;
        }
    }
}
