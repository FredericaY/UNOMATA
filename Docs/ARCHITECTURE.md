# 架构与实现边界

> 更新：2026-09-18。下面先描述当前代码，再描述目标；尚未实现的模块不会作为现有依赖使用。

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

`GameApp : Architecture<GameApp>`，按 Model → Utility → System 注册：

| 层 | 已注册 / 已存在 | 实现边界 |
|---|---|---|
| Model | PlayerModel、PlayerInputModel、WaveModel、AudioModel、AimModel | 玩家 HP/瞄准镜像、输入、波次占位、音频数据 |
| Utility | IAimWorldQuery / UnityAimWorldQuery | 相机/枪口射线与内部起点检查；过滤自身与触发器 |
| System | PlayerSystem、WaveSystem、AudioSystem、IAimSystem / AimSystem | 输入镜像、波次骨架、音频事实事件、瞄准几何与状态 |
| Command | SetMove/Jump/Sprint/FireInput、SetAimState、PlayFootstep、PlayLand | 输入与音频入口；ResetPlayerInputCommand 统一清理业务输入 |
| Command 骨架 | StartHack、SelectCard、Heal、DamagePlayer | OnExecute 仍为空，不算功能完成 |
| 表现/适配 | PlayerController、SAInputAdapter、PlayerMotor、PlayerAimPresentation、AudioBridge | 输入、运动/朝向、最终镜头/动画/武器/双臂、相位音频 |

当前没有正式的跨场景架构启动/关闭和完整重开流程。新增生命周期功能时须先设计，再同步契约。

## 当前数据流和所有权

```text
PlayerInput actions → PlayerController (-20) → Command → PlayerInputModel
  Move / Jump / Sprint → SAInputAdapter (-10) → StarterAssetsInputs → PlayerMotor (-5)
  IsAiming → PlayerSystem → PlayerModel 镜像 + AimStateChangedEvent
Look action → SAInputAdapter → StarterAssetsInputs.look → PlayerMotor 轨道意图

PlayerMotor 位移/身体朝向 → PlayerAimPresentation.LateUpdate (500)
  → Cinemachine ManualUpdate（最终混合/碰撞镜头）
  → 手动图推进 dt（基础动画 + 躯干，双臂权重为 0）
  → 采样当帧手/肩 → PrepareAimFrameCommand → AimSystem → AimPoseQuery
  → 独立武器/握把目标 → 同图零时间求值双臂约束
  → CompleteAimFrameCommand（实际枪口、手部可达性）→ AimModel 最终快照
  → AudioBridge (750) → Command / AudioSystem / Event → 两个 AudioSource
  → MagicaCloth AfterLateUpdate
```

本轮实现已接入 SampleScene，完整矩阵与用户视觉验收仍在进行，不能将接线完成等同 change 已完成。旧 StrafeController / AnimatorAimBridge / CameraAimBridge / AimTargetDriver 不再挂载到交付角色；原脚本/控制器保留历史参考，不能与新方案共同启用。

PlayerInputModel 拥有业务输入；PlayerModel.IsAiming 仍是 PlayerSystem 同步的镜像。Look 仅为相机表现输入。PlayerMotor 是唯一胶囊位移/根朝向消费者；来自 StarterAssets 的项目适配保留 2/5.335m/s、1.2 跳高与 -15 重力，供应商源文件不变。

AimModel/System 只保存值数据和 Utility 接口，不持有场景组件。PlayerAimPresentation 只负责场景采样与表现调度；基础姿态与双臂使用同一个手动图，两遍求值但每帧只推进一次时间，禁用 RigBuilder 自动图。非瞄准枪随当帧基础右手，瞄准枪独立解算，再由双手跟随。正常移动不重开瞄准过渡。

2026-09-18 输入修复已补齐持久化 Aim/Fire，加入绑定校验与生命周期清理，并将 SetAimStateCommand 改为先写 PlayerInputModel；自动回归 150 个断言通过。原故障证据见 [恢复记录](ENVIRONMENT_RECOVERY.md)，当前实现与验收边界见 [INPUT_BASELINE.md](INPUT_BASELINE.md)。

输入自动回归已覆盖重新启用、错误配置、释放及模拟失焦；恢复阶段已获用户整体确认，瞄准修复由 `fix-over-shoulder-aim-locomotion` 完成，并于 2026-09-18 获用户单独验收，正式规格已同步。Look 为适配器直通，主 spec 已同步当前输入与生命周期契约。Jump 的 Model 表示按钮状态，下游 SA 缓冲只接收新按下请求，不能反向改写 Model。

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

瞄准/输入与 Build Settings 的恢复历史、仍待处理的 factor/溢出契约及 Core 迁入风险统一见 [PROJECT_REVIEW.md](PROJECT_REVIEW.md)；近期次序见 [TODO.md](TODO.md)。功能变更先补齐设计与可验证场景，不通过修改现状说明来掩盖问题。

## Audio 场景清理补充（2026-09-18）

SampleScene/Audio 已移除误保存的 QFramework.UnRegisterOnDestroyTrigger 及其孤立内嵌 MonoScript。恢复阶段保留 AudioBridge 和两份 AudioSource；当前瞄准修复版改为显式成对订阅/退订，不再依赖该运行辅助组件，两个播放端及原音频保留。两次运行与音频复验通过，详见 [输入基线记录](INPUT_BASELINE.md)；没有修改框架/供应商源码。


## 举枪奔跑方向限制（用户复验修订）

PlayerSystem 以权威 IsAiming / Move 计算当前方向是否允许奔跑，`SprintDirectionQuery` 只读返回；PlayerMotor 结合原始 Sprint 缓冲执行。举枪仅纯前向可跑，A/S/D 与斜向（包括 W+A、W+D）同帧封顶 2m/s；纯前向与非瞄准仍可达 5.335m/s。零输入保留正常制动。原始 Sprint 不被清除，持住 Shift 回到纯 W 或退出瞄准即可恢复奔跑。

表现层在方向受限时清零 Gait，方向本身继续平滑混合；AimLocomotion 仅正前方接 Run，其他方向只接 AimWalk。旧 Run_45…315 已断开，包括旧子树和脚步相位引用。它们作为未采用实验资产保留，配置入口不会重新生成或挂回。

跳跃表现补充：PlayerMotor 只读公开 JumpTakeoffSpeed / VerticalVelocity / JumpTriggered；PlayerAimPresentation 同帧显式 CrossFade 到起跳，并以竖直速度映射 AirPhase，JumpStart/InAir 共用连续 AirR 姿态。Animator Jump 参数已成对移除，改为 AirPhase/VerticalVelocity；物理和落地音的所有权不变。
