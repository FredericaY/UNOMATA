using QFramework;
namespace Unomata.Gameplay
{
    public sealed class SprintDirectionQuery : AbstractQuery<bool>
    {
        protected override bool OnDo()=>this.GetSystem<PlayerSystem>().CanSprintInCurrentDirection();
    }
}
