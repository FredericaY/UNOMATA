## Context

动机与范围见 [proposal.md](proposal.md)。本文件为待实施设计，不代表下列行为已运行或验收。该变更涉及注册生命周期、动画、UI 和已有场景/验证入口，满足需要独立 design 的条件。

本轮已核对的接入事实：

- EnemyModel/System 已保存独立身份、HP/R/H、碰撞体映射和射击去重水位；`EnemySnapshotQuery` 可只读获取当前状态。
- EnemySystem 先提交状态，再发布 `EnemyDamagedEvent`，致死时随后发布 `EnemyDiedEvent`；`EnemyHackFactorChangedEvent` 已存在，注册/注销事实尚不存在。
- EnemyController 目前既管理碰撞注册又在死亡时立即隐藏 Renderer。`ShootingDiagnostics` 的重置只切换该 Controller 的 enabled，并不禁用整个 GameObject；新表现不能只依赖自身 OnEnable 重置。
- ShootingFeedbackView 已负责一次死亡特效和击杀提示。敌人表现若再次请求同一声音/特效，会造成重复。
- MechPack 三种模型均有待机、走跑、攻击、受击、死亡切片。本提案选用 `mech_defender`；其 `idle_01` 为循环，`damage_01` 和 `death_01` 为非循环，导入配置对应帧段为 0–100、310–320、345–445。这些是静态资产事实，实际导入后的长度、绑定和画面待定向运行验证。
- ShootingSavedSceneAudit 当前固定检查 SampleScene 的三个胶囊 Prefab；ShootingSceneSetup 也会在 SampleScene 重新放胶囊并复制到 Sandbox_Shooting。必须明确两类场景入口，不能简单删掉旧校验或让旧生成器覆盖机甲。
- 已有 UGUI/TMP 和 LiberationSans 字体资产，未找到项目中文字体。本版状态文字采用现有字体支持的英文短标签及数字；文档使用中文，不新增字体采购或本地机器字体依赖。

## Goals / Non-Goals

**Goals:**
- 将注册/碰撞适配、权威伤害状态、动画外观及状态 UI 分开，使更换外观不改变 HP 或射击行为。
- 生命周期按目标身份闭合，包含仅启停 Controller、仅启停 View 和死亡中重置。
- 用现有素材及小型项目资产完成可复验交付，保存胶囊作为独立伤害回归基线。

**Non-Goals:**
- 不为静止目标引入第二套运动控制器、AI、通用实体框架或全项目启动/重开流程。
- 不把受击动画升级为硬直业务，不通过动画事件结算伤害或决定逻辑死亡。
- 不将 H 的显示扩展为真实骇入状态机、可消耗护盾、计时或 Core 联动。

## Decisions

### D1. 状态所有权与依赖方向

下列新增类型名是实施建议，职责和对外行为以 specs 为准。

| 层 | 组件 | 职责与边界 |
|---|---|---|
| Model/System | 既有 EnemyModel / EnemySystem | 继续拥有身份、HP/R/H、伤害、死亡和注册表；新增注册/注销事实 |
| Command/Query | 既有注册/注销/H 命令及快照查询 | 写入仍委托 System，读取无副作用；必要时加只读显示投影，不新增第二份 HP |
| Controller | EnemyController | 校验业务初值与命中体、启用注册、致死关闭碰撞、禁用注销；不驱动 Animator 或 Renderer |
| View | EnemyPresentationView | 绑定一个 Controller 的只读 Id，订阅类型事件并查询快照；管理 Idle/Hit/Death/Hidden、动画和 Renderer |
| View | EnemyStatusView | 同样绑定目标身份；管理血条、文字、相机投影/朝向和可见性，不发送伤害命令 |
| 配置 | EnemyProfile + 表现/UI 配置 | 前者保存 HP/R/H 初值；后者保存动作/过渡、兜底时间、UI 尺寸和显示距离，运行不写回 |
| Editor 验证 | 新敌人 setup / validation / audit | 显式生成项目资产与执行验证，不进入默认运行链 |

`GameApp.Interface` 仍为唯一业务入口。View 的 Controller 引用只用来识别绑定对象和读取注册身份，业务通过 Command/Query/Event；View 之间不相互调用来拼接流程。所有订阅缓存架构和 Model 代际，在禁用/销毁时释放；清理不访问惰性入口。

