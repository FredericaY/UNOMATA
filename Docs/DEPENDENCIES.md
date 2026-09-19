# DEPENDENCIES.md — 依赖清单

> 记录工程环境、包与资产依赖。恢复时按当前锁定版本核对；历史验收不等于本轮复验。

---

## 开发环境（2026-09-17 按工程文件核对）

| 项目 | 基线 / 来源 |
|---|---|
| Unity Editor | **2022.3.62f3**，revision 96770f904ca7；见 ProjectSettings/ProjectVersion.txt |
| Universal RP | **14.0.12** |
| .NET（CardChainCore） | **.NET 8 SDK**；Directory.Build.props 的 TargetFramework=net8.0 |
| QFramework | 历史导入记录为 1.0.187-Unity2018Compatible，源码资产位于 Assets/QFramework/；不在 manifest 依赖中 |
| IDE | 按个人环境选择，不作为运行时依赖 |

不自动升级 Editor 或依赖。原文“2022.3.x 最新补丁”已改为工程记录的精确版本。2026-09-17 首次盘点时缺少 SDK；后续已恢复 SDK 8.0.425，Core 构建与 139 项测试通过，Unity 也已连接并编译。首次 Play Mode 发现输入资产缺陷，见 [恢复记录](ENVIRONMENT_RECOVERY.md)。恢复命令见 [DEVELOPMENT_SETUP.md](DEVELOPMENT_SETUP.md)。

## Unity 包基线

下表来自 Packages/manifest.json；完整直接与传递依赖以 manifest 和 packages-lock.json 为准。

| 包 | 版本 / 来源 | 用途 |
|---|---|---|
| com.unity.cinemachine | 2.10.3 | TPS 相机 |
| com.unity.inputsystem | 1.14.2 | 输入 |
| com.unity.animation.rigging | 1.3.1 | 瞄准 IK |
| com.unity.render-pipelines.universal | 14.0.12 | 渲染 |
| com.unity.test-framework | 1.1.33 | Unity 测试 |
| com.unity.textmeshpro | 3.0.7 | 文本 |
| com.coplaydev.unity-mcp | Git URL 的 main 引用；lock hash 78ee5418415953b79c358bfe6355fcc3fde7912b | 编辑器工具连接 |

MCP 包已经写入工程不等于当前会话已连接 MCP。后续已通过本机 MCP 服务核对并操作正确工程；项目级 Codex 连接配置本地保留。此次不修改锁文件或将 main 引用升级。依赖可重复性后续可单独评估固定引用。

### 当前 Registry 配置

manifest 配置了名为 Unity China 的 scoped registry：`https://packages.unity.cn`，scope 为 `com.unity`。这是仓库现存配置，本轮未测量网络速度或镜像完整性；不再保留“全部同步”“固定下载速度”等未经本轮验证的保证。

排障时检查 manifest、packages-lock 和 Editor Package Manager 实际错误。不要通过包文档链接推断包实际下载源，不写入账号/代理凭据，不自动切换团队使用的 registry。

### QFramework 安装方式

