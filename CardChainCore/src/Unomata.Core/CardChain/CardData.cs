namespace Unomata.Core;

/// <summary>
/// 卡牌数据。<see cref="Type"/> 决定 <see cref="Color"/> 与 <see cref="Number"/> 的可空约束：
/// <list type="bullet">
/// <item><description><see cref="CardType.Number"/>：<c>Color</c> 必填，<c>Number</c> 取值 0..9</description></item>
/// <item><description><see cref="CardType.Reverse"/>：<c>Color</c> 必填，<c>Number</c> 为 <c>null</c></description></item>
/// <item><description><see cref="CardType.Wild"/>：<c>Color</c> 与 <c>Number</c> 均为 <c>null</c></description></item>
/// <item><description><see cref="CardType.Empty"/>：<c>Color</c> 与 <c>Number</c> 均为 <c>null</c>，仅用作 <see cref="Empty"/> 单例</description></item>
/// </list>
/// 接龙合法性判定不在本类型中暴露，由后续 <c>HackSession</c> 内部完成（见 INTERFACE.md）。
/// </summary>
public sealed class CardData
{
    /// <summary>牌的类型。</summary>
    public CardType Type;

    /// <summary>牌的颜色；仅 <see cref="CardType.Number"/> 与 <see cref="CardType.Reverse"/> 时有值。</summary>
    public CardColor? Color;

    /// <summary>牌的数字；仅 <see cref="CardType.Number"/> 时有值，取值 0..9。</summary>
    public int? Number;

    /// <summary>
    /// 开局占位空牌单例，用于 <c>HackSession.CurrentCard</c> 的初始值。
    /// 任何状态下接任意合法牌都允许（空牌等价于"无 lastColor 无 lastNumber"）。
    /// 该字段为单例语义：多次访问返回同一引用，且永不出现在选项中。
    /// </summary>
    public static readonly CardData Empty = new()
    {
        Type = CardType.Empty,
        Color = null,
        Number = null,
    };
}