备选在 EnemyController 内加入全部动画/UI 会继续混合碰撞和外观生命周期；备选每帧从场景全局寻找敌人会丢失显式绑定，均不采用。只读显示投影可复用 `DamageCalculation.TryCalculate`（D=0）取得剩余减伤和 bonus，不在 UI 复制伤害规则。

### D2. 注册事实与首次绑定

在 EnemySystem 注册状态及 ColliderOwners 全部提交后发布 `EnemyRegisteredEvent(EnemySnapshot)`；成功注销并移除映射后发布 `EnemyUnregisteredEvent(Guid)`。失败注册、重复注销不发布成功事实；旧伤害去重与死亡顺序保持。事件不包含 GameObject、Animator、Collider 或 View。

Controller 在发送注册命令之前设置候选 Id，使早启用的 View 能匹配注册事实；命令返回后查询确认实际注册。View 启用采用“先订阅，再查询”的顺序：Controller 先启用时补读现状，View 先启用时等待成功注册事实。处理事件时始终校验当前绑定 Id/缓存身份及架构代际；不能依赖事件订阅者的执行顺序。

Controller 注销前同步关闭自己的命中体；注销事实使仍启用的两个 View 清空显示。配置/碰撞校验失败时不进入可受击状态，给出具体诊断；初始化是否成功由查询结果决定。多个 Controller 不能争用同一个命中体。

备选只在 View.OnEnable 读一次快照不能覆盖诊断工具仅重启 Controller；备选每帧扫描全注册表不必要。新增两种生命周期事实是本次唯一业务事件扩展，不增加波次含义。

### D3. 项目资产与命中体

项目根 `MechDefender` 挂 EnemyController 和一个粗粒度躯干 CapsuleCollider（Enemy 层、非 Trigger）；可见模型和 Animator 位于 `Visual`，UI 位于独立 `StatusAnchor`，避免死亡骨架将血条带着翻倒。无 Rigidbody 运动、NavMeshAgent 或 BehaviorTree；Animator 不应用 Root Motion。

来源：
- 模型/骨架参考：`Assets/ThirdParty/Characters/Enemy/MechPack/Prefabs/Animated/mech_defender.prefab`。
- 动画：同包 `Animations/mech_defender@animations.FBX` 的 `idle_01`、`damage_01`、`death_01`。
- 材质：同包现有 mech_defender 材质；若本轮发现不兼容，仅在项目派生材质修复所用链路。

项目资产建议：
- `Assets/_Project/Prefabs/Enemies/MechDefender.prefab`。
- `Assets/_Project/Animations/Enemies/MechDefender/`：项目 controller 及必要 clip 副本。
- `Assets/_Project/Settings/Combat/Enemies/`：正常初值、表现及 UI 配置。
- `Assets/_Project/UI/Enemies/`：简单血条/标签的项目资源（需要独立 UI Prefab 时放此处）。

初始视觉高度约 2m，以玩家、地面与模型实际包围盒做最终标定；根放在地面，不能沿用胶囊中心 y=1 导致悬空。命中体贴合主要躯干，允许简单形状，不做贴骨骼精确命中或弱点倍率。原位待机/受击不能把可见躯干明显移出命中体；死亡时碰撞已关闭。检查真实渲染包围盒和动画绑定，不能仅凭静态 Prefab 尺寸验收。

用项目包装和 AnimatorController 引用原配骨架，不照搬玩家 Humanoid 重定向或双臂 IK。项目副本清理无关动画事件；供应商演示 controller 不接管逻辑。

### D4. 受击、死亡和兼容胶囊表现

项目动画只接 Idle、Hit、Death，单层、单个 Animator 所有者。正常启动进 Idle 循环；非致死且 AppliedDamage>0 的事实在非 Hit 状态进入 Hit。Hit 播放期间不重触发、不排队，扣血/命中音效继续；播放完后回 Idle，下一次有效命中可再进 Hit。零扣血不进 Hit。

致死优先于 Hit：收到带 Killed 的受击事实时不进入普通受击，死亡事实锁存一次 Death；Controller 同一同步调用链关闭所有碰撞，UI 同步隐藏。Death 无返回 Idle 的过渡，完整播放后关闭模型 Renderer。根对象与注册的死亡快照保留供诊断读取/重置，不自动 Destroy、自动重生或接波次。

