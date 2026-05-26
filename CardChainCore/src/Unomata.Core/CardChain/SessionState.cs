namespace Unomata.Core;

/// <summary>
/// 骇入会话内部状态。仅 Core 层使用，不暴露给 Unity 端。
/// 由 <see cref="CardChainRules.IsValidNext"/> 与 <see cref="CardChainRules.ApplyPrev"/>
/// 共同维护：合法性判定读取本结构，玩家选中合法牌后由 ApplyPrev 原地更新。
/// </summary>
internal sealed class SessionState
{
    /// <summary>
    /// 当前牌的颜色。<c>null</c> 表示无颜色基准（开局，或刚接王牌后）。
    /// </summary>
    public CardColor? LastColor;

    /// <summary>
    /// 当前牌的数字。<c>null</c> 表示无数字基准（开局，或刚接反转/王牌后）。
    /// </summary>
    public int? LastNumber;

    /// <summary>
    /// 当前接龙方向。初始值 <see cref="ChainDirection.Ascending"/>，反转牌切换。
    /// </summary>
    public ChainDirection Direction = ChainDirection.Ascending;
}
