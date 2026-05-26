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
│  接龙规则 / 牌组生成 / 方向状态机 / 计时 / 得分 │
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

        // 玩家主动弃牌（或 Unity 端死局计时器超时调用），立即结算
        // 任何状态下调用都视为合法，会触发 OnSessionEnd(EndReason.Surrender)
        public void Surrender();

        // 每帧驱动计时（传入deltaTime），Unity端在Update中调用
        public void Tick(float deltaTime);

        // ── 查询属性 ──────────────────────────────────────
        public bool IsActive { get; }               // 会话是否进行中
        public int  ChainCount { get; }             // 当前已接龙数量
        public int  BasePot { get; }                // 满档基线（== TargetChainCount）
        public int  MaxPot { get; }                 // 当前奖励上限（动态，满档后冻结）
        public int  OverflowCount { get; }          // 满档后的溢出数量
        public bool IsMaxLatched { get; }           // 是否已进入满档（单向 latch）
        public float TimeRemaining { get; }         // 倒计时剩余秒数
        public CardData CurrentCard { get; }        // 当前需要接的牌（开局为 CardData.Empty）
        public CardData[] CurrentOptions { get; }   // 当前选项列表
        public ChainDirection CurrentDirection { get; } // 当前接龙方向

        // ── 事件（Unity端监听）───────────────────────────
        // 新一轮出牌，参数：当前牌（可能是 Empty/Reverse/Wild/Number）、选项列表、本轮是否死局
        // isDeadlock=true 表示选项中无任一合法牌，Unity 端可启动反应窗口计时器
        // 注意：含王牌的轮次永不死局（王牌永远合法）
        public event Action<CardData, CardData[], bool> OnNewRound;

        // 接牌成功，参数：已接数量
        public event Action<int> OnChainSuccess;

        // 接牌失败（选错牌），参数：已接数量（结算用）
        // 注意：是否扣血由 Unity 端依据 IsMaxLatched / chain >= BasePot 自行决定
        public event Action<int> OnChainFailed;

        // 倒计时归零自然结束，参数：已接数量
        public event Action<int> OnTimeUp;

        // 达到满档（chain >= maxPot 首次成立），参数：满档时的 maxPot 值（即冻结后的奖励上限）
        public event Action<int> OnMaxReached;

        // 满档后再多接一张合法牌，参数：当前溢出数量
        public event Action<int> OnOverflow;

        // 方向切换（反转牌触发），参数：新方向
        public event Action<ChainDirection> OnDirectionChanged;

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
        public int   TargetChainCount;   // basePot：满档基线（达到 100% 伤害去除所需接龙数）
        public float TotalTime;          // 本次骇入总倒计时（秒）
        public float SolvableRate;       // 本轮"有解"概率（0~1），由 Unity 端按 SyncRate 计算后传入
        public float WildAppearRate;     // 本轮王牌出现概率（0~1，独立判定，与 SolvableRate 解耦），建议默认 0.05
    }
}
```

> `TargetChainCount` 即文档中所称的 `basePot`，是 `DamageReductionFactor` 计算的分母。
> `MaxPot` 由会话内部根据玩家接到的反转/王牌数量动态计算，不在 config 中。
>
> `SolvableRate` 与旧字段 `ValidCardRatio` 不同：
> - 旧字段表示"每张选项独立合法的概率"（按张计算）
> - 新字段表示"本轮选项内至少存在一张合法牌的概率"（按轮计算）
> - 新机制下"有解轮"的合法牌张数固定为 1（参见第六节"发牌算法"）
>
> `WildAppearRate` 是独立小概率出现王牌的开关，**不影响 `isDeadlock` 之外的任何其它流程**。
> 王牌出现时会占用一个选项位（即非法牌位减少 1）。

---

### `CardData`
单张牌的数据结构。**取消** `CanFollow()` 方法（合法性判定迁入 `HackSession` 内部，因依赖会话方向状态）。

```csharp
namespace Unomata.Core
{
    public class CardData
    {
        public CardType   Type;     // Number / Reverse / Wild / Empty
        public CardColor? Color;    // Number/Reverse 有颜色；Wild/Empty 为 null
        public int?       Number;   // 仅 Number 类型有值（0~9）；其它类型为 null

        // 开局占位空牌的静态实例，UI 可识别此实例显示空白卡
        public static readonly CardData Empty;
    }

    public enum CardType
    {
        Number,    // 有色有数字（0~9）
        Reverse,   // 有色无数字，作为 next 时切换接龙方向
        Wild,      // 无色无数字，作为 next 永远合法，作为 prev 重置颜色和数字基准
        Empty      // 仅作开局 CurrentCard 占位，不出现在选项中
    }

    public enum CardColor { Red, Blue, Green, Yellow }

    public enum ChainDirection { Ascending, Descending }
}
```

#### 合法性判定（`HackSession` 内部，不暴露为 CardData 方法）

会话维护内部状态：

```csharp
// 伪代码（实现细节属 Core 层私有）
class SessionState
{
    public CardColor?    LastColor;     // 当前牌颜色（Wild/Empty 后为 null）
    public int?          LastNumber;    // 当前牌数字（Reverse/Wild/Empty 后为 null）
    public ChainDirection Direction;    // 初始 Ascending，反转牌切换
}

