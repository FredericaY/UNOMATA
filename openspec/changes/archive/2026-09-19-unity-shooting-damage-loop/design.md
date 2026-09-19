## Context

动机与交付范围见 [proposal.md](proposal.md)。本设计是待实施方案，不代表运行验证已经完成。

本轮只读核对得到的接入事实：

- `PlayerController` 已通过 Command 更新 `PlayerInputModel.IsAiming/Fire`，Fire 尚无射击消费者；输入恢复逻辑已有失焦与重新按键保护。
- `PlayerAimPresentation` 在 LateUpdate(500) 完成最终 Cinemachine 镜头、手动动画图和双臂求值，再提交枪口快照；动画图每帧只推进一次时间。`AimModel` 同时保存 `Solution` 与 `Snapshot`。
- `AimSystem.Complete` 可以把过渡中的快照改标为 Blocked；因此仅检查 Snapshot.Status 不能证明举枪完成。`MuzzleObstructed` 快照仍包含实际枪口，内部起点/不可达失败则不能作为可射击依据。
- `IAimWorldQuery` 只返回命中点，不能提供射击需要的法线、碰撞体和目标身份。当前射击/敌人领域不存在，WaveSystem 与骇入 Command 为占位。
- `AudioSystem.Play` 已发布 `SoundPlayedEvent`，`AudioBridge.OnSoundPlayed` 仍为空。原两个 AudioSource 负责脚步/落地，脚步存在短音筛选和 Stop+Play 策略，不能直接用于枪声。
- 候选素材实际存在：SciFiEffects 的 `Effects/Vulcan/vulcan_muzzle.prefab`、`vulcan_projectile.prefab`、`vulcan_impact.prefab`；音效包的 `AUDIO/Shoot/Rifle_Shotgun_Pistol/` 中有 Rifle 与 HiTech Rifle 多个变体；CombatGirls 有 `R_Shoot.fbx`、`R_AimIdle_AutoShoot.fbx`。静态内容显示 Vulcan 命中特效包含粒子，枪口/弹道还包含网格/灯光，不能把整组笼统当成纯粒子自动播放。本轮未试听或运行这些候选。

## Goals / Non-Goals

**Goals:**
- 分离射击意图、物理查询、伤害规则和素材播放，使下次替换敌人外观时复用业务层。
- 保持现有瞄准、输入、运动、脚步/落地与布料契约，并为视听反馈增加可测量的单次射击身份。
- 把消费给定 H 的伤害计算与未来 Core 产生 H 的算法分离。

**Non-Goals:**
- 不引入第二套角色控制器、业务单例、供应商伤害规则或独立自动求值的动画图。
- 不实施 Core 迁入、完整跨场景游戏重开、敌人行为树或波次；仅清理本模块的注册/上下文。
- 不将素材试选及可调数值假定为已经通过用户观感验收。

## Decisions

### D1. 状态与依赖分层

下表名称为建议新增类型，不是现有 API 承诺；实现如需改名保持职责和规格不变。

| 层 | 建议组件 | 所有权/职责 |
|---|---|---|
| Model | ShootingModel | 当前射击上下文、冷却/下一发时间、上一处理帧、递增 ShotId、配置值快照 |
| Model | EnemyModel | 目标注册表、碰撞体整数 ID 到目标身份映射、HP/R/H/存活状态与去重水位 |
| System | ShootingSystem | 输入门控、当帧姿态/安全状态验证、射速、调用物理 Utility、分发一次命中及射击事实 |
| System | EnemySystem | 注册/注销、参数验证、设置 H、纯数值结算、HP 与一次性死亡 |
| Utility | IShotWorldQuery / UnityShotWorldQuery | 射线与内部起点检查；返回值结构 ShotHit，含距离/点/法线/碰撞体整数 ID；不扣血 |
| Controller | ShootingController | 持有显式角色表现引用，在最终姿态后发送本帧 TickCommand；将 Unity 生命周期转为停止/释放 Command |
| Controller | EnemyController | 配置和 Collider 注册入口、订阅本目标死亡以禁用碰撞/隐藏胶囊；不保存权威 HP |
| View | ShootingFeedbackView / CombatAudioView | 消费事实，管理特效/声音池、命中提示；不得重新发射或扣血 |
| Editor/诊断 | ShootingValidation / ShootingDiagnostics | 显式启动测试、读数、H 设置和目标重置；运行时代码不得引用验证程序集 |

