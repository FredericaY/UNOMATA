### Requirement: QFramework 在 Unity 2022.3 LTS 无编译错误

QFramework 导入后 Unity Editor SHALL 无编译错误。Console 中不出现红色错误（警告可接受），菜单栏出现 `QFramework` 顶级菜单入口。

#### Scenario: Editor 打开无红色错误

- **WHEN** Unity 2022.3.x LTS 加载 UNOMATA 项目
- **THEN** Console 红色错误数量为 0（黄色警告不计）

#### Scenario: QFramework 菜单存在

- **WHEN** Unity Editor 编译完成
- **THEN** 顶部菜单栏中可见 `QFramework` 菜单项

#### Scenario: 脚本可正常引用 QFramework 命名空间

- **WHEN** 新建 C# 脚本添加 `using QFramework;`
- **THEN** 脚本编译通过，无未解析引用错误

---

### Requirement: GameApp Architecture 入口类可正常初始化

`GameApp` 继承 `Architecture<GameApp>`，`Init()` 方法 SHALL 按以下顺序完整注册 Phase 2 所需的 Model 与 System：

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

### Requirement: QFramework IOC/事件机制最小流程可跑通

QFramework 的核心机制（Command → System → Event 链路）SHALL 在 Unity 2022.3 LTS 下可正常运行，事件可被正确触发和订阅。

#### Scenario: Command → System → Event 链路正常

- **WHEN** 执行一个最小验证流程：发送 Command → System 处理 → 触发 Event → UI/监听方收到事件
- **THEN** 全链路无异常，事件监听方正确收到通知，Console 输出预期日志

---

### Requirement: DEPENDENCIES.md 记录 QFramework 实测兼容性结论

`Docs/DEPENDENCIES.md` SHALL 在末尾补充「QFramework 实测兼容性记录」章节，记录验证结果（通过/部分通过/失败）和 API Updater 处理情况。

#### Scenario: DEPENDENCIES.md 包含兼容性记录

- **WHEN** 验证完成、文档更新后
- **THEN** DEPENDENCIES.md 末尾有「QFramework 实测兼容性记录」章节，包含验证日期、Unity 版本、结论及处理备注

---

### Requirement: Phase 2 骨架验证链路

`QFrameworkValidator.cs` SHALL 在 Play Mode 下验证以下链路并输出日志：
1. `PlayerModel.HP` 写读（赋值后读取返回新值）
2. `PlayerSystem.TakeDamage(float)` 触发 HP 变化（HP 减少且不低于零）
3. `WaveSystem` 可成功 `GetModel<WaveModel>()` 获取 WaveModel 引用

验证通过后 Console SHALL 输出包含 `[QF验证通过] Phase2 骨架 System/Model 链路正常` 的日志行。

#### Scenario: Phase 2 骨架验证通过

- **WHEN** 场景中存在挂载 QFrameworkValidator 的 GameObject，进入 Play Mode
- **THEN** Console SHALL 输出 `[QF验证通过] Phase2 骨架 System/Model 链路正常`，无红色错误
