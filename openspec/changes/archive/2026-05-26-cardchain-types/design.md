## Context

本 change 是 `Phase 1 — Core 层开发`的第一步，对应 `Docs/TODO.md` 中 Change 1 (`cardchain-types`)。设计目标是把所有领域数据类型固定下来，作为后续 6 个 change（validator / deck-generator / hacksession-* / console-demo）的依赖根。

约束：
- 必须严守 `Docs/INTERFACE.md` 第二节"Core 层对外暴露的类"中关于 `CardData` 的最新定义（取消 `CanFollow` 方法，改为 `Type/Color/Number` 三字段 + `CardData.Empty` 静态实例）
- 必须遵守 `cardchain-core-scaffold` 已归档 spec：Core 项目零 `PackageReference`、零 `ProjectReference`、零 `UnityEngine` 引用
- 整个 Core 项目用 `Nullable=enable`、`TreatWarningsAsErrors=true`，意味着可空字段必须用 `?` 标注

## Goals / Non-Goals

**Goals:**
- 一次写对所有类型定义，后续 change 不再回头改 `CardData` 形状或枚举成员
- 类型层零业务逻辑：所有规则判定（`IsValidNext`、`ApplyPrev`、发牌等）一律推迟到 Change 2+
- 测试即文档：每个枚举/字段约定都有可执行的 xUnit case
- 文件组织上为后续 change 留好放置位置（`CardChain/` 子目录）

**Non-Goals:**
- 不实现任何接龙规则、状态机、会话生命周期（属于后续 change）
- 不动 Unity 端任何代码（Phase 4 才接入）
- 不写 `CanFollow`、不写 `CardData.IsValid` 等方法（合法性判定永久迁入 `HackSession` 内部）
- 不实现 `ComboType` 检测逻辑（v1 永远返回 `None`，由后续 `HackSession` change 履约）

## Decisions

### D1：`CardData` 用 `class` 而非 `record` 或 `struct`

**选择**：`public sealed class CardData`，可变字段。

**理由**：
- 后续 `HackSession` 和发牌器持有 `CardData` 引用并在事件中传递。`class` 引用语义对调试与 `ReferenceEquals(CardData.Empty, x)` 这种场景更直观
- `record` 自动值相等会在跨轮选项比较中带来意外（如不同轮抽到的 Red-5 用值相等会被认为同一张，但语义上是不同回合的卡面）。后续 change 可能需要"按引用区分"的能力
- `struct` 有可空字段（`Color?` / `Number?`），传值开销不利于事件订阅链路
- `sealed` 防止外部继承导致的不变量破坏

**替代方案**：
- `record class`：值相等带来的隐性 bug 风险大于语法糖收益，否决
- `readonly record struct`：可空成员让 struct 体积膨胀，不划算

### D2：可空字段用 `?` 标注，不用哨兵值

**选择**：`Color` 用 `CardColor?`、`Number` 用 `int?`，禁用 `CardColor.None` / `Number = -1` 等哨兵。

**理由**：
- 项目已开 `Nullable=enable`，这是 .NET 8 习惯写法
- 哨兵值需要在 `CardColor` 枚举里加 `None`，污染颜色枚举本意（4 色）；调用方还要记住"哨兵不是真颜色"
- 可空类型让"不存在颜色 / 数字"的语义在类型系统层面就强制表达，配合后续判定逻辑天然更安全（NRE 编译期可见）

**替代方案**：
- 在 `CardColor` 加 `None`：被否，破坏枚举语义
- 用 `OneOf<int, None>` 之类第三方库：被否，违反 Core 零依赖

### D3：`CardData.Empty` 用 `static readonly` 字段而非属性或工厂

**选择**：

```csharp
public sealed class CardData
{
    public static readonly CardData Empty = new()
    {
        Type = CardType.Empty,
        Color = null,
        Number = null
    };
    // ...
}
```

**理由**：
- `static readonly` 在类型加载时初始化一次，多次访问保证同一引用，符合 spec 中"单例语义"要求
- 字段而非属性：避免每次访问都生成 IL `getter` 调用；意图更纯（不变量 + 不可重赋）
- 不用静态工厂方法 `CreateEmpty()`：那种写法每次返回新实例，不满足单例约束

**替代方案**：
- 静态属性：等价但多一层 IL 间接，无收益
- 在 `CardType` 加 `EmptyInstance` 字段：违反职责分离（`CardType` 是枚举不持卡）

### D4：每个枚举一文件，`CardData` 单独一文件

**选择**：

```
CardChainCore/src/Unomata.Core/CardChain/
├── CardType.cs           // enum
├── CardColor.cs          // enum
├── ChainDirection.cs     // enum
├── EndReason.cs          // enum
├── ComboType.cs          // enum
└── CardData.cs           // class + Empty
```

**理由**：
- 项目 rules `单文件不超过 300 行` + `文件名 = 类名` 约定，每个公开类型独立文件最稳
- 后续 change 加 `HackSession` / `HackResult` 等也按"一类一文件"放在 `CardChain/` 同级，保持一致

**替代方案**：
- 所有枚举塞一个 `Enums.cs`：违反"文件名 = 类名"约定，否决

### D5：测试文件组织镜像源码结构

**选择**：

```
CardChainCore/tests/Unomata.Core.Tests/CardChain/
├── CardTypeTests.cs
├── CardColorTests.cs
├── ChainDirectionTests.cs
├── EndReasonTests.cs
├── ComboTypeTests.cs
└── CardDataTests.cs
```

每个测试文件用 `Theory` 或 `Fact` 覆盖对应 spec 的 Scenario。

**理由**：
- 镜像目录结构便于"找到一个类的测试在哪"
- 单文件聚焦单一类型，调试快
- 命名空间统一 `Unomata.Core.Tests.CardChain`，与源码 `Unomata.Core` 平行

### D6：枚举值序号显式 vs 隐式

**选择**：不显式标注序号（让编译器自动从 0 开始）。

**理由**：
- v1 不会序列化到磁盘或网络，序号变更不会破坏外部契约
- 显式标 `Number = 0` 增加噪音
- 如未来需要稳定序号（持久化/网络协议），届时再加，破坏性可控

## Risks / Trade-offs

- **[风险] `CardData` 用 class + 可变字段，外部代码可能在拿到引用后修改字段** → 缓解：spec 要求 `CardData` 不暴露 `CanFollow` 等方法是一个层面；后续 change 在引发"已发出的牌被改"问题时，可以把字段改为 `init;` only（C# 9+ 已开），不破坏现有调用
- **[风险] `CardData.Empty` 是静态可变字段（语法上可被反射改）** → 缓解：`readonly` 字段防止重赋值；字段本身的可变性靠"项目内规约"约束（rules.mdc 已明令禁止跨模块乱改 Empty）
- **[风险] 后续 change 可能发现需要在 `CardData` 加新字段（如未来 Combo 标志）** → 缓解：本 change 只锁定当前 3 个字段；新增字段属于"扩展"而非"破坏"，到时新开一个 change 即可，遵守 OpenSpec 工作流
- **[Trade-off] 不实现任何方法**意味着调用方拿到 `CardData` 后无法直接问"你能接吗"，必须经 `HackSession`：这是有意为之，避免合法性判定散落多处

## Migration Plan

不涉及迁移：本 change 是新增类型，没有需要替换或弃用的旧 API。`cardchain-core-scaffold` 已归档 spec 不受影响（脚手架与领域类型正交）。

## Open Questions

无。所有设计决策在上方 D1~D6 中固化，剩余待平衡项（数值层面）已在 `Docs/GAME_DESIGN.md` 附录 A 归档，不属于本 change 范围。
