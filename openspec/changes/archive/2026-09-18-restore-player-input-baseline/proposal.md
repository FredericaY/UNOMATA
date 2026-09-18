## Why

2026-09-17 恢复运行时，项目输入资产缺少 Aim / Fire，使 PlayerController 在启动和退出时抛出空引用异常，角色基线无法验收。现有组件还有重新启用不恢复订阅、残留输入、瞄准命令绕过输入 Model，以及主规格与实际 Player Action Map / Look 路由不一致的问题，须在继续 IK 调试前修复。

## What Changes

- 在现有项目副本的 **Player** Action Map 中持久化 Aim（鼠标右键）和 Fire（鼠标左键），保留资产 GUID、已有 Move/Look/Jump/Sprint 及其绑定；重新导入/重开工程后仍完整。
- 对必要引用、Action Map 和动作进行完整校验；配置错误时保持输入中立、报告具体缺项，安全禁用/销毁，避免半套订阅和空引用。
- 修复 PlayerController / SAInputAdapter 的启用、禁用、重启、失焦和输入停用边界，确保不会重复回调或残留移动/瞄准/开火状态。
- 统一瞄准链路为 Command → PlayerInputModel → PlayerSystem → PlayerModel / AimStateChangedEvent；同值写入不重复广播。
- 明确 Model → StarterAssets 的消费时序、Jump 按下一次的消费边界，以及 Sprint 实际值归零；修正规格中不成立的 Look 默认路由说明，保留适配器直通相机的实际方案。
- 增加可重复的输入与生命周期回归验收；保留首次运行故障证据。
- 按用户 2026-09-18 的补充指示，将已定位的 Audio 场景残留清理纳入本 change：优先通过 Editor 移除误保存的运行时退订组件；对已证实不可由普通 API 移除的内嵌引用，按 design 的严格校验例外清理，再由 Editor 重载并复验启动、退出与音频。

### Non-Goals

不调整枪口、AimRig、骨骼轴、相机构图或动画手感；不实现真实射击、敌人、Core 会话或 UI；不新增手柄 Aim/Fire 绑定、重映射、完整暂停菜单或跨场景架构；不升级依赖，不改供应商源文件。已确认纳入的资产修复仅为 SampleScene/Audio 的这一处运行时组件残留；其他无关资产/包问题仍另建变更，不批量删组件。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `player-input-model`：修正 Player Map / Aim / Fire 持久化契约，明确校验失败、生命周期与输入释放行为、适配器顺序、跳跃消费和 Look 单一写入者。
- `player-system`：明确 SetAimStateCommand 先写输入 Model，再由 PlayerSystem 同步瞄准状态与事件，以及订阅清理和重复值边界。
- `audio-system`：清除 SampleScene 中误保存的运行时退订辅助组件，确保场景重载和多次运行不产生对应 Missing Script，保持音频播放能力。

## Impact

- 主要实施位置：`Assets/_Project/Settings/UnomataPlayer.inputactions`；`Scripts/Gameplay/Player/PlayerController.cs`、`SAInputAdapter.cs`、`PlayerSystem.cs`；`Scripts/Gameplay/Commands/SetAimStateCommand.cs`（均位于 Assets/_Project 下）。
- 通过 Editor/MCP 清理并保存 SampleScene/Audio 的已定位残留；其他场景改动仍限必要的输入接线修复，保留已有 GUID、有效组件和未提交修改。
- 验证入口沿用 Unity MCP 的定向输入注入/运行快照和可重复的验收步骤；当前没有项目自有测试 asmdef，不为本修复重组全部运行时程序集。
- 同步 README、Docs/ARCHITECTURE、ENVIRONMENT_RECOVERY、TODO 与新增输入专项验收说明；主规格在实现验收后再同步。旧归档保持原样，`aim-ik-rig-constraint` 保持独立。
- 风险：PlayerInput 克隆动作实例、初始化顺序、PassThrough Sprint 松开语义、TPC 对 Jump 缓冲的消费、失焦恢复、Architecture 释放时订阅清理。验收要覆盖真实链路，直接改 Model 不能代替动作回调验证。
