# UNOMATA

UNOMATA is a dual-loop gameplay prototype: a third-person arena shooter combined with a UNO-inspired hacking card chain.

## Current status

Recovery accepted by the user on **2026-09-18**. The pre-restart baseline was commit `b501cde` (2026-06-01). This remains an early prototype without a complete gameplay loop.

- Implemented: card types, validation and option generation in the standalone Core; character/model integration, movement, aim cameras, QFramework input and audio bridges in Unity.
- Accepted and archived on **2026-09-18**: [over-shoulder aiming and locomotion repair](openspec/changes/archive/2026-09-18-fix-over-shoulder-aim-locomotion/proposal.md). SampleScene now provides consistent muzzle alignment and two-hand grips, smooth movement blending, forward-only aimed sprint, and continuous jump poses. A/D/S and diagonals walk while aiming even with Shift; ordinary sprint remains available. Automated geometry, movement, input, lifecycle and 30/60/120fps checks are recorded separately from the user's final visual, audio and focus-recovery acceptance. All 47 tasks are complete and four capabilities are synced to main specs. See the [animation record](Docs/AboutTheAnimation.md). The superseded IK change remains historical reference.
- Planned: complete hacking sessions/results, shooting, enemies, operational waves, hacking UI and integration.
- Recovery verified: .NET SDK 8.0.425 is installed; all 139 Core tests pass and Unity MCP/compilation work. The input repair now restores Aim/Fire and lifecycle handling, with 150 automated regression assertions passing. The user has accepted the restored baseline. The serialized runtime helper on Audio has been removed; two independent Play/Stop sessions and audio playback checks pass with no Console errors or warnings. The acceptance record distinguishes automated evidence from the user's overall phase acceptance; it does not invent extra physical-input test logs. See the [input baseline record](Docs/INPUT_BASELINE.md).

Start with the [restart review](Docs/PROJECT_REVIEW.md), [next steps](Docs/TODO.md) and [environment guide](Docs/DEVELOPMENT_SETUP.md).

## Environment and layout

Use **Unity 2022.3.62f3**, **URP 14.0.12** and **.NET 8 SDK** for the independent Core. QFramework is included as source assets, not as a manifest package. See the [dependency register](Docs/DEPENDENCIES.md).

| Path | Responsibility |
|---|---|
| `Assets/_Project/` | Project-owned Unity scripts, scenes, animations and assets |
| `Assets/_Project/Scenes/SampleScene.unity` | Current character prototype scene |
| `Assets/ThirdParty/` | Categorized vendor assets |
| `Assets/QFramework/`, `Assets/QFrameworkData/` | Framework source and configuration; preserve paths |
| `CardChainCore/` | Standalone Core library, xUnit tests and console scaffold |
| `Docs/` | Design, actual architecture, roadmap and verification evidence |
| `Tools/Check-Project.ps1` | Environment and documentation checks; optional Core/spec validation |
| `openspec/` | Existing tracked contracts, active changes and historical archives |

Open the project with the pinned Unity Editor, then open SampleScene. The Editor resolves Build Settings to the correct scene through its GUID; the previously recorded path concern is cleared. For Core, run `dotnet test CardChainCore/CardChainCore.sln`. The console currently prints a scaffold message. Personal AI entry files and generated Codex skills stay local; existing tracked OpenSpec history is preserved.

---

# UNOMATA（中文）

双线玩法原型：第三人称竞技场射击 + UNO 改编的接龙骇入，验证两条玩法同时运行的设计。

## 当前状态

**2026-09-18** 用户确认恢复阶段完成，项目已回到暂停开发时的基线。重启前基线为 `b501cde`（2026-06-01）；完整游戏循环仍未实现。

- 已实现：独立 Core 的类型、接牌判定、选项生成；Unity 的角色/模型整合、移动、瞄准相机、QFramework 输入与音频桥接。
- **2026-09-18 已验收并归档**：[移动与越肩瞄准修复](openspec/changes/archive/2026-09-18-fix-over-shoulder-aim-locomotion/proposal.md)。SampleScene 已具备一致枪口对齐、双手握枪、平滑运动混合、仅纯向前举枪奔跑及连续跳跃姿态。举枪 A/S/D 与斜向即使 Shift 也走路，普通奔跑保留。几何、动作、输入、生命周期及 30/60/120fps 自动证据与用户最终视觉、听感和失焦恢复验收分别记录；47 项任务全部完成，四项能力已同步主规格。见 [动画记录](Docs/AboutTheAnimation.md)。被替代的旧 IK change 保留历史参考。
- 后续计划：完整骇入会话与结算、射击、敌人、可运行波次、骇入 UI 与双线联动。
- 恢复验证：已安装 .NET SDK 8.0.425，本轮 139 项 Core 测试全部通过，Unity MCP 与编译可用。输入修复已补齐 Aim/Fire 和生命周期处理，150 个自动回归断言通过。用户已确认恢复基线可接受；Audio 运行时组件残留已清理，两次独立 Play/Stop 与音频播放复验通过，Console 零错误/警告；验收记录区分自动证据与用户整体阶段确认，不补写未提供的物理输入逐项日志，详见 [输入基线记录](Docs/INPUT_BASELINE.md)。

阅读顺序：[重启回顾](Docs/PROJECT_REVIEW.md) → [近期待办](Docs/TODO.md) → [环境恢复](Docs/DEVELOPMENT_SETUP.md) → [文档索引](Docs/README.md)。

环境固定为 **Unity 2022.3.62f3 + URP 14.0.12**；Core 使用 **.NET 8 SDK**。QFramework 已以源码资产导入。目录职责与上方英文表一致，依赖版本见 [DEPENDENCIES.md](Docs/DEPENDENCIES.md)。

使用指定 Editor 打开工程，再打开 `Assets/_Project/Scenes/SampleScene.unity`。Editor 已通过 GUID 将 Build Settings 解析到正确场景，上轮路径疑点已排除。Core 测试入口是 `dotnet test CardChainCore/CardChainCore.sln`，Console 目前仅输出 scaffold 提示。个人 AI 入口及 Codex skills 本地保留；原有已跟踪的 OpenSpec 历史继续保留。
