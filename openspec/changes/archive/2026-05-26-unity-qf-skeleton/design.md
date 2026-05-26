## Context

当前 `GameApp.cs` 的 `Init()` 为空实现，仅有注释占位。`QFrameworkValidator.cs` 只覆盖 Phase 0 最小链路（`QFTestCommand → QFTestEvent`）。目录结构已预留 `Player/`、`Wave/`、`Commands/`，但均为 `.gitkeep` 占位。

Phase 2 的所有 Unity change（B1a~B4）都需要以 QF System/Model 为依托——`PlayerSystem.TakeDamage` 被 B3b 的敌人 AI 调用，`WaveSystem` 被 B3c 的波次管理器填充，`StartHackCommand` 被 B4 的骧入触发调用。本 change 一次性建立这套骨架，避免后续每个 change 各自"裸注册"。

## Goals / Non-Goals

**Goals:**
- 在 `GameApp.Init()` 完整注册 Phase 2 所需 Model（PlayerModel / WaveModel）和 System（PlayerSystem / WaveSystem）
- 建立统一的 `Commands/` 目录，声明 4 个 Command 骨架（StartHackCommand / SelectCardCommand / HealCommand / DamagePlayerCommand）
- 扩展 `QFrameworkValidator.cs`，Play Mode 可验证 Phase 2 骨架链路——PlayerModel HP 写读、PlayerSystem.TakeDamage 扣 HP、WaveSystem 可获取 WaveModel

**Non-Goals:**
- **不实现**任何 WaveSystem 业务逻辑（OnStartWave / OnEnemyKilled 留空，B3c 填充）
- **不实现**任何 Command 的 OnExecute 业务逻辑（B3b / B4 填充）
- **不涉及** SyncRateModel / SyncRateSystem / HackSystem（Phase 4 另建）
- **不修改**任何场景文件或 Prefab

## Decisions

### D1：PlayerModel.HP 使用 `float` 而非 `int`
- **结论**：`BindableProperty<float>`
- **原因**：Phase 4 联动时，敌人攻击伤害经过 `DamageReductionFactor`（float）计算后施加给玩家，使用 float 避免精度损失和类型转换。整数 HP 表现层（如显示整数血条）由 UI 层 `Mathf.RoundToInt` 处理。
- **备选**：int HP + float 中间缓冲变量；排除原因：增加不必要的转换逻辑。

### D2：WaveModel 在 B0 只暴露 `AliveCount`，不维护敌人列表
- **结论**：`BindableProperty<int> AliveCount`，无 `List<EnemyController>`
- **原因**：B0 阶段 `EnemyController` 类型尚未存在（B3a 才创建），Model 不能依赖未定义类型。AliveCount 已足够支撑波次全灭判定（归零即可），列表管理由 B3c 扩展。
- **备选**：`List<GameObject>`；排除原因：类型太宽泛，B3c 扩展时仍需重构。

### D3：Commands 统一放 `Gameplay/Commands/` 子目录
- **结论**：`Assets/_Project/Scripts/Gameplay/Commands/*.cs`，命名空间 `Unomata.Gameplay`
- **原因**：ARCHITECTURE.md 明确 Commands 作为独立层级；统一目录比按功能散落更便于新成员查找和 AI Agent 检索。
- **备选**：按功能归入 Player/ Wave/ 等子目录；排除原因：Command 本质跨层，归属单一功能目录语义不清。

### D4：PlayerSystem 直接暴露 `TakeDamage(float)` 公开方法
- **结论**：`PlayerSystem.TakeDamage(float raw)` 为 `public` 方法
- **原因**：B3b 敌人 AI 需通过 `DamagePlayerCommand.OnExecute` 调用 `this.GetSystem<PlayerSystem>().TakeDamage(damage)`；PlayerSystem 内聚 HP 边界逻辑（不低于零），不让 Command 自己处理边界。
- **备选**：只通过 Event 驱动；排除原因：过度解耦，单向数据流已足够。

### D5：QFrameworkValidator.cs 原地扩展，不新建 Phase2Validator
- **结论**：在现有文件中追加 Phase 2 验证代码
- **原因**：规则要求单文件不超过 300 行；当前文件约 50 行，追加 Phase 2 验证后预计 ~120 行，未超限。保持单一验证入口便于管理。

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|---------|
| Command 骨架 `OnExecute` 为空——后续 change 忘记填充会静默失败 | QFrameworkValidator 扩展时不验证 Command 业务逻辑（那是 B3b/B4 的验收范围），骨架本身不引入 bug |
| WaveModel 缺少敌人列表——B3c 可能需要补充 | B3c 的 TODO 范围已明确标注"扩展 WaveModel"，不视为本 change 债务 |
| `BindableProperty<float>` 精度问题（如 HP = 99.9999）| UI 层 `Mathf.RoundToInt`；PlayerSystem.TakeDamage 中加 `Mathf.Max(0f, ...)` 边界防护 |
