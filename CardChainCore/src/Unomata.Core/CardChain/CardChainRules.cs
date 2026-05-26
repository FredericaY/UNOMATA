namespace Unomata.Core;

/// <summary>
/// 接龙规则的纯函数实现。包含合法性判定 <see cref="IsValidNext"/>
/// 与状态更新 <see cref="ApplyPrev"/>，是 <c>HackSession</c> 的语义内核。
/// 严格遵循 <c>Docs/GAME_DESIGN.md</c> 第 3.5 节的匹配规则与 3.5.2 节的状态更新规则。
/// </summary>
internal static class CardChainRules
{
    /// <summary>
    /// 判定 <paramref name="next"/> 是否能合法接在当前 <paramref name="state"/> 之后。
    /// 规则：
    /// <list type="bullet">
    /// <item><description><see cref="CardType.Wild"/>：永远合法</description></item>
    /// <item><description><see cref="CardType.Empty"/>：永远非法（仅作开局占位，不应进入选项）</description></item>
    /// <item><description><see cref="CardType.Reverse"/>：同色或 LastColor 为 null 时合法</description></item>
    /// <item><description><see cref="CardType.Number"/>：LastColor 为 null（开局/王牌后）任意合法；
    /// 否则同色任意合法；否则 LastNumber 为 null（反转后）异色全非法；
    /// 否则按方向严格 ±1：Asc 时 N'==last+1，Desc 时 N'==last-1</description></item>
    /// </list>
    /// 本方法不修改 <paramref name="state"/> 与 <paramref name="next"/>。
    /// </summary>
    internal static bool IsValidNext(CardData next, SessionState state)
    {
        return next.Type switch
        {
            CardType.Wild => true,
            CardType.Empty => false,
            CardType.Reverse => state.LastColor is null
                                || next.Color == state.LastColor,
            CardType.Number => IsValidNumber(next, state),
            _ => throw new ArgumentOutOfRangeException(nameof(next)),
        };
    }

    /// <summary>
    /// 数字牌作为 next 的合法性判定。规则按短路顺序：
    /// <list type="number">
    /// <item><description><c>state.LastColor is null</c>（开局或刚接王牌后）→ 任意合法</description></item>
    /// <item><description>同色（<c>next.Color == state.LastColor</c>）→ 任意数字合法（覆盖方向约束）</description></item>
    /// <item><description><c>state.LastNumber is null</c> 且 LastColor 非 null（刚接反转牌后）→ 异色数字全非法</description></item>
    /// <item><description>否则按 <see cref="SessionState.Direction"/> 严格 ±1 判定：
    /// Asc 时需 <c>next.Number == state.LastNumber + 1</c>，Desc 时需 <c>next.Number == state.LastNumber - 1</c></description></item>
    /// </list>
    /// 严格 ±1 升降序由 D7 决策固化（详见 design.md），异色合法解唯一。
    /// </summary>
    private static bool IsValidNumber(CardData next, SessionState state)
    {
        if (state.LastColor is null)
        {
            return true;
        }

        if (next.Color == state.LastColor)
        {
            return true;
        }

        if (state.LastNumber is null)
        {
            return false;
        }

        return state.Direction == ChainDirection.Ascending
            ? next.Number == state.LastNumber + 1
            : next.Number == state.LastNumber - 1;
    }

    /// <summary>
    /// 用 <paramref name="prev"/> 原地更新 <paramref name="state"/>。每个 CardType 的更新规则：
    /// <list type="bullet">
    /// <item><description><see cref="CardType.Number"/>：LastColor / LastNumber 写入 prev 值，Direction 不变</description></item>
    /// <item><description><see cref="CardType.Reverse"/>：LastColor 写入 prev 值，LastNumber 清 null，Direction 翻转</description></item>
    /// <item><description><see cref="CardType.Wild"/>：LastColor / LastNumber 全清 null，Direction 不变</description></item>
    /// <item><description><see cref="CardType.Empty"/>：抛 <see cref="InvalidOperationException"/>，Empty 不应作为 prev 出现</description></item>
    /// </list>
    /// 本方法仅修改 <paramref name="state"/>，不修改 <paramref name="prev"/>。
    /// </summary>
    /// <exception cref="InvalidOperationException"><paramref name="prev"/> 类型为 <see cref="CardType.Empty"/></exception>
    /// <exception cref="ArgumentOutOfRangeException">未知 CardType（防御未来枚举扩展）</exception>
    internal static void ApplyPrev(CardData prev, SessionState state)
    {
        switch (prev.Type)
        {
            case CardType.Number:
                state.LastColor = prev.Color;
                state.LastNumber = prev.Number;
                break;

            case CardType.Reverse:
                state.LastColor = prev.Color;
                state.LastNumber = null;
                state.Direction = state.Direction == ChainDirection.Ascending
                    ? ChainDirection.Descending
                    : ChainDirection.Ascending;
                break;

            case CardType.Wild:
                state.LastColor = null;
                state.LastNumber = null;
                break;

            case CardType.Empty:
                throw new InvalidOperationException(
                    "Empty card must not be applied as prev. " +
                    "Empty is only used as the initial CurrentCard placeholder.");

            default:
                throw new ArgumentOutOfRangeException(nameof(prev));
        }
    }
}
