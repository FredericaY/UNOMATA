### Requirement: 每个第三方资产包必须通过最小可用性 Sandbox 场景验证

每个新导入的第三方资产包 SHALL 通过一个独立的 `Sandbox_<PackageName>.unity` 场景进行 B 档最小可用性验证：场景中放入该包的代表性 prefab / 组件，进入 Play Mode 后 SHALL 在不接业务、不接 QFramework、不依赖其他 capability 的前提下展现"最小可见行为"，且 Unity Console 在 Play Mode 期间不出现红色错误。

"最小可见行为" SHALL 满足：

- 模型类资产：模型在场景中正常渲染，Animator 至少播放一段 Idle 或默认动画无报错
- 场景类资产：主 prefab 拖入空场景后，PlayerArmature（或等效角色）可移动、Collider 阻挡正常
- 特效类资产：特效 prefab 在 Play Mode 自动播放且可见
- 音效类资产：AudioSource 引用一段音频，Play Mode 自动播放且可听
- 框架类资产（如 BT 框架）：核心组件挂在空 GameObject 上，能 tick 一次最小行为节点（如 Idle + Log）

#### Scenario: Mech Pack 通过 Sandbox 验证

- **WHEN** `Sandbox_MechPack.unity` 进入 Play Mode
- **THEN** 场景中至少 1 个 mech prefab 正常渲染，Animator Controller 播放 Idle 动画无 Console 报错

#### Scenario: SciFiArena 通过 Sandbox 验证

- **WHEN** `Sandbox_SciFiArena.unity` 进入 Play Mode
- **THEN** 场景包含 Arena 主 prefab 与 PlayerArmature；玩家可在 Arena 内移动，Collider 正常阻挡墙体与障碍物，Console 无红色错误

#### Scenario: SciFiEffects 通过 Sandbox 验证

- **WHEN** `Sandbox_SciFiEffects.unity` 进入 Play Mode
- **THEN** 场景中至少 1 个特效 prefab 自动播放，粒子系统正常渲染，Console 无红色错误

#### Scenario: SciFiWeaponsBulletHell 音效通过 Sandbox 验证

- **WHEN** `Sandbox_Audio.unity` 进入 Play Mode
- **THEN** 场景中至少 1 个 AudioSource 自动播放一段武器音效，Console 无红色错误

#### Scenario: Behavior Designer 通过 Sandbox 验证

- **WHEN** `Sandbox_BT.unity` 进入 Play Mode
- **THEN** 场景中至少 1 个 GameObject 挂载 `BehaviorTree` 组件并外接最简 BT（包含 Sequence + Idle + Log 节点），BT 正常 tick 且 Log 节点输出可见，Console 无红色错误

---

### Requirement: Sandbox 场景目录与命名规范

所有第三方资产可用性验证 Sandbox 场景 SHALL 统一存放在 `Assets/_Project/Scenes/Sandbox/` 子目录下，命名格式为 `Sandbox_<PackageName>.unity`，其中 `<PackageName>` 与 `Assets/ThirdParty/<分类>/<PackageName>/` 中的包名保持一致。

Sandbox 场景 SHALL 不污染 `SampleScene.unity` 或任何其他生产场景：禁止把验证用的临时 prefab、组件、AudioSource 等放进 `SampleScene` 进行验证。

#### Scenario: Sandbox 场景统一存放在专属子目录

- **WHEN** 资产验证场景创建完成
- **THEN** 所有 `Sandbox_*.unity` 场景文件位于 `Assets/_Project/Scenes/Sandbox/` 目录下；`Assets/_Project/Scenes/` 根目录及其他子目录下不存在 Sandbox 场景

#### Scenario: SampleScene 未被验证用临时对象污染

- **WHEN** 资产验证完成
- **THEN** `Assets/_Project/Scenes/SampleScene.unity` 中不包含任何仅用于第三方资产验证的临时 prefab 实例、AudioSource、特效 prefab 或测试用 BT 组件

---

### Requirement: BT 框架选型敲定为 Opsive Behavior Designer

项目敌人 AI 的 Behavior Tree 框架 SHALL 使用 Opsive Behavior Designer，位于 `Assets/ThirdParty/AI/BehaviorDesigner/`。该选型 SHALL 在 `Docs/DEPENDENCIES.md` 与 `Docs/DEVELOPMENT_PLAN.md` Phase 2.3 C2 任务行同步落地，不再使用 `com.unity.behavior` 或其他 BT 包。

#### Scenario: DEPENDENCIES.md 标注 BT 框架选型

- **WHEN** 文档更新完成
- **THEN** `Docs/DEPENDENCIES.md` Asset Store 资产表中 BehaviorDesigner 条目"用途"列明确写为"敌人 AI Behavior Tree 框架"，且其状态列为"已验证-Sandbox demo"

#### Scenario: DEVELOPMENT_PLAN.md Phase 2.3 C2 BT 选型敲定

- **WHEN** 文档更新完成
- **THEN** `Docs/DEVELOPMENT_PLAN.md` Phase 2.3 C2 任务行明确写明"BT 包：Opsive Behavior Designer（已导入并验证），无需走 Package Manager"，删除任何"`com.unity.behavior` 或第三方"的二选一措辞
