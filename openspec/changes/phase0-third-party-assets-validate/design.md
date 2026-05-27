## Context

UNOMATA 是 TPS+接龙双线 Unity 项目，Phase 0 阶段需在写任何 Phase 2 业务代码前把所有第三方资产纳管到 `Assets/ThirdParty/` 下。当前 Phase 0 已完成 3 个包（CombatGirls / StarterAssets / MagicaCloth2）的导入与 URP 转换（一层结构 `ThirdParty/<PackageName>/`），但近期又导入 5 个新包散落根目录：

- `Assets/Behavior Designer/`（Opsive Behavior Designer，BT 框架）
- `Assets/Gizmos/`（Behavior Designer 提供的 Gizmo 图标，**Unity 引擎保留路径**）
- `Assets/Mech Pack/`（敌人模型+动画）
- `Assets/Sci fi 2in1/`（地图/竞技场，含 Sci Fi Arena 1 / 2 两套）
- `Assets/FORGE3D/Sci-Fi Effects/`（VFX）
- `Assets/Sci-Fi Weapons-Bullet Hell Sound Effects Pack/`（音效）

约束：

- Unity 2022.3.62f1 LTS + URP，所有资产移动 MUST 走 Unity AssetDatabase API（保 GUID），禁文件系统命令
- 现有 `asset-organization` spec 仅约定一层结构，无法承载新增分类
- BT 框架先前在 DEV plan Phase 2.3 C2 标记为"`com.unity.behavior` 或第三方"，本次需敲定 = Opsive Behavior Designer
- 验证档位：B 档（每包跑通最小 demo，不接业务），不在本 change 做集成
- 不动 `Assets/QFramework/` 与 `Assets/QFrameworkData/`（路径硬编码）

## Goals / Non-Goals

**Goals:**

- 把 5 个新包 + 3 个已有包统一迁入 `Assets/ThirdParty/<分类>/<PackageName>/` 二层结构
- 每个新包通过一个独立 `Sandbox_<PackageName>.unity` 场景验证最小可见行为
- URP 材质兼容性确认覆盖 Mech Pack / SciFiArena / SciFiEffects 三个高风险包
- 同步更新 DEPENDENCIES.md / DEVELOPMENT_PLAN.md / agent rules，使文档与现状一致
- 敲定 BT 框架选型为 Opsive Behavior Designer
- 保持 Unity Console 零红色错误（GUID 引用未断、编译通过）

**Non-Goals:**

- 不做敌人 AI 业务集成（BT 树编排、`MeleeAttack` 自定义 Action 等留给 Phase 2.3 C2）
- 不替换 Phase 2.1 已搭好的 PlayerArmature 角色控制器
- 不把 SciFiArena 替换 SampleScene 的占位地形（Phase 2.3 之后再做）
- 不解决无法 URP 转换的材质（标记为 Phase 6 打磨期遗留项即可）
- 不引入新的 Package Manager 依赖（Behavior Designer 是 Asset Store 资产包）
- 不做接 QFramework 的 BT 任务（如自定义 `MeleeAttack` Action）

## Decisions

### D1：目录二层化 vs 维持一层

选 **二层** `ThirdParty/<分类>/<PackageName>/`。

**分类约定**：

| 分类 | 内含包 | 备注 |
|---|---|---|
| `Characters/Player/` | CombatGirls | 玩家模型动画 |
| `Characters/Enemy/` | MechPack | 敌人模型动画；取代原空壳 `Monsters/` |
| `Locomotion/` | StarterAssets | TPS 控制器 |
| `Cloth/` | MagicaCloth2 | 布料物理 |
| `Environment/` | SciFiArena | 场景/地图（保留壳目录名） |
| `VFX/` | SciFiEffects | 特效（保留壳目录名） |
| `Audio/` | SciFiWeaponsBulletHell | 音效（新建壳） |
| `AI/` | BehaviorDesigner | AI 框架（新建壳） |

**为什么不维持一层**：

- 一层下现已 3 包，再加 5 包共 8 个目录，按"按用途归类"的认知模型查找成本陡增
- 同类资产（玩家/敌人均为 Characters）放一起便于团队默认查找
- DEPENDENCIES.md 资产表增长后，文档与文件系统结构出现错配

**代价**：

- 已有 3 包必须同步迁移（一次性付清）
- `asset-organization` spec 是 BREAKING 修改

### D2：作者目录拍平 vs 保留

新进 `Assets/FORGE3D/Sci-Fi Effects/` 这一层 `FORGE3D` 是作者命名。**拍平**为 `ThirdParty/VFX/SciFiEffects/`。

**理由**：

- 作者名对游戏开发无意义，包名足以识别
- 与 Mech Pack（无作者外层）保持一致
- 减少深度，IDE/Editor 文件树更易扫读

### D3：`Assets/Gizmos/` 处理

**保持原位不动**。Unity 引擎 Editor 自动从 `Assets/Gizmos/` 加载 Gizmo 图标（详见 Unity Manual / Gizmos Class），**该路径硬编码**。Behavior Designer 包导入时把 hierarchy 与 scene 图标放进该目录。

**契约后果**：`asset-organization` spec 必须新增例外条款：`Assets/Gizmos/` 与 `Assets/QFramework[Data]/` 同列 Unity / 框架硬编码路径例外。

### D4：BT 框架选型