正常隐藏由实际 Death 状态播放完成判定；独立的有限兜底计时覆盖动作缺失、状态机错误和离屏不更新。兜底初值取实际 clip 时长加过渡及余量，采用 Unity 游戏时间，不引入暂停菜单或跨场景计时语义。提案初始过渡为受击 0.05s、死亡 0.05s，死亡完成立即隐藏，不另加停尸阶段；运行标定只调观感参数，不改变一次死亡与即时失效规则。动作不存在或无法完成时仅诊断一次并执行兜底，不能用兜底常态替代正确动画。正常死亡离屏也需有限结束，不遗留永久显示对象。

独立启停表现不改变权威状态：重启 View 后存活目标回当前 Idle，死亡目标直接 Hidden，不回放历史动作。新注册身份清除旧 trigger/过渡、死亡锁存、计时和缓存，不允许旧身份的延迟操作隐藏新实例。

胶囊 Prefab 采用同一表现适配的“立即隐藏”配置（无需 Animator），保持旧胶囊行为；Controller 不保留第二条隐藏路径。迁移时删除或迁移旧 `_renderers` 序列化引用，并更新生成胶囊的 Editor 工具，避免两组件竞争同一 Renderer。

ShootingFeedbackView 继续是死亡粒子和击杀提示的唯一出口，CombatAudioView 保持既有声音出口。本次不另发死亡爆炸/命中音，不改玩家反馈池和枪口时序。

### D5. 状态 UI 内容与数值

使用原生 UGUI 世界空间 Canvas + TMP；简单水平 HP 条和一行防护标签，放在机甲头顶上方。UI 只是表示当前 HP 比例，不新增护盾条、伤害飘字、名字板、目标锁定或玩家 HUD。

初始标签使用现有 LiberationSans 支持的 `RESIST 95%`、`RESIST 47.5%`、`RESIST 0%`、`VULNERABLE +20%`、`VULNERABLE +60%`，分别对应规格中的中文语义。颜色仅辅助区分，不用颜色替代文字。普通配置的百分比最多一位小数，不出现浮点残留；极大但合法的 H 采用有限的科学计数显示，不显示 NaN/Infinity，不修改业务值来适应 UI。

血条填充取当前 HP/MaxHp，事件后最迟下一次渲染更新，不加入延迟扣血尾条；H 变化只更新防护标签。UI 本地缓存仅用于避免重复格式化，不成为状态源。只读投影使用现有数值计算，必要时新增小型查询和值结构；不会为显示把 H clamp 到 1。

全部 UI graphic 禁止 raycastTarget，不增物理 Collider，也不监听战斗输入。沿用现有字体，必要的项目派生字体资产必须记录源路径和原许可信息；不引入机器字体或默认假定中文字符齐全。中文本地化、完整科幻 UI 资产选型仍属于后续 UI 阶段。

实施核对补充：首次实际运行发现缺少 TMP_Settings，TMP 的 Awake 在等待配置时提前返回，导致已有字体不生成网格。作为本次 UI 接入的一部分，在项目 Settings/Resources 下建立唯一 TMP Settings 配置并引用已有 LiberationSans；不导入整个示例包，不修改供应商或升级依赖。验收增加配置重载与实际字形网格检查。

### D6. 镜头、遮挡和显示时序

相机、StatusAnchor、玩家需要忽略的 Collider 列表通过场景显式接线；Prefab 不保存其他场景实例。只旋转 Canvas 面向实际渲染相机，不修改敌人根或骨骼。UI 跟随/可见性在玩家最终镜头与射击驱动之后更新，避免读取过渡中的虚拟机位；初始安排 LateUpdate(700)，当前最终镜头为 500、射击为 600。

显示条件：当前身份注册且存活、相机/锚点合法、锚点位于相机前方且投影在视口内、距离不超过配置值（初值 40m）、相机到锚点路径无遮挡。离屏隐藏而非压到边缘，不要求先瞄准或先命中才能显示。锚点遮挡作为本版明确判据，不扩展为全身多点可见性检测。