GameApp 按 Model → Utility → System 注册，EnemySystem/AudioSystem 先于依赖它们的 ShootingSystem。System 仅持有 Model、其他 System 或 Utility 接口，不存场景对象。Command 只委托 System，无返回值；查询返回只读快照。

备选“EnemyController 公有 HP + IDamageable 中结算”会把外观和业务绑定，也沿用旧计划的状态所有权问题，因此不采用。碰撞适配可以有受击标记，但必须转换为稳定身份后交给 EnemySystem。

### D2. 开火门控和节奏

采用当前输入状态的组合：`IsAiming && Fire`，并要求本帧有效稳定姿态。过去已释放的点击不排队；若左键仍保持按下，条件变为有效后按冷却开始射击。无腰射、射击不自动进入瞄准。

建议初始可调参数：8 发/秒、每发基础伤害 20、射程 200m、无限弹药。参数是试射起点而非永久平衡值；正式数值以配置和运行记录为准。本次瞄准射击不加随机散布与持续镜头上抬。

冷却属于业务状态，松键或反复切换瞄准不能令尚未到期的冷却归零。正常连发保留亚帧余量以避免每发按整帧舍入导致射速降低；同帧最多一发，对长帧/暂停/失效期间的多发欠账丢弃。交付配置必须低于最低验收帧率可表达的射速（初始 8 < 30），不为高于帧率的配置声称同样射速精度。同一上下文/帧重复 Tick 不再次更新或发射，负/非有限 dt 拒绝。

备选输入回调直接射击会在最终武器姿态之前执行；独立协程连发容易跨失焦/停用残留，均不采用。

### D3. 射击时序与 Blocked 的细分

```text
Input / Motor
  --> PlayerAimPresentation LateUpdate(500)
      --> camera + animation(dt) + weapon/hands(0)
      --> CompleteAimFrameCommand
  --> ShootingController LateUpdate(600)
      --> TickShootingCommand(context, frame, dt)
      --> ShootingSystem --> IShotWorldQuery --> EnemySystem
      --> ShotFired / EnemyDamaged / EnemyDied facts
  --> VFX / CombatAudio views
  --> AudioBridge LateUpdate(750) locomotion audio
```

ShootingController 只读取显式绑定表现的当前快照身份/帧并发送驱动命令；ShootingSystem 再从 AimModel 核对 ActiveContext、Solution 与 Snapshot 的同帧一致性，不能信任调用者单独传入 Ready 布尔值。

放行必须同时满足：Solution 有效且 Status=Ready；Snapshot=Ready，或 Snapshot=Blocked 且 Failure=MuzzleObstructed；输入双键有效、上下文活跃、角色表现未停用/失焦且配置合法。CameraInsideObstacle、MuzzleInsideObstacle、TargetUnreachable、InvalidPose、Transition、Inactive、旧帧一律拒绝。这样过渡被 Blocked 覆盖也不会误放行。

在真实枪口再执行一次射击物理查询，不能把瞄准期望目标当伤害目标。环境位于枪口路径上时保留开火并在近墙落点反馈；内部起点等不安全情况连枪声/闪光也不产生。采用实际枪管方向，未命中终点为起点 + 方向 × 射程。

备选“只允许 Ready”会把正常射墙误做哑火；“所有 Blocked 均允许”会穿透内部起点。两者均不符合已确认行为。

### D4. 命中查询与目标解析

新建 IShotWorldQuery 而不挤入只返回点的 IAimWorldQuery。Unity 实现返回纯值 ShotHit：是否命中、Point、Normal、Distance、ColliderId；System 用注册映射解析 EnemyId。若最近实体没有活敌人注册，按普通表面处理，不能跳过它继续寻找后面的敌人。