选 **Opsive Behavior Designer**（已导入 `Assets/Behavior Designer/`）。

**调研结论**：内置库覆盖 Phase 2.3 C2 需求 `Idle → Detect → Chase → Attack`：

| 节点 | 内置任务 |
|---|---|
| Idle | `Actions/Idle.cs` |
| Detect | `Conditionals/Physics/`（OverlapSphere / Raycast）+ Tag/Layer 比较 |
| Chase | `Tasks/Unity/NavMeshAgent/SetDestination` 或 `Transform/MoveTowards` |
| Attack | 自写 `MeleeAttack : Action` → `this.SendCommand<DamagePlayerCommand>(damage)` |

**对比 `com.unity.behavior`**（未选）：

- 用户已熟悉 Behavior Designer
- 资产已导入，再装 PM 包冗余
- Unity 官方 BT 包尚在迭代，API 稳定性不如 Behavior Designer

**代价**：未来若需要切换 BT 框架，BT 资产（`.behavior` 文件）不可移植；但此风险可接受。

### D5：B 档验证粒度

每包验证内容如下，**不接业务、不接 QF**：

| 包 | Sandbox 场景 | 最小可见行为 |
|---|---|---|
| Mech Pack | `Sandbox_MechPack.unity` | 1 mech prefab + Animator 播 Idle，无报错 |
| SciFiArena | `Sandbox_SciFiArena.unity` | 拖入 Arena 主 prefab，PlayerArmature 移动可达，Collider 阻挡 |
| SciFiEffects | `Sandbox_SciFiEffects.unity` | 1 特效 prefab，Play Mode 自动播放可见 |
| Audio | `Sandbox_Audio.unity` | 1 AudioSource 引用一段武器音效，Play Mode 可听 |
| Behavior Designer | `Sandbox_BT.unity` | 空 GameObject + `BehaviorTree` 组件 + 最简 BT（Sequence + Idle + Log），Play Mode 不报错且 tick |

**不在本 change 做**：

- A 档（仅 import 验证）：太轻，不能暴露 URP 紫材质等隐患
- C 档（接业务集成）：抢 Phase 2 的活，且会跨多个 capability

**Sandbox 场景目录**：`Assets/_Project/Scenes/Sandbox/`，命名 `Sandbox_<PackageName>.unity`。**禁污染** `SampleScene`。

### D6：URP 材质兼容性策略

新进 Mech Pack / SciFiArena / SciFiEffects 大概率含 Built-in 管线材质，URP 下表现紫色。流程：

1. 导入后场景拖一个代表性 prefab
2. 紫材质 → 跑 `Edit → Render Pipeline → Universal Render Pipeline → Convert Selected Built-in Materials to URP`
3. 转换不动者（如自写 Shader 包）登记到 `tasks.md` 末尾"遗留项"段，标 Phase 6 处理
4. SciFiEffects 粒子 Shader 单独检查（Particles/Standard 系列在 URP 下需手动替换为 Particles Lit / Unlit）

**不强制 100% URP 转换通过**——B 档允许少量材质遗留，能跑通 Sandbox 即可。

### D7：迁移操作通道

**统一走 Unity AssetDatabase API**。具体执行通过 unityMCP 的 `manage_asset` action=move 或在 Unity 内手动拖拽。

**禁止**：PowerShell `mv`、`Move-Item`、文件系统 `git mv`（会破坏 GUID 引用）。

每次 move 后立即 `AssetDatabase.Refresh()`，确认 Console 零错误后再做下一个。

## Risks / Trade-offs

- [二层化是 BREAKING 改动，已有 3 包路径迁移] → 一次性付清；前置 change `phase0-cleanup-and-validate` 已归档，本次延伸是合理演进；归档前同步刷 DEPENDENCIES.md / DEV plan / rules
- [Mech Pack / Arena 材质大量紫色] → 跑 URP Convertor；不动者标 Phase 6 遗留；B 档允许少量遗留
- [SciFiEffects 粒子 Shader 不兼容 URP] → 单独检查，必要时手动替换 Shader；不影响其他包
- [Sandbox 场景污染 SampleScene] → 强制使用 `Assets/_Project/Scenes/Sandbox/` 子目录，spec 中明文约定
- [Gizmos 目录被误整理到 ThirdParty 下] → spec 明确例外条款；rules.mdc 同步说明
- [Behavior Designer 资产体积较大但只用 BT 核心] → 不裁剪（Asset Store 包不可裁剪），版本控制忽略其大小
- [Opsive Behavior Designer 是付费资产，未来若新成员不持有 license 将无法本地构建] → 项目内已包含资产，不需重新购买；提交规则里资产入库走 LFS 或直接入仓
- [URP 转换不可逆，转换失败的材质回滚成本高] → 转换前确认包整体可转换；逐包测试不混转
- [Sandbox 场景未来维护成本] → 可保留作为冒烟测试场景；如冗余可在 Phase 6 删除

## Migration Plan

按 `tasks.md` 顺序执行：先建分类壳目录 → 迁移 5 个新包 → 迁移 3 个已有包 → URP 检查 → Sandbox 验证 → 文档同步。

回滚策略：每步 git commit；任何步骤导致 Console 红色错误时立即 `git reset` 该步并定位 GUID 断裂源。

## Open Questions

- 暂无（用户已就分类、档位、change 切分、BT 框架四项决策给出明确回答）
