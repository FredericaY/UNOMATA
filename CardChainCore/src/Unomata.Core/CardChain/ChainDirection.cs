namespace Unomata.Core;

/// <summary>
/// 接龙方向枚举。决定异色数字牌的合法性判定（严格 ±1 升降序）：
/// <list type="bullet">
/// <item><description><c>Ascending</c>：升序，下一张异色数字必须严格等于 <c>lastNumber + 1</c></description></item>
/// <item><description><c>Descending</c>：降序，下一张异色数字必须严格等于 <c>lastNumber - 1</c></description></item>
/// </list>
/// 同色任意数字合法，覆盖方向约束。反转牌切换方向，王牌不改变方向。会话开局默认 <c>Ascending</c>。
/// </summary>
public enum ChainDirection
{
    Ascending,
    Descending,
}
