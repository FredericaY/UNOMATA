# ARCHITECTURE.md — 架构说明

---

## 整体架构

```
┌──────────────────────────────────────────────────────────┐
│                        Unity 端                           │
│                                                          │
│  ┌─────────────┐   ┌──────────────┐   ┌──────────────┐  │
│  │  Gameplay   │   │      UI      │   │   Linking    │  │
│  │  TPS主线    │   │  副线UI表现  │   │  双线联动层  │  │
│  │  波次管理   │   │  骇入面板    │   │  事件→行为   │  │
│  │  敌人AI     │   │  HUD         │   │  系数写入    │  │
│  └──────┬──────┘   └──────┬───────┘   └──────┬───────┘  │
│         │                 │                  │           │
│         └─────────────────┴──────────────────┘           │
│                           │                              │
│              调用方法 / 订阅事件                           │
└───────────────────────────┼──────────────────────────────┘
                            │  接口边界（见 INTERFACE.md）
┌───────────────────────────▼──────────────────────────────┐
│                        Core 层                            │
│                   （纯C#，无Unity依赖）                    │
│                                                          │
│  ┌─────────────┐   ┌──────────────┐   ┌──────────────┐  │
│  │  HackSession│   │   CardDeck   │   │  HackResult  │  │
│  │  会话生命周期│   │  牌组生成    │   │  结算数据    │  │
│  │  计时/事件  │   │  合法牌比例  │   │  减免系数    │  │
│  └─────────────┘   └──────────────┘   └──────────────┘  │
└──────────────────────────────────────────────────────────┘
```

---

## 层级职责

### Core 层（`Unomata.Core`）
- 命名空间：`Unomata.Core`
- 路径：开发期位于独立 .NET 8 工程 `CardChainCore/src/Unomata.Core/`；Phase 4 复制至 Unity 内 `Assets/_Project/Scripts/Core/` 并配 `Unomata.Core.asmdef`（`noEngineReferences=true`）
- **严禁引用 `UnityEngine`**
- 职责：接龙规则（严格升降序 + 反转/王牌方向状态机）、牌组生成、计时、Combo检测（预留）、结算（basePot / maxPot 双层奖励池）

### Gameplay 层（`Unomata.Gameplay`）
- 命名空间：`Unomata.Gameplay`
- 路径：`Assets/_Project/Scripts/Gameplay/`
- 职责：TPS移动/射击、敌人AI、波次管理、骇入触发检测
- 基于 QFramework Architecture 分层（System / Model / Command）

### Linking 层（`Unomata.Gameplay.Linking`）
- 路径：`Assets/_Project/Scripts/Gameplay/Linking/`
- 职责：Core 事件 → Unity 行为的适配层
  - `OnChainFailed` → 扣玩家血量
  - `OnOverflow` → 生命回复充能
  - `OnSessionEnd` → 写入目标敌人 `DamageReductionFactor`

### UI 层（`Unomata.UI`）
- 命名空间：`Unomata.UI`
- 路径：`Assets/_Project/Scripts/UI/`
- 职责：骇入面板、HUD，纯表现层，监听事件驱动显示

---

## QFramework 架构分层

```
GameApp（Architecture入口）
├── Systems
│   ├── HackSystem        // 骇入会话管理，持有当前 HackSession
│   ├── WaveSystem        // 波次管理
│   └── PlayerSystem      // 玩家状态（血量、技能充能）
├── Models
│   ├── PlayerModel       // 玩家数据（HP、充能数）
│   └── WaveModel         // 当前波次数、难度参数
└── Commands
    ├── StartHackCommand  // 发起骇入
    ├── SelectCardCommand // 选牌
    └── HealCommand       // 触发生命回复
```

---

## 数据流向

```
[玩家操作]
    骇入键按下 → StartHackCommand
        → HackSystem 创建 HackSession
        → HackSession 事件 → Linking 层 → 敌人/玩家状态变更
        → HackSession 事件 → UI 层 → 界面刷新

    选牌点击 → SelectCardCommand
        → HackSystem.CurrentSession.SelectOption(index)
        → 触发 OnChainSuccess / OnChainFailed / OnMaxReached / OnOverflow

[每帧]
    HackSystem.Tick(deltaTime) → HackSession.Tick(deltaTime)
```

---

## 文件结构速查

### 当前 (Phase 1, Core 在独立 .NET 工程)

```
CardChainCore/
├── src/Unomata.Core/
│   └── CardChain/
│       ├── CardType.cs           // enum (Change 1)
│       ├── CardColor.cs          // enum (Change 1)
│       ├── ChainDirection.cs     // enum (Change 1)
│       ├── EndReason.cs          // enum (Change 1)
│       ├── ComboType.cs          // enum 占位 (Change 1)
│       └── CardData.cs           // class + Empty 单例 (Change 1)
│       // 后续 Change 2~6 在此追加: HackDifficultyConfig / HackSession / HackResult 等
├── tests/Unomata.Core.Tests/
│   └── CardChain/                // 镜像源码结构
└── console/Unomata.Core.Console/
    └── Program.cs                // Phase 1 末期由 Change 7 填充演示
```

### 目标 (Phase 4 迁入 Unity 后)

```
Assets/_Project/Scripts/
├── Core/                          // 由 CardChainCore/src/Unomata.Core/ 复制迁入
│   └── CardChain/
│       ├── CardType.cs / CardColor.cs / ChainDirection.cs / EndReason.cs / ComboType.cs
│       ├── CardData.cs
│       ├── HackDifficultyConfig.cs
│       ├── HackSession.cs
│       └── HackResult.cs
├── Gameplay/
│   ├── Player/
│   ├── Enemy/
│   ├── Wave/
│   └── Linking/
│       └── HackSessionLinker.cs   // Core 事件 → Unity 行为
└── UI/
    └── HackPanelController.cs
```