bool IsValidNext(CardData next, SessionState s)
{
    if (next.Type == CardType.Wild)    return true;       // 王牌永远合法
    if (next.Type == CardType.Empty)   return false;      // Empty 不应出现在选项中
    if (next.Type == CardType.Reverse) return s.LastColor == null
                                           || next.Color == s.LastColor;
    // Number
    if (next.Color == s.LastColor) return true;            // 同色任意数字合法
    if (s.LastNumber == null)      return true;            // 无数字基准 → 任意数字合法
    return s.Direction == ChainDirection.Ascending
        ? next.Number > s.LastNumber
        : next.Number < s.LastNumber;
}

void ApplyPrev(CardData prev, SessionState s)
{
    switch (prev.Type)
    {
        case CardType.Number:
            s.LastColor = prev.Color; s.LastNumber = prev.Number; break;
        case CardType.Reverse:
            s.LastColor = prev.Color; s.LastNumber = null;
            s.Direction = s.Direction == ChainDirection.Ascending
                ? ChainDirection.Descending : ChainDirection.Ascending;
            break;
        case CardType.Wild:
            s.LastColor = null; s.LastNumber = null; break;
        case CardType.Empty:
            // 开局占位，不会作为玩家打出的 prev
            break;
    }
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
        public int  ChainCount;          // 实际接龙数
        public int  BasePot;             // 满档基线（== config.TargetChainCount）
        public int  MaxPot;              // 满档时冻结的 maxPot；未满档则为最后一刻的动态值
        public int  OverflowCount;       // 溢出数量（仅满档后累计）
        public bool IsMaxReached;        // 是否达到满档
        public EndReason Reason;         // 结束原因

        // 计算伤害减免削减系数（>= 0，无上限 clamp）
        // 注意：实现时用 System.Math.Max 替代 Mathf.Max，Core 层禁止引用 UnityEngine
        public float DamageReductionFactor =>
            BasePot > 0 ? System.Math.Max(0f, (float)ChainCount / BasePot) : 0f;
    }

    public enum EndReason { TimeUp, WrongCard, Surrender }
}
```

> 关键变更：旧公式 `Math.Clamp(... 0, 1)` 已废除。
> `factor` 可超过 1.0，超出部分代表"满档 + 特殊牌"换来的额外受击加成。
> 是否扣血由 Unity 端依据 `Reason` 与 `IsMaxReached` 判定（详见第三节）。

---

### `ComboType`（预留枚举，v1不实现）

```csharp
namespace Unomata.Core
{
    public enum ComboType
    {
        None,
        SameColorTwice,        // 连续两次同色 → 时间延长
        SameDirectionTwice,    // 连续两次符合方向数字 → 额外伤害加成
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
| 调用 `Surrender()` | 玩家主动按弃牌键，或死局反应窗口超时 |
| 监听所有事件 | 驱动UI表现、扣血、结算伤害减免、充能 |
| 维护 `SyncRate` 状态 | 战术资源（详见 `GAME_DESIGN.md` 同步率章节），用于计算 `SolvableRate` 传入 config |
| 死局反应窗口管理 | 收到 `OnNewRound(..., isDeadlock=true)` 后启动倒计时；玩家窗口内 `Surrender()` 给奖励，超时则 Unity 主动 `Surrender()` 不奖励 |
| 将 `HackResult` 应用到目标敌人 | 修改对应敌人的伤害减免；factor > 1 时附加受击加成 |

### 扣血判定（Unity 端基于事件）

```
OnChainFailed 触发时：
  Phase 1 占位：扣固定血量（不论是否满档）
  Phase 5 平衡：根据 SyncRate / 是否满档 / 等因素调整
  最终具体规则待 GAME_DESIGN.md 第 3.7.2 节"惩罚机制（待平衡）"敲定

OnTimeUp 触发时：
  → 永远不扣血

OnSessionEnd 携带的 HackResult.Reason 区分原因：
  - WrongCard: 接错；扣血与否依上面规则
  - TimeUp:    超时；不扣血
  - Surrender: 主动弃牌（玩家按键或死局超时强退）；不扣血
```

### 死局反应窗口（Unity 端）

```
OnNewRound(card, options, isDeadlock):
  if isDeadlock:
      deadlockStartTime = Time.time
      启动 UI 警告（红色边框 / 闪烁）
  else:
      deadlockStartTime = -1

弃牌键按下:
  if deadlockStartTime > 0 && Time.time - deadlockStartTime <= DEADLOCK_WINDOW_SEC:
      → SyncRate 奖励（数值由 Unity 端常量决定）
  session.Surrender()

每帧:
  if deadlockStartTime > 0 && 已超 DEADLOCK_WINDOW_SEC:
      session.Surrender()    // 不奖励
      deadlockStartTime = -1
```

**反应窗口时长 `DEADLOCK_WINDOW_SEC`、奖励量 `DEADLOCK_BREAKTHROUGH_REWARD` 全部由 Unity 端配置，Core 不可见。**

---

## 四、典型调用流程

```
[玩家按下骇入键]
    → Unity端 Raycast 检测目标
    → 命中 → 按 SyncRate 生成 HackDifficultyConfig
       SolvableRate = 0.5 + 0.45 × syncRate
       WildAppearRate = 0.05 (常量, 后续可调)
    → new HackSession(config)
    → 订阅所有事件
    → session.Start()
        → 触发 OnNewRound(CardData.Empty, options[], isDeadlock=false)
        → CurrentCard 初始为 Empty，CurrentDirection = Ascending

[每帧]
    → session.Tick(Time.deltaTime)
    → 检查死局窗口超时 → 必要时 session.Surrender()

[玩家点击选项卡]
    → session.SelectOption(index)

[玩家按下弃牌键]
    → 判断是否在死局窗口内 → 决定 SyncRate 奖励
    → session.Surrender()

[监听事件]
    → OnNewRound          → 刷新UI显示当前牌 / 选项 / 方向箭头 / 死局警告
    → OnChainSuccess      → 更新接龙计数UI
    → OnDirectionChanged  → UI 播放方向切换动画
    → OnMaxReached        → 播放满档特效，UI 标记"无负担接龙"模式
    → OnOverflow          → 充能技能 +1
    → OnChainFailed       → 按惩罚规则处理（待平衡），关闭UI
    → OnTimeUp            → 关闭UI（不扣血）
    → OnSessionEnd        → 将 result.DamageReductionFactor 写入目标敌人
                            （factor > 1 时附加额外受击加成）
```

---

## 五、发牌算法（Core 内部约定）

每轮选项生成的伪代码：

```
generate_options(state, config):
    isSolvable = roll(config.SolvableRate)      // 本轮是否有解
    hasWild    = roll(config.WildAppearRate)    // 本轮是否塞王牌
    
    options = []
    
    if hasWild:
        options.append(Wild)                    // 占用 1 个选项位
    
    if isSolvable:
        legalCard = pick_random_legal(state, deck)   // 抽 1 张当前 state 下合法的牌
        options.append(legalCard)               // 有解轮固定 1 张合法牌
    
    // 剩余位置全部填非法牌
    while len(options) < config.OptionCount:
        illegalCard = pick_random_illegal(state, deck)
        options.append(illegalCard)
    
    shuffle(options)
    isDeadlock = options 中无任一 IsValidNext(opt, state) == true
    return (options, isDeadlock)
```

四种轮次组合：

| isSolvable | hasWild | 实际有解？ | isDeadlock | 选项构成（OptionCount=3 示例） |
|------------|---------|-----------|------------|------------------------------|
| true  | true  | 是 | false | 1 王牌 + 1 合法 + 1 非法 |
| true  | false | 是 | false | 1 合法 + 2 非法 |
| false | true  | 是 | false | 1 王牌 + 2 非法 |
| false | false | 否 | **true** | 3 非法 |

约束：
- 选项内**不重复**（同一轮 5 张选项必须两两不同；跨轮可重）
- 反转牌**不强塞**，只在 `pick_random_legal` 抽到时自然出现
- 王牌**只通过 `WildAppearRate` 强塞**，不进入 `pick_random_legal/illegal` 池
- `Empty` 牌仅作 `CurrentCard` 占位，**永不出现在选项中**

> 概率示例（`SyncRate → SolvableRate` 假设线性 0.5~0.95 + WildAppearRate=0.05）：
>
> | SyncRate | SolvableRate | 实际死局率 | 含义 |
> |----------|--------------|-----------|------|
> | 1.0      | 0.95         | 5% × 95% = 4.75% | 满同步率几乎稳过 |
> | 0.5      | 0.725        | 27.5% × 95% ≈ 26% | 中位偶尔卡死 |
> | 0.0      | 0.50         | 50% × 95% ≈ 47.5% | 零同步率每轮约一半概率死局 |

---

## 六、待确认事项

| 事项 | 状态 |
|------|------|
| 骇入效果持续时间（秒） | 待定，暂用常量占位 |
| 生命回复技能充能缓存上限 | 待定，平衡调整后确认 |
| 接错牌惩罚机制（扣血/掉同步率/两者） | 待定，参见 `GAME_DESIGN.md` 3.7.2，Phase 5 平衡 |
| 满档后额外伤害加成基线（factor=1.0 之上 +1 是否正比） | 暂按 `(factor - 1.0)` 比例线性附加 |
| 死局反应窗口时长 / 突破奖励量 | Unity 端常量，待 Phase 5 平衡 |
| `WildAppearRate` 默认值 | 暂定 0.05，Phase 5 平衡 |
| `SyncRate → SolvableRate` 映射公式 | 暂定 `0.5 + 0.45 × syncRate`，Phase 5 平衡 |
