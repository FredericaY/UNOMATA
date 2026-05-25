## ADDED Requirements

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

按 ARCHITECTURE.md 规划，项目 SHALL 能创建一个继承自 `Architecture<GameApp>` 的 `GameApp` 类，并在场景中正常完成初始化（不抛出运行时异常）。

#### Scenario: GameApp 入口类正常运行

- **WHEN** 场景中挂载 GameApp 初始化组件并进入 Play Mode
- **THEN** Console 无运行时异常，GameApp 实例正常创建

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
