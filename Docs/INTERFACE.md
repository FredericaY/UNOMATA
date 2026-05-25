# INTERFACE.md — 底层接口约定

> 本文档定义 `Core` 层（纯C#）与 `Unity` 端之间的所有交互接口。
> 两端开发必须严格遵守此约定，禁止绕过此文档直接跨层调用。
> 接口变更需双方确认后更新本文档，再同步实现。

---

## 一、架构边界

```
┌─────────────────────────────────────────────┐
│              Unity 端（Unomata.Gameplay）      │
│  TPS主线 / 骇入触发 / 敌人管理 / UI表现         │
│                                              │
│  调用 Core 公开方法 ──→  监听 Core 事件        │
└───────────────────┬──────────────────────────┘
                    │ 接口边界（本文档范围）
┌───────────────────▼──────────────────────────┐
│              Core 层（Unomata.Core）           │
│  接龙规则 / 牌组生成 / 计时 / Combo / 得分      │
│  严禁引用 UnityEngine                          │
└─────────────────────────────────────────────┘
```

---

## 二、Core 层对外暴露的类

### `HackSession`
单次骇入会话的完整生命周期管理。每次骇入新建一个实例。

```csharp
namespace Unomata.Core
{
    public class HackSession
    {
        // ── 构造 ──────────────────────────────────────────
        // 传入本次骇入的难度参数
        public HackSession(HackDifficultyConfig config);

        // ── 驱动方法（Unity端调用）────────────────────────
        // 开始计时，开始第一轮出牌
        public void Start();

        // 玩家选牌，传入选项索引（0 ~ config.OptionCount-1）
        public void SelectOption(int optionIndex);

        // 每帧驱动计时（传入deltaTime），Unity端在Update中调用
        public void Tick(float deltaTime);

        // ── 查询属性 ──────────────────────────────────────
        public bool IsActive { get; }               // 会话是否进行中
        public int  ChainCount { get; }             // 当前已接龙数量
        public int  TargetCount { get; }            // 满档所需接龙数量
        public int  OverflowCount { get; }          // 超出满档的溢出数量
        public float TimeRemaining { get; }         // 倒计时剩余秒数
        public CardData CurrentCard { get; }        // 当前需要接的牌
        public CardData[] CurrentOptions { get; }   // 当前选项列表

        // ── 事件（Unity端监听）───────────────────────────
        // 新一轮出牌，参数：当前牌、选项列表
        public event Action<CardData, CardData[]> OnNewRound;

        // 接牌成功，参数：已接数量
        public event Action<int> OnChainSuccess;

        // 接牌失败（选错牌），参数：已接数量（结算用）
        public event Action<int> OnChainFailed;

        // 倒计时归零自然结束，参数：已接数量
        public event Action<int> OnTimeUp;

        // 达到满档，参数：满档所需数量
        public event Action<int> OnMaxReached;

        // 溢出接龙+1，参数：当前溢出数量
        public event Action<int> OnOverflow;

        // 会话结束（任意原因），参数：结算结果
        public event Action<HackResult> OnSessionEnd;

        // ── 预留接口（v1不实现）──────────────────────────
        // 预留：多目标骇入扩展用，v1始终为null
        public string TargetId { get; set; }

        // 预留：Combo状态，v1始终为ComboType.None
        public event Action<ComboType> OnComboTriggered;
    }
}
```

---

### `HackDifficultyConfig`
难度参数数据类，由 Unity 端根据当前波次生成后传入 `HackSession`。

```csharp
namespace Unomata.Core
{
    public class HackDifficultyConfig
    {
        public int   OptionCount;        // 选项数量（初期3，后期5）
        public int   TargetChainCount;   // 满档所需接龙数量
        public float TotalTime;          // 本次骇入总倒计时（秒）
        public float ValidCardRatio;     // 选项中可接牌的占比（0~1）
    }
}
```

---

### `CardData`
单张牌的数据结构。

```csharp
namespace Unomata.Core
{
    public class CardData
    {
        public CardColor Color;   // 牌的颜色
        public int       Number;  // 牌的数字

        // 判断 other 是否能接在本牌后面
        public bool CanFollow(CardData other);
    }

    public enum CardColor { Red, Blue, Green, Yellow }
}
```

---

### `HackResult`
骇入结算结果，由 `OnSessionEnd` 事件携带。

```csharp
namespace Unomata.Core
{
    public class HackResult
    {
        public int   ChainCount;          // 实际接龙数
        public int   TargetCount;         // 满档所需数
        public int   OverflowCount;       // 溢出数量
        public bool  IsMaxReached;        // 是否达到满档
        public EndReason Reason;          // 结束原因

        // 计算伤害减免削减系数（0.0 ~ 1.0）
        public float DamageReductionFactor => 
            Mathf.Clamp01((float)ChainCount / TargetCount);
    }

    public enum EndReason { TimeUp, WrongCard, Manual }
}
```

---

### `ComboType`（预留枚举，v1不实现）

```csharp
namespace Unomata.Core
{
    public enum ComboType
    {
        None,
        SameColorTwice,    // 连续两次同色 → 时间延长
        SameNumberTwice,   // 连续两次同数字 → 额外伤害加成
    }
}
```

---

## 三、Unity 端职责

Unity 端**不实现任何接龙规则逻辑**，只负责：

| 职责 | 说明 |
|------|------|
| 生成 `HackDifficultyConfig` | 根据当前波次数计算参数 |
| 创建 `HackSession` | 骇入命中目标后 `new HackSession(config)` |
| 调用 `Start()` | 命中确认后启动会话 |
| 每帧调用 `Tick(deltaTime)` | 驱动计时 |
| 调用 `SelectOption(index)` | 将玩家UI点击转为索引传入 |
| 监听所有事件 | 驱动UI表现、扣血、结算伤害减免、充能 |
| 将 `HackResult` 应用到目标敌人 | 修改对应敌人的 `DamageReductionFactor` |

---

## 四、典型调用流程

```
[玩家按下骇入键]
    → Unity端 Raycast 检测目标
    → 命中 → 生成 HackDifficultyConfig（按波次）
    → new HackSession(config)
    → 订阅所有事件
    → session.Start()

[每帧]
    → session.Tick(Time.deltaTime)

[玩家点击选项卡]
    → session.SelectOption(index)

[监听事件]
    → OnNewRound       → 刷新UI显示当前牌和选项
    → OnChainSuccess   → 更新接龙计数UI
    → OnChainFailed    → 播放失败特效，扣玩家血量，关闭UI
    → OnTimeUp         → 播放结束特效，关闭UI
    → OnMaxReached     → 播放满档特效
    → OnOverflow       → 生命回复技能充能+1
    → OnSessionEnd     → 将 result.DamageReductionFactor 写入目标敌人
```

---

## 五、待确认事项

| 事项 | 状态 |
|------|------|
| 骇入效果持续时间（秒） | 待定，暂用常量占位 |
| 生命回复技能充能缓存上限 | 待定，平衡调整后确认 |
| 接错牌扣血量 | 待定，暂用常量占位 |
| 满档后额外伤害加成数值 | 待定，暂用常量占位 |
