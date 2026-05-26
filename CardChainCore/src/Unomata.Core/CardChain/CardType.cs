namespace Unomata.Core;

/// <summary>
/// 牌的类型枚举。决定 <see cref="CardData"/> 中 <c>Color</c> 与 <c>Number</c> 字段的可空约束：
/// <list type="bullet">
/// <item><description><c>Number</c>：有色有数字（0-9），合法接龙时参与升降序判定</description></item>
/// <item><description><c>Reverse</c>：有色无数字，作为 next 时切换接龙方向</description></item>
/// <item><description><c>Wild</c>：无色无数字，作为 next 永远合法</description></item>
/// <item><description><c>Empty</c>：占位空牌，仅用作 <see cref="HackSession"/> 的开局 CurrentCard 占位，永不参与发牌</description></item>
/// </list>
/// </summary>
public enum CardType
{
    Number,
    Reverse,
    Wild,
    Empty,
}