配置碰撞掩码覆盖环境和目标层；显式忽略玩家 Collider、Trigger 与诊断对象。注册/生成时验证目标层进入掩码，禁止只配置 Enemy 导致漏墙。多命中查询按距离选最近；NonAlloc 缓冲填满时采用完整查询或扩容重查，不能静默丢候选。测试显式同步新建/移动碰撞体，生产中如需同步只放在必要变更点，不擅改全局物理设置。

一发只处理最近一个碰撞体，多个碰撞体归同一目标也最多扣血一次。不采用供应商弹丸脚本控制飞行/碰撞/伤害：本次既定行为为 hitscan，弹道素材只表现终点。

### D5. 胶囊注册、伤害和死亡

EnemyController 启用时生成注册身份（上下文 + 新 Guid），携初始 HP/R/H 和 Collider 整数 ID 发 RegisterEnemyCommand；注册是否成功通过只读查询确认。禁用/销毁发注销 Command。所有请求带身份，重置/重启使用新身份，避免旧事件命中新实例；清理只使用已缓存且仍有效的架构引用。

EnemySystem 验证最大 HP>0、R∈[0,1]、H≥0，均须有限；D≥0 且有限。非法初值拒绝注册，非法 H 更新保留旧值。公式中间计算使用足够精度并检查范围，非有限结果拒绝，不悄悄把非法数据改为治疗或任意上限。

```text
remainingReduction = R * (1 - clamp(H, 0, 1))
bonus               = max(H - 1, 0)
resolvedDamage      = D * (1 - remainingReduction) * (1 + bonus)
appliedDamage       = min(CurrentHp, resolvedDamage)
```

受击事实带 EnemyId、ShotId、D/R/H、resolvedDamage、appliedDamage、前后 HP。先提交完整 HP/死亡状态，再广播受伤、必要时一次死亡，避免事件回调重入再次致死。每个射击上下文生成单调 ShotId；同目标以该上下文的已消费序号水位去重，释放上下文时清理水位，不维护无限增长的历史集合。

死亡后同步失去可受击资格，视图立即关闭 Collider，再播放独立短死亡效果/隐藏外观；逻辑死亡不等待动画播完。不会在本次自动调用空 WaveSystem 或制造波次推进。后续敌人模型/UI只需订阅相同事实。

### D6. 系数设置与未来骇入边界

新增 SetEnemyHackFactorCommand(enemyId, H) 委托 EnemySystem 覆盖单目标 H，不累计。当前 H 来自初始配置或显式调试入口；本次不实现自动过期计时、Core 事件、敌人被骇入中的状态机。

消费给定 H 的公式已由用户确认，不能再列成未决项。A5/A6 中“溢出是否继续增加 H”仍待决定；两者是不同问题。未来 Linking 通过此命令设置系数，无需把 H 错写为剩余减免率，也不在 Unity 重复接龙规则。

### D7. 素材接入与唯一反馈事实

ShootingSystem 在门控与物理查询成功后完成一次同步伤害处理，再产生一次 ShotFiredEvent，载明上下文/ShotId、实际枪口、方向、终点、法线及命中分类。同时由 ShootingSystem 对该发分别调用 AudioSystem.Play 请求一次枪声及必要的命中音。效果消费射击事实，声音播放端消费音频请求，死亡消费 EnemyDiedEvent；任何 View 都不能再调用受击业务或重复请求同一枪声。

优先评估 Vulcan 成套枪口/弹道/命中与 Rifle 或 HiTech Rifle 枪声；需要对候选做当前 URP 下的项目副本运行检查。静态筛选不能当作可用性验收。枪口网格/灯光类效果由项目播放包装明确启停，粒子按配置发射；不假设所有预制体挂上就自行正确播放。必要材质替换仅在项目副本进行，保留材质、贴图和原始来源记录。

候选组合试听/试射后保存为一个 RifleFeedbackProfile，记录最终选择、尺度、寿命、色彩、空间衰减和音量。命中音需从已有包中试听适当片段，包目录名不证明其适合命中用途；如需剪裁/调参，生成项目派生素材并记录来源。供应商自驱动发射器、碰撞伤害、声音和挂点回调不随效果一起接管游戏逻辑。

