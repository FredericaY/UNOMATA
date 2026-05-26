## MODIFIED Requirements

### Requirement: GameApp Architecture 入口类可正常初始化

`GameApp` 继承 `Architecture<GameApp>`，`Init()` 方法 SHALL 按以下顺序完整注册 Phase 2 所需的 Model 与 System（不再是空实现）：

```
Model 层（先注册）：
  RegisterModel<PlayerModel>(new PlayerModel())
  RegisterModel<WaveModel>(new WaveModel())

System 层（后注册）：
  RegisterSystem<PlayerSystem>(new PlayerSystem())
  RegisterSystem<WaveSystem>(new WaveSystem())
```

注册顺序约束：**所有 `RegisterModel` 调用 SHALL 先于所有 `RegisterSystem` 调用**，符合 QFramework IOC 依赖解析规则。

#### Scenario: GameApp 入口类正常运行

- **WHEN** 场景中挂载 GameApp 初始化组件并进入 Play Mode
- **THEN** Console 无运行时异常，GameApp 实例正常创建，`PlayerModel` / `WaveModel` / `PlayerSystem` / `WaveSystem` 均可通过 `GetModel<T>()` / `GetSystem<T>()` 获取非 null 实例

---

## ADDED Requirements

### Requirement: Phase 2 骨架验证链路

`QFrameworkValidator.cs` SHALL 扩展 Phase 2 验证，在 Play Mode 下验证以下链路并输出日志：
1. `PlayerModel.HP` 写读（赋值后读取返回新值）
2. `PlayerSystem.TakeDamage(float)` 触发 HP 变化（HP 减少且不低于零）
3. `WaveSystem` 可成功 `GetModel<WaveModel>()` 获取 WaveModel 引用

验证通过后 Console SHALL 输出包含 `[QF验证通过] Phase2 骨架 System/Model 链路正常` 的日志行。

#### Scenario: Phase 2 骨架验证通过

- **WHEN** 场景中存在挂载 QFrameworkValidator 的 GameObject，进入 Play Mode
- **THEN** Console SHALL 输出 `[QF验证通过] Phase2 骨架 System/Model 链路正常`，无红色错误
