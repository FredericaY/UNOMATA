namespace Unomata.Core;

/// <summary>
/// 骇入会话结束原因枚举：
/// <list type="bullet">
/// <item><description><c>TimeUp</c>：倒计时归零，自然结束（不扣血）</description></item>
/// <item><description><c>WrongCard</c>：玩家选中非法牌，会话中断</description></item>
/// <item><description><c>Surrender</c>：玩家主动按弃牌键，或 Unity 端死局窗口超时强退</description></item>
/// </list>
/// 旧版 <c>Manual</c> 已废弃，使用 <c>Surrender</c> 替代。
/// </summary>
public enum EndReason
{
    TimeUp,
    WrongCard,
    Surrender,
}