项目已带有源码资产，无需为了恢复工程再次导入最新版。若从缺失框架的副本重建，以历史使用版本和当前仓库内容核对，从 [QFramework Releases](https://github.com/liangxiegame/QFramework/releases) 取得对应 unitypackage，再按 Unity 导入流程操作。

保留 `Assets/QFramework/` 与 `Assets/QFrameworkData/` 路径。QFramework 不属于 manifest 中的 Registry 包，也不迁入 ThirdParty 分类目录。

---

## Asset Store 资产

### 目录组织约定

第三方资产 SHALL 位于二层结构 `Assets/ThirdParty/<分类>/<PackageName>/` 下。`<分类>` 为按用途归类的一级目录，`<PackageName>` 为不含空格的 PascalCase 包名（拍平作者命名层）。

| 分类 | 含义 | 示例包 |
|------|------|--------|
| `Characters/Player/` | 玩家角色模型 + 动画 | CombatGirls |
| `Characters/Enemy/` | 敌人角色模型 + 动画 | MechPack |
| `Locomotion/` | 角色控制器 / 移动 | StarterAssets |
| `Cloth/` | 布料物理 | MagicaCloth2 |
| `Environment/` | 场景 / 地图 | SciFiArena |
| `VFX/` | 特效 / 粒子 | SciFiEffects |
| `Audio/` | 音效 / BGM | SciFiWeaponsBulletHell |
| `AI/` | AI 框架 / 资产 | BehaviorDesigner |
| `UI/` | HUD / 副线 UI / 主菜单等表现层资产 | （Phase 3 前选型，待补） |

**例外路径**（不属于 ThirdParty 二层结构覆盖范围）：

| 路径 | 原因 |
|------|------|
| `Assets/QFramework/` | QFramework 框架本体路径硬编码 |
| `Assets/QFrameworkData/` | QFramework 运行时配置路径硬编码 |
| `Assets/Gizmos/` | Unity 引擎保留路径，Editor 自动从该目录加载 Gizmo 图标（由 Behavior Designer 等资产包提供运行时 Gizmo 资源） |
| `Assets/StreamingAssets/` | Unity 引擎保留路径 |
| `Assets/Screenshots/` | 项目截图存放目录 |

### 资产清单

| 资产名 | 用途 | 目标目录 | 状态 |
|--------|------|---------|------|
| CombatGirls - RifleCharacterPack | 玩家角色模型+动画 | `Assets/ThirdParty/Characters/Player/CombatGirls/` | ✅ 已验证-方案B |
| FemaleRunnerAnimset | RifleGirl 跳跃/Land/RunJump 动画补充包（CombatGirls 主包缺失跳跃链路） | `Assets/ThirdParty/Characters/Player/FemaleRunnerAnimset/` | ✅ 已迁移-`phase0-femalerunner-animset-validate` |
| Starter Assets - Third Person Controller | TPS控制器基础 | `Assets/ThirdParty/Locomotion/StarterAssets/` | ✅ 已验证-方案B |
| MagicaCloth2 | CombatGirls 布料物理依赖 | `Assets/ThirdParty/Cloth/MagicaCloth2/` | ✅ 已导入 |
| MechPack | 敌人角色模型 + 动画（mech_defender / mech_walker / robot_dog） | `Assets/ThirdParty/Characters/Enemy/MechPack/` | ✅ 已迁移-URP 转换 3 mat |
| SciFiArena (Sci fi 2in1) | 竞技场场景（含 Arena 1 / Arena 2 两套） | `Assets/ThirdParty/Environment/SciFiArena/` | ✅ 已迁移-材质 URP 兼容 |
| SciFiEffects (FORGE3D) | 科幻 VFX 特效（爆炸 / 能量 / Warp / Holographic / Turret 等） | `Assets/ThirdParty/VFX/SciFiEffects/` | ⚠ 已迁移-19 mat Shader 缺失 + FORGE3D 框架依赖（详见下方注） |
| SciFiWeaponsBulletHell | 科幻武器音效（射击 / 爆炸 / UI） | `Assets/ThirdParty/Audio/SciFiWeaponsBulletHell/` | ✅ 已迁移 |
| BehaviorDesigner (Opsive) | 敌人 AI Behavior Tree 框架 | `Assets/ThirdParty/AI/BehaviorDesigner/` | ✅ 已迁移-Sandbox demo |
| TextMesh Pro 资源 | 敌人头顶字体与基础 UI 支持 | `Assets/ThirdParty/UI/TextMeshPro/` | ✅ 已归位并验证；来源 TMP 3.0.7 导入及既有共享资源 |
| UI 包（待选） | HUD / 副线接龙 UI / 骇入面板 / 主菜单 / 结算等表现层素材 | `Assets/ThirdParty/UI/<PackageName>/` | ⏳ Phase 3 副线 UI 阶段前选型；计划先用 Unity 原生 UGUI / TMP 占位，骇入 UI 尚未实现 |

### 状态语义

- ✅ **已验证-方案B**：`phase0-cleanup-and-validate` 期完成 URP 转换 + Retarget 验证
- ✅ **已迁移-...**：`phase0-third-party-assets-validate` 期完成二层目录迁移与对应处理
- ⚠ **已迁移-... 登记遗留**：迁移完成但有 Phase 6 待处理项（详见对应 change tasks.md 的"遗留项"段）
- ⏳ **待迁移-`<change-name>`**：包刚导入 Asset 根目录或临时位置，等待指定 change 完成二层目录归位 + 副作用清理
- ⏳ **待选型**：尚未挑选具体资产包，规划阶段占位条目

### UI 资产选型待办（Phase 3 前）

> 副线 UI（接龙小游戏 / 骇入面板）是 Phase 3 的核心交付，需要在 Phase 2 收尾前敲定 UI 资产方案。

**预期素材范围**：

- HUD：HP / 充能 / 弹药 / 准星 / 提示文字
- 骇入面板：4 张牌容器、计时圈、当前牌指示、连接动画
- 主菜单 / 暂停 / 结算 / 设置
- 字体（中文 + 英文）

**候选方向**（决策时再细评）：

1. **DOTween + Unity UGUI + 自绘 sprite** —— 零成本，全手撸；适合美术资源由队友自行整理的小项目
2. **Asset Store 科幻 UI 包**（如 `Sci-Fi GUI Pack` / `Cyberpunk UI Kit` / `HUD Toolkit` 等） —— 节省美术成本，与现有 SciFi 题材契合
3. **Modern Procedural UI**（Shader Graph 驱动） —— 自定义灵活但开发量大，与本期 SciFiEffects 风格统一可加分

**敲定时机**：Phase 2 TPS 主线收尾之后、Phase 3 副线 UI 任务起手之前。届时新建 change `phase3-ui-assets-import` 走完整 propose 流程。

**当前状态**：用 Unity 原生 UGUI + TMP 占位（如果 Phase 2 期间需要 HUD 雏形）。


### SciFiEffects（FORGE3D）特别说明

> Phase 2.3 战斗特效集成前必读。

**问题 1：FORGE3D 自驱动 prefab 依赖 F3DTime / PoolManager 单例**

包内挂载 `FORGE3D.F3D*` 自驱动脚本的 prefab（如 `Lightning Gun` / `Plasma Beam` / `Rail Gun` / `Warp Jump` / `Seeker Bolt` / `Pulsewave` / `Laser Impulse` / `Missiles` 主驱动等）依赖 `F3DTime.time` 与 `PoolManager` 单例。**独立场景内 Play 会触发 `NullReferenceException at F3DLightning.OnSpawned`**。

**集成方案三选一**：

1. 在 GameApp 启动时初始化 `F3DTime` 与 `PoolManager` 单例（场景里挂一个 `F3DTime` 组件 + `PoolManager` 组件即可）
2. 从包内 `Examples/` 场景拷贝完整启动器 prefab 作为初始化参考
3. **只挑用纯 `ParticleSystem` 自包含的 prefab**（推荐 Phase 2.3 起步）：
   - `Plasma Gun/` / `Flames/` / `Shot Gun/` / `Sniper/` / `Solo Gun/` / `Trails/` / `Vulcan/` / `Missiles/MissileFlame.prefab` / `Missiles/MissileSmokeTrail.prefab` 等不含 FORGE3D 脚本的 prefab

**问题 2：19 个材质 Shader 缺失**

包使用 Amplify Shader Editor 生成的 Shader 在 Unity 2022.3 + URP 14 下未能正常 import，对应 19 个材质回退为 `Hidden/InternalErrorShader`。涉及子模块：Burnout / Debris / Explosions/Shock_Ring / Heat / Holographic / Nebula / Warp Tunnel / Legacy Turret。

**修复方案**：用 Unity Shader Graph 重写 / 直接换 URP `Particles/Lit` 系列 / 影响范围窄的（如 Nebula 副本）直接删除。Phase 6 打磨期处理。Sandbox 验证选用的特效 prefab 已避开这 19 mat 引用。


---

## CardChainCore（独立.NET项目）

Core 层采用**独立 .NET 8 控制台工程**方案：开发期在 `CardChainCore/` 内迭代，Phase 4 时一次性将 `src/Unomata.Core/*.cs` 复制到 `Assets/_Project/Scripts/Core/` 并配 `Unomata.Core.asmdef`（`noEngineReferences=true`）。`tests/` 与 `console/` 不迁入 Unity。

### 运行时依赖

| 依赖 | 说明 |
|------|------|
| 无第三方 NuGet 包 | 运行时保持零外部依赖，纯 C# 标准库 |

### 开发期依赖（仅 tests/console 工程）

| 依赖 | 说明 |
|------|------|
| xUnit | 单元测试框架 |
| xunit.runner.visualstudio | IDE / `dotnet test` 运行器 |
| Microsoft.NET.Test.Sdk | .NET 测试 SDK |

> 不引入 FluentAssertions、Moq 等额外测试库：Core 为纯逻辑无外部依赖，xUnit 原生断言已足够。

---

## QFramework 实测兼容性记录

| 项目 | 结果 |
|------|------|
| 验证日期 | 2026-05-25 |
| Unity 版本 | 2022.3.62f1 LTS |
| QFramework 版本 | 1.0.187-Unity2018Compatible |
| 编译错误 | ✅ 零红色错误 |
| QFramework 菜单 | ✅ 正常出现 |
| `using QFramework;` 编译 | ✅ 通过 |
| `GameApp : Architecture<GameApp>` 初始化 | ✅ Play Mode 正常运行 |
| Command → System → Event 链路 | ✅ 全链路验证通过 |
| API Updater | 无弹窗；ResKit 有3条 CS0618 警告（UnityWebRequest.isNetworkError 废弃 API），不影响框架可用性 |
| **总结** | **完全可用**，可按 ARCHITECTURE.md 规划开始 Phase 1/2 开发 |

## 资产记录的恢复边界

上表中的“已验证/已迁移”沿用 2026-05 历史记录，本轮没有重新播放 Sandbox 场景。SciFiEffects 的材质和框架依赖遗留继续保留。

现有清单主要记录用途、路径和状态。第三方资产的完整版本、来源链接、作者、授权范围及凭证位置仍需逐包补齐；未核对的项保持待补，不根据仓库中已有资产推断可以公开再分发。购买凭证原件和账号信息不入库。


## 本轮角色资产适配（2026-09-18，已通过整体验收）

- `Assets/_Project/Prefabs/Player/ShoulderAimPlayer.prefab`：基于已有 RifleGirl/StarterAssets 场景角色的项目包装，保留原 Avatar、模型、武器材质和 MagicaCloth 配置；相机在场景上明确绑定。
- `Assets/_Project/Animations/Player/Aiming/ShoulderAim.controller`：项目运动/瞄准状态机。该目录的 `.anim` 从 CombatGirls RifleGirl 与 FemaleRunnerAnimset 现有资源复制，清理供应商挂点/音效事件；`Run_45/90/135/180/225/270/315` 由普通 `Run` 的腿部周期生成，原生成入口为 `AimDirectionalRunSetup`（现已停用方向跑生成），不修改供应商 fbx。
- `RifleAim.asset` 保存瞄准几何、躯干/肘标定和步幅配置；`Footsteps.asset` 保存项目动作的脚步相位。运行时不写回这些配置。
- `PlayerMotor.cs` 依据已安装 StarterAssets ThirdPersonController 适配输入消费、朝向及时序；不新增运动能力，不使用第二个活跃供应商运动组件。
- 本轮不安装/升级包，不新增采购资产；资产原始授权/版本缺项仍按上方登记，不由项目复制推断再分发权限。

`StableArmIKConstraint` / Job / Binder 为项目约束，复用 Animation Rigging 1.3.1 的 `TwoBoneIKConstraintData`、绑定器和无肘提示的双骨解算；肘转向使用固定肩手轴，修复反向投影时的端点漂移。包内 `AnimationRuntimeUtils`、`QuaternionExt` 等文件保持只读。

用户最终观感复验否定方向快跑后，`Run_45…315` 保留为未采用实验资产，已从当前控制器和 Footsteps 配置断开；`AimDirectionalRunSetup.Configure()` 现仅配置纯向前 Run 与各向 AimWalk，不再烘焙方向跑。

跳跃时序修订复用项目 `JumpStart.anim`（FRA R_Jump_AirR 副本），JumpStart/InAir 状态共享其物理相位；旧 `InAir.anim`（AirL）不再进入当前控制器。未修改 fbx 或包版本。

## 射击素材接入（2026-09-19，整体视听验收通过）

| 项目资产/配置 | 来源与本次处理 |
|---|---|
| `Assets/_Project/VFX/Shooting/RifleMuzzle.prefab` | SciFiEffects / Vulcan / vulcan_muzzle；项目副本，网格和灯光明确启停，灯光禁阴影 |
| `Assets/_Project/VFX/Shooting/RifleTracer.prefab` | SciFiEffects / Vulcan / vulcan_projectile；仅渲染捕获的枪口到命中点，不挂供应商弹丸/伤害逻辑 |
| `Assets/_Project/VFX/Shooting/RifleImpact.prefab` | SciFiEffects / Vulcan / vulcan_impact；项目副本，粒子非循环且不自销毁，项目池负责回收；死亡采用放大的同一效果 |
| `Assets/_Project/Settings/Combat/RifleFeedback.asset` | 绑定上述效果与现有 AudioClip，保存音量、尺度、寿命、池容量和轻微后坐参数 |
| 枪声 | SciFiWeaponsBulletHell / AUDIO/Shoot/Rifle_Shotgun_Pistol / SFX_SCIFI_WEAPON_Rifle_Shoot_1、2、3.wav |
| 表面命中音 | SciFiEffects / Sci-Fi Effects/Sounds / impact_projectile_001、002、003.wav |
| 敌人命中音 | 同上 / impact_projectile_metal_001、002、004.wav |
| `EnemyCapsule.prefab`、`Capsule0/1/2.asset`、项目 Capsule/Backstop 材质 | Unity 胶囊、项目状态适配与 URP/Lit 材质，供规则验证；真实敌人资产下一 change 接入 |

当前采用的 Vulcan 材质引用已有 FORGE3D/URP/Additive 和 FORGE3D/URP/AlphaBlended，Editor 支持检查及实际画面未出现粉色材质；没有因此修复包中其余未采用的 Shader。素材原件、fbx、音频源文件和包版本不变，沿用原资产授权记录的待补边界。

R_Shoot / R_AimIdle_AutoShoot 实测长度约 0.967 秒、带 SwitchSocket 事件。本次采用程序化轻微后坐位移并由已验收的武器/双臂求值链重新对准；不把供应商事件引入运行链。采用路径及运行报告见 [射击基线](SHOOTING_BASELINE.md)。

## 敌人表现接入（2026-09-19，已验收归档）

- 项目 `Prefabs/Enemies/MechDefender.prefab` 包装 `Assets/ThirdParty/Characters/Enemy/MechPack/Prefabs/Animated/mech_defender.prefab`，引用原骨架、网格和 URP/Lit 材质；项目包装移除原 Rigidbody/Collider，以自有躯干碰撞适配接入。
- 项目 `Animations/Enemies/MechDefender/{Idle,Hit,Death}.anim` 分别来自同包 `Animations/mech_defender@animations.FBX` 的 idle_01、damage_01、death_01；项目 controller 仅包含三个状态，关闭 Root Motion。使用 AnimationUtility 清理项目 clip 事件，原文件不改。
- 项目 `Settings/Combat/Enemies/` 保存表现与数值配置；原 EnemyCapsule 迁移到立即隐藏的独立 View，保留胶囊回归。
- 头顶 UI 使用现有 UGUI/TMP 包；字体直接引用 `Assets/ThirdParty/UI/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset` 及其已有 atlas/material。为补齐初始化新增项目 `Settings/Resources/TMP Settings.asset`，验收后用户导入的 TMP 示例与重复配置已按资产规则清理，不引入外部字体。
- 本节省略前缀的项目路径均位于 `Assets/_Project/`；引擎/包无升级，原资产来源版本与授权缺项继续保持待补，不推断再分发许可。当前采用链路的运行证据见 [ENEMY_PRESENTATION.md](ENEMY_PRESENTATION.md)。

## TextMesh Pro 资源归位（2026-09-19）

- 来源：Unity 的 TMP 导入提示，当前安装 com.unity.textmeshpro 3.0.7；与 CombatGirls 既有 TMP 小目录共用的字体/Shader 通过 AssetDatabase 保 GUID 统一迁到 Assets/ThirdParty/UI/TextMeshPro。
- 保留范围：Fonts/LiberationSans.ttf、Resources/Fonts & Materials 的 SDF 与 Fallback 资产、Shaders/TMP_SDF-Mobile.shader、Shaders/TMPro_Properties.cginc 和 Fonts/LiberationSans - OFL.txt。原供应商源内容保持，导入器对三份 meta 的非功能性 licenseType 改写已恢复。
- 字体随附许可为 SIL Open Font License 1.1，文件记录 Google（2010）及 Red Hat（2012）版权与保留字体名；原许可文本完整保留。此处不以 Unity meta 的 licenseType 字段推断版权授权。
- 未引用的 Examples & Extras、演示脚本、重复 TMP Settings 和其他未采用资源已裁剪；项目 Settings/Resources/TMP Settings.asset 是唯一运行配置。根 Assets/TextMesh Pro 以及 CombatGirls 内旧 TMP 目录均已消除。
- 没有新增或升级 Package Manager 依赖。本次新增许可文件很小，迁移的原字体二进制约 350KB，原 SDF 为已跟踪的文本资产；沿用普通 Git，无新增大型二进制或 LFS 配置。
- GUID、依赖、保存引用与实际字形/UI 复验见 [敌人表现记录](ENEMY_PRESENTATION.md)。其他第三方包授权的既有待补项不变。
