namespace Unomata.Core;

/// <summary>
/// 单次骇入会话的难度参数。由 Unity 端按当前波次 + 同步率生成后传入 <c>HackSession</c>。
/// 字段语义详见 <c>Docs/INTERFACE.md</c> 第二节 <c>HackDifficultyConfig</c> 与第五节"发牌算法"。
/// 纯数据载体，无逻辑，无方法。
/// </summary>
public sealed class HackDifficultyConfig
{
    /// <summary>
    /// 每轮选项数量。典型范围 3 ~ 5（早期 3，后期 5），由 Unity 端按波次曲线给出。
    /// </summary>
    public int OptionCount;

    /// <summary>
    /// 目标接龙次数（basePot）。决定 <c>HackResult.DamageReductionFactor = chain / basePot</c> 的分母。
    /// 与 maxPot / 满档 / 溢出无关——maxPot 由反转/王牌动态扩张。
    /// </summary>
    public int TargetChainCount;

    /// <summary>
    /// 总倒计时秒数。由 <c>HackSession.Tick(deltaTime)</c> 累计。
    /// </summary>
    public float TotalTime;

    /// <summary>
    /// 本轮选项中至少存在一张合法牌的概率（**下界语义**），取值 [0, 1]。
    /// 实际有解率 ≥ 配置值——在 <c>(null, null, *)</c> 等小池状态下被合法位扩展守卫强制提升至 100%。
    /// </summary>
    public float SolvableRate;

    /// <summary>
    /// 本轮选项中塞 1 张王牌的独立概率，取值 [0, 1]。
    /// 王牌不进入 deck 抽样池，独立判定后占 1 个选项位。
    /// </summary>
    public float WildAppearRate;
}
