# 架构与实现边界

> 更新：2026-09-17。下面先描述当前代码，再描述目标；尚未实现的模块不会作为现有依赖使用。

## 当前部署

| 部分 | 路径 | 实际内容 |
|---|---|---|
| 独立 Core | `CardChainCore/src/Unomata.Core/` | net8.0 纯 C#；类型、规则、状态更新、选项生成 |
| Unity Gameplay | `Assets/_Project/Scripts/Gameplay/` | GameApp、Player、Commands、Events、Audio、Wave 骨架 |
| Unity Core / UI / Linking | `Assets/_Project/Scripts/` 对应目录 | 尚无实现代码；独立 Core 尚未接入 Unity |
| 原型场景 | `Assets/_Project/Scenes/SampleScene.unity` | 当前角色与瞄准试验场景 |
| 隔离资产验证 | `Assets/_Project/Scenes/Sandbox/` | Audio、BT、MechPack、SciFiArena、SciFiEffects |

环境版本见 [DEPENDENCIES.md](DEPENDENCIES.md)；项目没有来自 VG2 的 GameArchitecture 或 ShutdownIfInitialized API，不可直接复制其业务生命周期约定。

## 当前 QFramework 组合根

`GameApp : Architecture<GameApp>`，先注册所有 Model，再注册 System：

| 层 | 已注册 / 已存在 | 实现边界 |
|---|---|---|
| Model | PlayerModel、PlayerInputModel、WaveModel、AudioModel | 玩家 HP/瞄准镜像、输入、波次占位、音频数据 |
| System | PlayerSystem、WaveSystem、AudioSystem | 玩家扣血与瞄准广播、空波次方法、音频事件 |
| Command | SetMove/Jump/Sprint/FireInput、SetAimState、PlayFootstep、PlayLand | 输入与音频入口；ResetPlayerInputCommand 统一清理业务输入 |
| Command 骨架 | StartHack、SelectCard、Heal、DamagePlayer | OnExecute 仍为空，不算功能完成 |
| 表现/适配 | PlayerController、SAInputAdapter、StrafeController、AnimatorAimBridge、CameraAimBridge、AimTargetDriver、AudioBridge | 输入、第三方运动适配、朝向、动画/相机/音频 |

当前没有正式的跨场景架构启动/关闭和完整重开流程。新增生命周期功能时须先设计，再同步契约。

## 当前数据流和所有权

```text
PlayerInput actions
  → PlayerController (-20，校验/生命周期管理)
  → Set*Command
  → PlayerInputModel
      ├─ IsAiming → PlayerSystem → PlayerModel.IsAiming + AimStateChangedEvent
      │                            → 动画 / Rig 权重、相机、Strafe
      └─ Move / Jump / Sprint → SAInputAdapter.Update (-10)
                                → StarterAssetsInputs → ThirdPersonController

Look action → SAInputAdapter → StarterAssetsInputs.look（实际的相机直通路径）
Main Camera → AimTargetDriver.LateUpdate (-5) → AimTarget → AimRig（求值时序待运行核对）
Animator 相位 / 落地状态 → AudioBridge → 音频 Command / System / Event → AudioSource
```

PlayerInputModel 拥有业务输入状态；PlayerModel.IsAiming 是当前实现保留的镜像，由 PlayerSystem 同步。Look 未进 Model。Strafe 在 LateUpdate 控制身体 yaw，IK 控制脊椎朝向，AnimatorAimBridge 同时渐变动画层与 Rig 权重。

2026-09-18 输入修复已补齐持久化 Aim/Fire，加入绑定校验与生命周期清理，并将 SetAimStateCommand 改为先写 PlayerInputModel；自动回归 150 个断言通过。原故障证据见 [恢复记录](ENVIRONMENT_RECOVERY.md)，当前实现与验收边界见 [INPUT_BASELINE.md](INPUT_BASELINE.md)。

输入自动回归已覆盖重新启用、错误配置、释放及模拟失焦；恢复阶段已获用户整体确认，瞄准姿态偏差留给后续新 change。Look 为适配器直通，主 spec 已同步当前输入与生命周期契约。Jump 的 Model 表示按钮状态，下游 SA 缓冲只接收新按下请求，不能反向改写 Model。

## Core 的现状与目标

已存在的公开类型为 CardData、CardType、CardColor、ChainDirection、EndReason、ComboType、HackDifficultyConfig。SessionState、CardChainRules、OptionGenerator 是 internal，通过 InternalsVisibleTo 提供测试访问。

尚未实现：HackSession、HackResult、完整计时、奖励池与结束路径。ComboType 只有预留枚举，没有 Combo 检测。不存在 CardData.CanFollow；匹配规则依赖会话状态，由 CardChainRules 判定。Console 还不能演示完整骇入。

[INTERFACE.md](INTERFACE.md) 保留目标会话契约；它不是现有 API 清单的替代品。

## 目标结构（尚未全部实施）

```text
Unity Controller → Command / Query → QFramework System / Model
                                      ├─ HackSystem → Core.HackSession
                                      ├─ Enemy / Wave / Player / SyncRate
                                      └─ Event / BindableProperty → View / UI / Audio

Core.HackSession 原生事件 → Linking → Unity 业务命令与表现事件
```

- HackSystem 拥有会话创建、Tick、订阅、结束与释放；Controller 不再独立持有第二套权威会话。
- Core 拥有卡牌规则、发牌、计时、奖励池与结果，禁止 Unity 依赖。
- System/Model 拥有敌人 HP、减免、波次和同步率；场景对象与显示由 Controller / View 管理。
- Linking 负责不同领域之间的解释和转换，不能把“骇入削减系数”直接当作“敌人剩余减免率”。
- Phase 4 前验证 Unity 兼容性与单一源码维护方案。旧计划中的复制迁入未执行，不保持两份人工同步源码。

## 已知架构债与下一步

瞄准/输入、factor/溢出契约、Build Settings 旧路径和 Core 迁入风险统一见 [PROJECT_REVIEW.md](PROJECT_REVIEW.md)；近期次序见 [TODO.md](TODO.md)。功能变更先补齐设计与可验证场景，不通过修改现状说明来掩盖问题。

## Audio 场景清理补充（2026-09-18）

SampleScene/Audio 已移除误保存的 QFramework.UnRegisterOnDestroyTrigger 及其孤立内嵌 MonoScript。AudioBridge 和两份 AudioSource 不变，运行时仍由既有订阅链创建正常退订辅助组件，退出后不保存回场景。两次运行与音频复验通过，详见 [输入基线记录](INPUT_BASELINE.md)；没有修改框架/供应商源码。
