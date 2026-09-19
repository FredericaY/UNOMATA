using System;
using QFramework;
namespace Unomata.Gameplay
{
    public sealed class RegisterEnemyCommand : AbstractCommand
    {
        private readonly Guid _id;
        private readonly EnemySettings _settings;
        private readonly int[] _colliders;
        public RegisterEnemyCommand(Guid id, EnemySettings settings, int[] colliders)
        { _id = id; _settings = settings; _colliders = colliders; }
        protected override void OnExecute() => this.GetSystem<EnemySystem>().Register(_id, _settings, _colliders);
    }
}
