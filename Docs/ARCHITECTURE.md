# 架构与实现边界

> 更新：2026-09-19。下面先描述当前代码，再描述目标。瞄准射击与胶囊受击已接入场景，自动验证与用户视听、真实切窗验收均已通过，正式规格已同步归档，见 [射击基线](SHOOTING_BASELINE.md)。

## 当前部署

| 部分 | 路径 | 实际内容 |
|---|---|---|
| 独立 Core | `CardChainCore/src/Unomata.Core/` | net8.0 纯 C#；类型、规则、状态更新、选项生成 |
| Unity Gameplay | `Assets/_Project/Scripts/Gameplay/` | GameApp、Player、Commands、Events、Audio、Shooting、Enemy 与 Wave 骨架 |
| Unity Core / UI / Linking | `Assets/_Project/Scripts/` 对应目录 | 骇入相关目录尚无实现；独立 Core 尚未接入 Unity；射击准星和敌人头顶状态 UI 由 Gameplay 表现提供 |
| 原型场景 | `Assets/_Project/Scenes/SampleScene.unity` | 当前角色、机甲射击与状态 UI 场景；射击与机甲表现均已验收归档 |
| 隔离资产验证 | `Assets/_Project/Scenes/Sandbox/` | Audio、BT、MechPack、SciFiArena、SciFiEffects、Shooting |

环境版本见 [DEPENDENCIES.md](DEPENDENCIES.md)；项目没有来自 VG2 的 GameArchitecture 或 ShutdownIfInitialized API，不可直接复制其业务生命周期约定。

## 当前 QFramework 组合根

`GameApp : Architecture<GameApp>`，按 Model → Utility → System 注册：

| 层 | 已注册 / 已存在 | 实现边界 |
|---|---|---|
| Model | PlayerModel、PlayerInputModel、WaveModel、AudioModel、AimModel、EnemyModel、ShootingModel | 玩家/输入/波次占位/音频/瞄准、目标 HP/R/H/身份、射击冷却与序号 |
| Utility | IAimWorldQuery / UnityAimWorldQuery、IShotWorldQuery / UnityShotWorldQuery | 相机/枪口目标查询与实际射击命中、内部起点检查；自身/Trigger 过滤及密集查询兜底 |
| System | PlayerSystem、WaveSystem、AudioSystem、IAimSystem / AimSystem、EnemySystem、ShootingSystem | 输入镜像、波次骨架、音频事件、瞄准、受击/死亡、门控与射速；Enemy/Audio 先于 Shooting 注册 |
| Command | SetMove/Jump/Sprint/FireInput、SetAimState、PlayFootstep、PlayLand | 输入与音频入口；ResetPlayerInputCommand 统一清理业务输入 |
| Command 骨架 | StartHack、SelectCard、Heal、DamagePlayer | OnExecute 仍为空，不算功能完成 |
| 表现/适配 | PlayerController、SAInputAdapter、PlayerMotor、PlayerAimPresentation、AudioBridge、ShootingController、EnemyController、EnemyPresentationView、EnemyStatusView、ShootingFeedbackView、CombatAudioView | 输入/运动/姿态、射击驱动、碰撞注册、效果及独立战斗声音；表现不持有 HP 或重复伤害 |

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
  → ShootingController (600) → TickShootingCommand → ShootingSystem
      → IShotWorldQuery → EnemySystem（HP/减免/增伤/死亡）
      → ShotFiredEvent / EnemyDamagedEvent / EnemyDiedEvent → VFX / 简单提示
      → AudioSystem.Play → SoundPlayedEvent → CombatAudioView（24 声部）
  → AudioBridge (750) → Command / AudioSystem / Event → 原两个 AudioSource
  → MagicaCloth AfterLateUpdate
```

移动/瞄准修复已于 2026-09-18 完成验收；射击完整矩阵及用户视听、真实切窗验收已于 2026-09-19 完成。旧 StrafeController / AnimatorAimBridge / CameraAimBridge / AimTargetDriver 不再挂载到交付角色；原脚本/控制器保留历史参考，不能与新方案共同启用。

PlayerInputModel 拥有业务输入；PlayerModel.IsAiming 仍是 PlayerSystem 同步的镜像。Look 仅为相机表现输入。PlayerMotor 是唯一胶囊位移/根朝向消费者；来自 StarterAssets 的项目适配保留 2/5.335m/s、1.2 跳高与 -15 重力，供应商源文件不变。

AimModel/System 只保存值数据和 Utility 接口，不持有场景组件。PlayerAimPresentation 只负责场景采样与表现调度；基础姿态与双臂使用同一个手动图，两遍求值但每帧只推进一次时间，禁用 RigBuilder 自动图。非瞄准枪随当帧基础右手，瞄准枪独立解算，再由双手跟随。正常移动不重开瞄准过渡。

2026-09-18 输入修复已补齐持久化 Aim/Fire，加入绑定校验与生命周期清理，并将 SetAimStateCommand 改为先写 PlayerInputModel；自动回归 150 个断言通过。原故障证据见 [恢复记录](ENVIRONMENT_RECOVERY.md)，当前实现与验收边界见 [INPUT_BASELINE.md](INPUT_BASELINE.md)。

输入自动回归已覆盖重新启用、错误配置、释放及模拟失焦；恢复阶段已获用户整体确认，瞄准修复由 `fix-over-shoulder-aim-locomotion` 完成，并于 2026-09-18 获用户单独验收，正式规格已同步。Look 为适配器直通，主 spec 已同步当前输入与生命周期契约。Jump 的 Model 表示按钮状态，下游 SA 缓冲只接收新按下请求，不能反向改写 Model。

## 射击与目标所有权

ShootingSystem 同时核对 AimModel 的 Solution 与 Snapshot：前者必须是当前帧 Ready，后者允许 Ready 或稳定的 MuzzleObstructed；过渡被遮挡状态覆盖时不会误放行。实际射线从最终枪口出发，命中最近环境/敌人，不以相机目标替代伤害目标。ShotId 由独立射击上下文和单调序号组成；上下文释放后旧请求无效。

EnemyModel 注册表只保存值状态、碰撞体整数 ID 和去重水位；EnemyController 启用注册，禁用/销毁注销，并在死亡或外部注销时关闭碰撞。EnemySystem 提交注册/注销后发布类型事实，EnemyViewBinding 为每个 View 管理订阅、只读快照及身份隔离。EnemyPresentationView 独占 Renderer/Animator，机甲死亡动作结束后隐藏，胶囊使用立即隐藏配置。EnemyStatusView 在最终相机之后显示血量及减伤/易伤并处理遮挡，不拥有权威 HP；显示数值复用 DamageCalculation。系数 H 的设置经过 Command/System，资产只提供初值。当前实现/验收边界见 [敌人表现记录](ENEMY_PRESENTATION.md)。

轻微后坐由 PlayerAimPresentation 接收射击事实，在下一帧最终对齐之前改变枢轴位移，继续由同一图和双臂约束求值。战斗效果与声部拥有独立世界空间运行根对象，命中点不会随角色移动。详细接口见 INTERFACE，运行与素材证据见 SHOOTING_BASELINE。

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
