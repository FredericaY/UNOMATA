## Why

Phase 1 Core 层开发的第一步是把所有领域数据类型与枚举先固化下来。后续每一个 change（validator / deck-generator / hacksession）都要消费这些类型，没有它们就没法写。把它们独立成一个最小的 change 有两个好处：①类型层零逻辑，最容易做到一次写对、不需要回滚；②给后续 change 一个稳定的依赖根，避免边写逻辑边动类型。

## What Changes

- 新增 `Unomata.Core` 领域类型与枚举（纯定义，不含任何方法逻辑）：
  - `enum CardType { Number, Reverse, Wild, Empty }`
  - `enum CardColor { Red, Blue, Green, Yellow }`
  - `enum ChainDirection { Ascending, Descending }`
  - `enum EndReason { TimeUp, WrongCard, Surrender }`
  - `enum ComboType { None, SameColorTwice, SameDirectionTwice }`（v1 仅占位，无逻辑）
  - `class CardData`：`Type` / 可空 `Color` / 可空 `Number` 三字段
  - `static readonly CardData CardData.Empty` 单例，专用于 `HackSession.CurrentCard` 开局占位
- 新增 xUnit 测试套件覆盖类型基本性质（枚举值齐全、`CardData.Empty` 不变性、不同 `Type` 下可空字段的约定）
- 不引入任何接龙规则、状态机、会话逻辑（这些进后续 change）

## Capabilities

### New Capabilities

- `cardchain-types`：Core 层接龙领域的纯数据类型与枚举定义。涵盖牌的颜色/类型/数字三元组、接龙方向、会话结束原因、Combo 占位枚举、`CardData.Empty` 占位实例，是 `Unomata.Core` 命名空间所有后续模块的依赖根。

### Modified Capabilities

（无）

## Impact

- 新增源文件目录：`CardChainCore/src/Unomata.Core/CardChain/`
  - `CardData.cs` / `CardType.cs` / `CardColor.cs` / `ChainDirection.cs` / `EndReason.cs` / `ComboType.cs`
- 新增测试目录：`CardChainCore/tests/Unomata.Core.Tests/CardChain/`
  - `CardDataTests.cs` 等
- 不动 `cardchain-core-scaffold`（脚手架已稳定）
- 不动任何 Unity 端代码（Unity 端 Phase 4 才接入）
- 接口契约对齐 `Docs/INTERFACE.md` 第二节"Core 层对外暴露的类"