枪口附着实际 Muzzle；曳光使用该发捕获的起终点；命中特效固定世界点并使用表面法线。命中胶囊时零扣血仍有物理反馈；只有死亡事实触发死亡效果。简单准星基于瞄准状态显示，命中标记依据实际敌人命中事实；正式敌人血条属于下一 change。

备选只用占位闪烁无法满足本次验证成品素材效果的目标；直接使用整套 FORGE3D 演示控制器会引入另一套发射/音频生命周期，因此采用项目包装。

### D8. 轻微射击动作与已有瞄准精度

射击事实在最终姿态后产生：枪口/VFX/声音可以当帧出发；角色反馈脉冲从下一帧同一表现协调者求值，不在快照发布后私自改枪。评估 R_Shoot/R_AimIdle_AutoShoot 的上身片段，项目副本清理供应商事件，提取/混合与双臂约束兼容的动作。

默认反馈采用有限幅度的后坐位移和上身回弹，位置偏移在 Prepare 前进入武器枢轴求解，再由既有武器对齐及双臂链完成最终姿态。武器方向仍对准目标；不叠加随机射偏、持续镜头抬升或未校验的最终旋转。若原 clip 与精度冲突，采用基于该节奏的项目程序化回弹；必须交付可感知、自然的反馈，不能用完全禁用反馈来通过测试。

不新增第二个武器变换写入者，不重开 Transition 豁免每发精度；每帧仍一次 dt 推进和零时间双臂求值。验证枪管、握把、腿部运动、跳落和音频相位均无退化。此方案保留主 over-shoulder-aim 的既有要求，不需要放宽其精度规格。

### D9. 音频与效果池的生命周期

补齐战斗音效配置，由配置 Command/System 保存资产引用与音量数据，AudioSource 仍在 View。使用单独 CombatAudioView 消费 SoundPlayedEvent，保留 AudioBridge 的两份脚步/落地播放端；删去或明确保留原空 OnSoundPlayed 订阅为无处理，不能出现两个战斗播放者。

枪声/命中按已发事实各请求一次，经过 AudioSystem.Play；暂不改变现有 SoundPlayedEvent 结构，去重在射击事实转音效请求处完成。战斗播放不复用脚步 MaxFootstepLength 筛选或 Stop+Play 通道；总音量来自 AudioModel。使用有限声部池，优先复用空闲声部，超限替换最旧同类别声部，优先级与上限由交付配置记录；常规射速下需配置足够声部以避免明显截断。

特效池按 profile 给定每类容量与最大寿命，上限取决于射速×寿命并保留少量余量；超限回收最旧同类纯表现对象，不影响已完成伤害。停火不新生实例，短尾部可播完；失焦、停用/销毁、场景退出强制清理本上下文。所有订阅成对释放，不在清理时访问惰性 GameApp.Interface。

### D10. 场景、配置与验证入口

- 项目资产：新增 `Assets/_Project/Settings/Combat/` 的步枪/敌人/反馈配置，`Assets/_Project/Prefabs/Enemies/EnemyCapsule.prefab` 与项目 VFX/音效派生资源；运行状态不得写回配置。
- 正式接入：对 `Assets/_Project/Prefabs/Player/ShoulderAimPlayer.prefab` 和 `Assets/_Project/Scenes/SampleScene.unity` 增加必要组件/引用、少量胶囊和简单反馈。
- 独立验证：新增 `Assets/_Project/Scenes/Sandbox/Sandbox_Shooting.unity`，使用相同项目角色/胶囊配置，包含墙面、近墙和多个目标；不把测试自动扣血脚本保存到正式角色。
- 调试入口：用 Editor 窗口读取最近射击/伤害快照，显式命令设置指定 H、重置目标。设置 H 不需要 HackSession；重置先注销旧身份再注册，不能直接改 Model 字段。运行时程序集不引用 Editor/验证代码。
- GameApp 本次新增模块自行管理上下文释放，不借此重做全项目启动/关闭。

### D11. 验证策略与交付证据

