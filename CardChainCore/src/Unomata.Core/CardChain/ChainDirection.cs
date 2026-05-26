namespace Unomata.Core;

/// <summary>
/// 接龙方向枚举。决定异色数字牌的合法性判定：
/// <list type="bullet">
/// <item><description><c>Ascending</c>：升序，下一张数字必须严格大于当前 lastNumber</description></item>
/// <item><description><c>Descending</c>：降序，下一张数字必须严格小于当前 lastNumber</description></item>
/// </list>
/// 反转牌切换方向，王牌不改变方向。会话开局默认 <c>Ascending</c>。
/// </summary>
public enum ChainDirection
{
    Ascending,
    Descending,
}
