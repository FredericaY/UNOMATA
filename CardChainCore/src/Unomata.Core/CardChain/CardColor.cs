namespace Unomata.Core;

/// <summary>
/// 牌的颜色枚举。卡池中每个颜色各 10 张数字牌（0-9）+ 2 张反转牌。
/// 王牌（<see cref="CardType.Wild"/>）与空牌（<see cref="CardType.Empty"/>）无颜色。
/// </summary>
public enum CardColor
{
    Red,
    Blue,
    Green,
    Yellow,
}