1. 纯逻辑验证：公式表与 R/D/H 边界、非法/溢出参数、过量伤害、去重、一次死亡、身份失效、系数覆盖；以固定输入和独立预期值判断。
2. 确定性节奏验证：合法双键、释放、过渡、稳定受阻、非法姿态、重复 Tick、点击限速、长帧/暂停无补射。默认交付射速下，实际 30/60/120fps 各保持 10 秒，记录时长、发数和间隔。
3. 实际物理/角色验证：环境和敌人最近命中、自身/Trigger 过滤、密集查询、近中远/无命中、内部起点和解除遮挡；真实角色站立、八方向行走、纯向前跑、原地与移动跳落及连续转向开火。
4. 影响回归：复用并按新能力调整现有 InputBaselineValidation、AimRuntimeValidation、AimBoundaryValidation、AimInputSceneValidation、AimSavedSceneAudit；AimRuntimeValidation 增加默认关闭的 shooting 参数，开启时通过正式输入 Command 持续开火、记录每例射击帧一致性，并写入 .utmp/shooting/aim-* 独立报告，保留原有全部门槛与原报告路径；检查枪口≤既有稳定/动态门槛、双手≤1cm/2°，每帧只推进一次动画。旧“输入 Fire 不直接射击”的测试保留在禁用射击消费者的隔离用例，再追加完整集成用例。
5. 生命周期/稳定性：三次组件启停、真实失焦与恢复、目标注销、场景退出；60 秒持续射击后等最长尾部结束，统计实例/声部上限与空闲基线，检查 Console。
6. 保存并重载资产/场景，两次独立 Play/Stop，核对 GUID、meta、第三方/包差异、无诊断污染。视觉与听感必须由用户实际游玩/录像认可，包括单发/连发、墙面/胶囊、死亡与移动跳落，不以测试计数代替。

音频样片使用临时 ShootingAudioCapture 探针：Unity 不允许将 Editor 程序集中的 MonoBehaviour 挂载到运行对象，因此探针位于 Gameplay/Tests 并以 UNITY_EDITOR 条件编译，仅在编辑器录制时动态挂到 AudioListener，完成即销毁，不保存到场景/Prefab；生产逻辑不引用它，也不引入 Editor/测试程序集依赖。

报告写入项目忽略的临时验证目录（建议 `.utmp/shooting/`），共享文档保留可复核摘要、版本及验收结论。当前提案阶段没有执行以上测试。

## Risks / Trade-offs

- [射击动作破坏刚完成的瞄准] → 反馈纳入唯一协调者、先反馈后最终求解、完整动作矩阵复验，不降低旧误差门槛。
- [Blocked 隐藏了过渡或无效起点] → 同时核对 Solution 与 Snapshot，按原因分类，不把全部 Blocked 视为可开火。
- [素材框架和坏材质] → 对选中候选逐个验证、制作项目副本及播放包装；仅修复本次使用链路，所有必需反馈仍需交付。
- [事件回调重入/多 Collider 导致重复扣血] → 唯一 ShotId、身份校验、先提交状态再广播、一次死亡和重复请求测试。
- [无限连发导致堆叠和性能问题] → 声部/特效有界池、明确上限与替换策略，运行记录实例及时间；不声称未经测量的性能结论。
- [旧文档将 H 与减免混为一谈] → 新增明确名称 BaseDamageReduction / HackFactor / RemainingReduction，消费端公式已定；保留 Core 溢出争议为后续事项。

## Migration Plan

先实现值数据/规则及验证，再接真实物理和胶囊，随后接素材、动作、声音与正式场景；每步保持可独立验证。Unity 场景/Prefab/材质操作经 Editor/AssetDatabase；供应商源文件保持不变。

出现回归时在本次新增项目组件/引用范围撤回接入，保留已有瞄准角色基线；不回滚用户无关修改，不重写旧归档。本次只创建规划文件，正式主 specs 在实现、运行/用户验收和文档同步后才更新。

## Open Questions

- 最终 Rifle/HiTech Rifle 枪声与 Vulcan 等特效组合，以及音量、尺度、动作幅度，由本次已列明的试听/试射任务决定；不改变交付类别。
- 射速、基础伤害、目标 HP 的最终调试值可在默认起点上配置；伤害公式和输入门控不随调参改变。
- Core 溢出到 H 的规则、骇入效果持续时间和下一阶段敌人资产选型属于后续 change，不阻塞本次。
