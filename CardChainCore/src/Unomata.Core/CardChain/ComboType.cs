namespace Unomata.Core;

/// <summary>
/// Combo 类型占位枚举。v1 不实现任何检测逻辑，所有运行时取值始终为 <c>None</c>。
/// 后续版本扩展：
/// <list type="bullet">
/// <item><description><c>SameColorTwice</c>：连续两次同色 → 时间延长</description></item>
/// <item><description><c>SameDirectionTwice</c>：连续两次符合方向数字 → 额外伤害加成</description></item>
/// </list>
/// </summary>
public enum ComboType
{
    None,
    SameColorTwice,
    SameDirectionTwice,
}