遮挡查询只负责场景表现，忽略目标自身、显式绑定的玩家 Collider 和 Trigger；其余有效实体包括墙体及其他目标都可遮挡。查询使用有限缓存，饱和时完整查询或扩容重查，不能假定返回顺序或漏掉墙体。几何/深度处理与 UI 材质配合，墙后不能残留文字。相机缺失、距离参数非法或引用不齐时仅隐藏 UI 并诊断一次，敌人业务不停止。

备选屏幕空间血条需要场景级 Canvas/实例注册管理，本版三个静止目标无需该基础设施；原 IMGUI 调试窗口仍留给调试，不作为正式敌人 UI。

### D7. 场景接入与旧工具迁移

- **SampleScene**：只替换 ShootingRange 的三个胶囊为同种机甲，保持原先 x/z 对照位置、墙面、玩家和射击/音频链路；校准脚底 y。保留三组 HP=100、R=0.95、H=0/0.5/1.2 的显式验证配置，分别约 1/10.5/24 点每发伤害。普通可复用敌人 Prefab 默认 H=0；三个演示系数不解释为已发生骇入，也不赋予自动过期。
- **Sandbox_Shooting**：保留三个胶囊与旧射击/物理布置，迁移必要的胶囊表现组件后继续原回归。
- **Sandbox_EnemyPresentation**：新建独立场景，使用同一玩家、机甲、相机和方向光，包含并排与前后目标、墙面及可见性验证位置。测试入口显式启动，不保存自动扣血/改 H 的组件。
- **ShootingSceneSetup**：将胶囊初始化与新机甲 setup 分开；旧入口针对胶囊 Sandbox，或在已接入机甲的 SampleScene 明确拒绝并指向正确入口，不允许静默再造胶囊/覆盖新场景。具体函数拆分按最小改动，验收要求是重复执行不回退目标类型或制造重复目标。
- **ShootingSavedSceneAudit**：保留原胶囊断言，在 Sandbox_Shooting 校验；公共玩家/音效/引用校验复用，新机甲审计显式检查新 Prefab、动画、UI、碰撞和无测试残留，不通过泛化为“任意 Prefab 均可”消除约束。
- **ShootingDiagnostics**：继续仅启停 Controller 来重置，新增生命周期事实保证模型和 UI 同步，无需让诊断工具直接操作 Animator 或血条。

旧状态与新计划的冲突以此处迁移明确登记：代码的立即隐藏移交 View，审计的固定胶囊路径按场景分开；既有伤害规格无需降低要求。上一轮未提交的实现是当前基线，不能为获得干净差异而回滚。

### D8. 验证与完成条件

1. 领域/生命周期：重跑既有伤害公式、非法参数、去重/一次死亡；新增注册/注销事实的提交顺序、失败不发事件、重复注销、新身份隔离及事件中查询。
2. 真实模型：在 Editor 检查导入 clip 名称/长度/事件、骨架与材质，再在 Play Mode 验证贴地、待机无根漂移、正常受击及死亡实际播放。测试据实际选中 clip 时间等待，不以固定一帧模拟播放完成。
3. 命中/动画：现有枪口射击命中机甲、两个实例独立扣血；连发不重启 Hit、零扣血、受击中致死、重复事件不重播、死亡碰撞当帧关闭、后方目标可击中；检查旧反馈次数未增加。
4. UI：用独立预期表校验 H=0/0.5/1/1.2/1.6 和极大合法值，覆盖血条比例、覆盖系数、晚绑定、墙后/离屏/背面/超距、Trigger/自身过滤和相机缺失。用实际画面确认字符完整、可读与不遮挡准星。
5. 生命周期：Controller/View 各三次启停、死亡中重置、旧身份事件、离屏死亡、缺动作/相机/UI 引用；退出场景和两次独立 Play/Stop，核对订阅/显示对象与 Console。缺项测试的预期诊断单列，交付配置零未解释错误/警告。
6. 回归：Sandbox_Shooting 重跑领域、物理、完整输入/反馈和保存审计；在机甲场景做 30/60/120fps 的待机/连发/死亡/重置/镜头检查，以及玩家站立、八方向行走、纯向前跑和移动跳落试射。复用既有枪口/双手阈值；若改到玩家表现/动画/音频，追加对应完整回归，不能以旧报告替代。
7. 保存重载与验收：核对新资产 GUID/meta、场景/Prefab 引用、原素材/包版本无变化；正常启动没有自动验证。提交含待机、受击、死亡、血条和遮挡的实际游玩入口或录像，由用户确认观感。检查连续射击及重置时血条/表现对象数量有界，不声称未经测量的性能提升。

报告写入被忽略的 `.utmp/enemy-presentation/`；共享验收文档记录本次结果、失败/未运行和用户认可。所有任务在对应实现和验证完成后才勾选。

## Risks / Trade-offs

- [模型瞬间消失或旧死亡计时误伤新身份] → 隐藏权移交唯一 View，以身份锁存和新注册复位处理；死亡中重置专项验证。
- [连发把动作卡在起点] → Hit 播放中不重启、不排队；持续扣血与表现节奏分别验证。
- [View 启用次序造成漏绑或幽灵 UI] → 提交后事实、订阅后快照、注销显式清理，不依赖脚本相对顺序。
- [原胶囊测试与 setup 覆盖机甲] → 两类场景入口和保存审计分开，保留旧断言并验证重复执行安全。
- [粗命中体与动画轮廓有差异] → 本期限定静止躯干命中、按实际动作标定；精确部件命中另立交付，不暗中增加倍率。
- [世界空间 UI 随距离变小或被墙遮挡错误] → 配置尺寸/距离、最终相机求值和定向遮挡验证，以实际画面验收。
- [死亡动画/字体素材存在但实际不可用] → 采用前运行验证与字符检查；只做项目派生修复，不将配置兜底算成功交付。

## Migration Plan

先扩展注册事实及验证，再拆分 Controller/胶囊表现并保持 Sandbox_Shooting 通过；然后制作机甲动画和 UI，接独立新场景验证，最后替换 SampleScene。所有资产经 Editor/AssetDatabase 创建或迁移；本提案阶段不执行资产操作。

若接入出现回归，在本 change 新增资产/接线范围恢复原胶囊配置，保留既有射击与用户修改；不使用全工作区回滚。新的状态说明仅在实现后进入 ARCHITECTURE/INTERFACE 等现状章节；规划入口先更新 TODO 和 DEVELOPMENT_PLAN。完成运行及用户验收后再同步三份 delta、文档和归档；本次不提交/推送。

## Open Questions

没有阻塞 change 范围或实施拆分的未决项。以下仅是所选方案内的运行标定：

- mech_defender 的最终统一缩放、躯干碰撞尺寸和头顶锚点高度，需要按实际导入包围盒及动作画面微调。
- 动画过渡、UI 宽高/颜色/字号和显示距离按文中初值试验，需用户视觉验收；不因此增加敌人行为或 UI 功能。

完整骇入的溢出、H 持续时间及后续 AI/波次规则保持在原文档中待定，不由本 change 定义，也不阻塞本次消费既有状态。

## Post-acceptance asset closeout — 2026-09-19

用户确认全部验收通过后，说明按 TMP 提示导入了 Essential Resources / Examples & Extras，并明确要求按项目规则整理后继续归档、提交与推送。本节承接该授权，保持已验收玩法不变。

按现有 asset-organization 契约处理四项导入收口：先保留忽略目录中的可恢复导入备份；恢复仅被导入器改写 licenseType 的三份旧字体 meta；通过 AssetDatabase 将 CombatGirls 中与 TMP 包共用 GUID 的字体/Shader 小目录迁到 Assets/ThirdParty/UI/TextMeshPro，迁入刚导入的 LiberationSans OFL 许可；对 Assets/TextMesh Pro 的未使用 Examples & Extras、重复 TMP Settings 及其余未采用资源，在项目依赖检查确认无引用后通过 AssetDatabase 删除。

保持 Assets/_Project/Settings/Resources/TMP Settings.asset 为唯一运行配置。项目资产 GUID 引用自动保持，Editor setup 的两处字体路径改为新位置。保留的供应商文件内容不改，必要 .meta 保持 GUID；不升级 TMP 3.0.7 或修改 manifest。验证迁移映射/GUID、路径与 meta、唯一 TMP 设置、全部项目依赖、编译/Console、机甲保存审计及实际字体/血条运行。该收口适用已有资产规格，不增加新玩法 capability。
