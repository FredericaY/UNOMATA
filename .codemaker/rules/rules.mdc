---
description: AI coding guidance and rules for this repository.
alwaysApply: true
---
<rule>

# UNOMATA Agent Rules

## 项目概览

双线并行 TPS 验证项目。主线：TPS 竞技场；副线：接龙小游戏（UNO 改编），用于验证卡普空《PRAGMATA》双线 gameplay 的可扩展性。

- Unity 2022.3.x LTS + URP + QFramework 1.0.187
- 底层逻辑纯 C#（`Unomata.Core`），通过 C# 原生 `event` 与 Unity 端解耦
- 独立 .NET 8 控制台项目 `CardChainCore/` 用于 Core 层开发期验收
- 详见 `Docs/`：`ARCHITECTURE.md` / `INTERFACE.md` / `DEPENDENCIES.md` / `DEVELOPMENT_PLAN.md`

入口：`Assets/_Project/Scripts/Gameplay/GameApp.cs` —— `GameApp : Architecture<GameApp>`

---

## Unity 操作（最高优先级）

- **必须使用 UnityMCP** 直接在 Unity 中执行操作：创建/移动 GameObject、挂载/修改组件、AssetDatabase 操作、运行脚本、读取 Console 等
- 操作前先确认 Unity Editor 已打开且**处于非运行状态**（非 Play Mode）
- 涉及 Scene 修改后**提示用户保存场景**
- 资产移动**必须**走 Unity AssetDatabase API（保 GUID），禁止用文件系统 mv/rm 直接搬 `Assets/` 下的文件
- 编译错误以 Unity Console 红色错误为准，黄色警告可接受

---

## QFramework 架构规范（严格遵守）

UNOMATA 的 Unity 端**全部 gameplay 代码必须**走 QFramework 分层。任何绕过 QFramework 直接在 MonoBehaviour 间调用业务逻辑的代码视为违规。

### 分层职责

```
GameApp（Architecture<GameApp> 入口）
├── Systems          // 业务逻辑、跨帧状态、Tick 驱动
│   ├── HackSystem
│   ├── WaveSystem
│   └── PlayerSystem
├── Models           // 纯数据，无逻辑
│   ├── PlayerModel
│   └── WaveModel
└── Commands         // 一次性写操作，封装 System 调用
    ├── StartHackCommand
    ├── SelectCardCommand
    └── HealCommand
```

- **Model**：只放数据 + getter/setter，发 `PropertyChanged` 事件，禁写业务逻辑
- **System**：跨帧状态/订阅 Model 变化/调度业务，禁直接持有 MonoBehaviour
- **Command**：写操作的封装，继承 `AbstractCommand`，`OnExecute` 内只调 `this.GetSystem<T>()` / `this.SendEvent(...)`，禁返回值
- **Query**：读操作封装，继承 `AbstractQuery<TResult>`，`OnDo` 内返回数据，禁副作用
- **Event**：跨层广播，定义为 `struct` 放在对应 System 同目录
- **Controller**（MonoBehaviour）：实现 `IController`，`GetArchitecture() => GameApp.Interface`，**只**调 `this.SendCommand` / `this.SendQuery` / `this.RegisterEvent`，禁直接 new System/Model

### 强制写法

```csharp
// MonoBehaviour 必须这样接入 QFramework
public class XxxController : MonoBehaviour, IController
{
    IArchitecture IBelongToArchitecture.GetArchitecture() => GameApp.Interface;

    void Start()
    {
        // 事件订阅必须挂 UnRegisterWhenGameObjectDestroyed，防泄漏
        this.RegisterEvent<XxxEvent>(OnXxx).UnRegisterWhenGameObjectDestroyed(gameObject);
    }
}
```

- 注册顺序：Model 先于 System；System 内 `OnInit` 取 Model 用 `this.GetModel<T>()`
- `GameApp.Init()` 中按 `RegisterModel → RegisterSystem` 顺序注册，与 `GameApp.cs` 现有占位注释保持一致
- 禁止用 `FindObjectOfType` / 单例 / 静态字段 跨 Controller 通信，必须走 Event/Query

### 参考实现

- 入口模板：`Assets/_Project/Scripts/Gameplay/GameApp.cs`
- 链路验证：`Assets/_Project/Scripts/Gameplay/Tests/QFrameworkValidator.cs`（Command → System → Event 全链路示例）

---

## Core 层与接口边界（INTERFACE.md）

- `Assets/_Project/Scripts/Core/` 与 `CardChainCore/` 共享同一套 `Unomata.Core` 命名空间代码
- **Core 层严禁引用 `UnityEngine`**，包括 `Mathf` / `Time` / `Debug.Log` / `Vector3` 等
  - 用 `System.Math.Clamp` 替代 `Mathf.Clamp01`
  - 时间通过外部传入 `deltaTime`，不读 `Time.deltaTime`
  - 日志用 `System.Console` 或事件回调，不用 `Debug.Log`
- Core 与 Unity 端**只能**通过 `Docs/INTERFACE.md` 约定的公开类/事件交互：
  - 公开类：`HackSession` / `HackDifficultyConfig` / `CardData` / `HackResult` / `ComboType`
  - Unity 端持有 `HackSession`，订阅 7 个事件（`OnNewRound` / `OnChainSuccess` / `OnChainFailed` / `OnTimeUp` / `OnMaxReached` / `OnOverflow` / `OnSessionEnd`），每帧 `Tick(deltaTime)`，调 `SelectOption(index)`
- **接龙规则、计时、Combo 检测全部写在 Core 层**，Unity 端不实现任何接龙规则
- 接口签名变更前必须更新 `Docs/INTERFACE.md`，再改实现

---

## 目录约定

| 路径 | 用途 |
|------|------|
| `Assets/_Project/Scripts/Core/` | 底层纯 C# 逻辑，无 UnityEngine 依赖 |
| `Assets/_Project/Scripts/Core/CardChain/` | 接龙规则、牌组、会话、结算 |
| `Assets/_Project/Scripts/Core/Interfaces/` | Core 公开枚举/接口 |
| `Assets/_Project/Scripts/Gameplay/` | Unity 端业务（TPS、波次、敌人、玩家） |
| `Assets/_Project/Scripts/Gameplay/Linking/` | Core 事件 → Unity 行为适配层 |
| `Assets/_Project/Scripts/Gameplay/Tests/` | QFramework 链路与运行时验证脚本 |
| `Assets/_Project/Scripts/UI/` | 副线 UI、HUD，纯表现层 |
| `Assets/_Project/Prefabs/{Player,Enemy,UI,VFX}/` | 项目自有预制体 |
| `Assets/_Project/Scenes/` | 项目自有场景 |
| `Assets/_Project/Settings/` | URP 配置 |
| `Assets/QFramework/` | QFramework 框架本体（路径硬编码，禁移动） |
| `Assets/QFrameworkData/` | QFramework 运行时配置（路径硬编码，禁移动） |
| `Assets/ThirdParty/<PackageName>/` | 第三方资产，**只读** |
| `CardChainCore/` | .NET 8 控制台项目，开发期验收 Core 层 |
| `Docs/` | 项目文档 |

新增项目代码**只能**放 `Assets/_Project/Scripts/` 下对应子目录；第三方包导入后立刻移到 `Assets/ThirdParty/<PackageName>/`（QFramework 与 QFrameworkData 例外）。

---

## 代码规范

### 命名

- 命名空间：`Unomata.Core` / `Unomata.Gameplay` / `Unomata.Gameplay.Linking` / `Unomata.Gameplay.Tests` / `Unomata.UI`
- 类/方法/属性/公开字段/事件：`PascalCase`
- 接口：`IXxx`
- 局部变量/参数：`camelCase`
- 私有字段：`_camelCase`
- 事件命名：`OnXxx`（如 `OnChainSuccess`）
- 文件名 = 类名（一文件一公开类）

### 注释与格式

- 公开类/方法用 XML doc：`/// <summary>...</summary>`
- 接口区域用分组注释 `// ── 分组标题 ──────────────────────────────────────`（参考 `INTERFACE.md` 中 `HackSession` 写法）
- 验证/工具脚本头部用 `/** ... */` 块注释说明用途、链路、使用方法
- 单文件不超过 **300 行**，超出拆分（按职责拆 partial 或分类）

### 事件 / 异步 / 错误

- 事件统一用 C# 原生 `event Action` / `event Action<T>`，**禁用 `UnityEvent`**
- 跨层广播走 QFramework Event（`SendEvent` / `RegisterEvent`），不用 C# 原生 event
- Core 层禁用 Unity 协程；如需异步用 `async/await` + `CancellationToken`
- Unity 端 `try/catch` 只捕获预期异常，未预期异常让其抛出由 Console 暴露

### 常量

- 禁止魔法数字
- 常量放对应模块的 `XxxConfig` 类（如 `HackDifficultyConfig`）或 `static readonly` 字段
- 平衡数值统一放 ScriptableObject 或 Config 类，不硬编码在业务代码

---

## 关键参考

| 类/接口 | 路径 | 用途 |
|--------|------|------|
| `GameApp` | `Assets/_Project/Scripts/Gameplay/GameApp.cs` | QFramework Architecture 入口 |
| `HackSession` | `Unomata.Core`（待实现） | 单次骇入会话生命周期，见 `INTERFACE.md` |
| `HackDifficultyConfig` | `Unomata.Core` | 难度参数，由 Unity 端按波次生成 |
| `CardData` / `CardColor` | `Unomata.Core` | 牌数据 + `CanFollow(other)` |
| `HackResult` / `EndReason` | `Unomata.Core` | 结算，含 `DamageReductionFactor` |
| `ComboType` | `Unomata.Core`（v1 仅 `None`） | 预留 Combo 枚举 |
| `QFrameworkValidator` | `Assets/_Project/Scripts/Gameplay/Tests/QFrameworkValidator.cs` | QFramework 链路验证示例 |

新增 System / Model / Command / Event 时**先**查 `INTERFACE.md` 与 `ARCHITECTURE.md`，避免重复定义。

---

## 数据流（典型骇入流程）

```
[骇入键按下]
  Controller.SendCommand<StartHackCommand>
    → HackSystem 用 HackDifficultyConfig 创建 HackSession
    → session.Start() → 订阅 7 个事件
[每帧]
  HackSystem.Tick(Time.deltaTime) → session.Tick(dt)
[选牌]
  Controller.SendCommand<SelectCardCommand(index)>
    → HackSystem.CurrentSession.SelectOption(index)
[Core 事件回流]
  OnChainFailed  → Linking 层扣血
  OnOverflow     → Linking 层充能 +1
  OnSessionEnd   → Linking 层把 result.DamageReductionFactor 写入目标敌人
  所有事件       → UI 层刷新显示
```

---

## 响应规范（节省 Token）

- 读文件前先用 `grep` / `glob` 确认存在，避免无效读取
- 已知项目结构时不重复扫描目录
- 修改现有文件时只展示变更部分（diff 风格），不重复输出未修改内容
- 分析问题直接给结论，不逐步复述已知信息
- 代码块只包含必要上下文（前后各 3 行），不输出整个文件
- 同一轮可并行的只读工具调用合并到一次响应

---

## OpenSpec 归档纪律

归档一个 change 时 (`/opsx:archive` 或手动 mv 至 `openspec/changes/archive/<date>-<name>/`)，**必须**在归档前完成两条同步：

1. **delta specs → 主 specs**：把 `openspec/changes/<name>/specs/<cap>/spec.md` 的 ADDED/MODIFIED/REMOVED 内容应用到 `openspec/specs/<cap>/spec.md`（去掉 delta 操作头 `## ADDED Requirements` 等，恢复成裸 `### Requirement:` 结构）

2. **项目自带文档对齐现状**：扫描 `Docs/`、`README.md` 与其他用户可读文档，把 change 实施后产生的事实差异同步进去。包括但不限于：
   - `Docs/DEVELOPMENT_PLAN.md`：本 change 涉及的任务勾选状态、阶段进展
   - `Docs/ARCHITECTURE.md` / `Docs/DEPENDENCIES.md` / `Docs/INTERFACE.md`：与现实结构/依赖/契约一致
   - `README.md`：顶层目录结构、环境要求、快速开始指引
   - `Docs/TODO.md`：已完成任务的清理或迁移
   - 其他被 change 影响的 `.md` 文档

**自检流程**（归档前必跑）：

```
1. 列出 change 改动的代码/文件范围（git status / change 的 tasks.md / proposal "Impact" 段）
2. 用 grep 扫所有 Docs/*.md 与 README.md 中是否还有"过时陈述"
   关键词举例：被删除/重命名的路径、未勾选的已完成任务、版本号/技术选型旧值
3. 把不一致项一一修正 → 再 mv 到 archive
4. 归档动作本身只是最后一步，不是开始
```

**违规信号**：归档完成后用户发现 `Docs/` 仍写着"待完成"、README 仍指向已删目录、DEPENDENCIES 还列着已弃技术——视为归档不完整，必须立即补做对齐。

未对齐就归档 = 制造文档债。后续看文档的人（包括队友、未来的自己、AI agent 自己）会被错误现状误导。

---

## 禁止事项

- ❌ 不修改 `Assets/ThirdParty/` 下任何文件
- ❌ 不移动 `Assets/QFramework/` 与 `Assets/QFrameworkData/`（路径硬编码）
- ❌ Core 层不引入任何 Unity 依赖（`UnityEngine` / `UnityEditor` / `Mathf` / `Time` / `Debug` 等）
- ❌ 不绕过 `Docs/INTERFACE.md` 约定的接口直接跨层调用
- ❌ 不绕过 QFramework 直接在 MonoBehaviour 间调用业务逻辑
- ❌ 不用 `FindObjectOfType` / 静态单例 跨 Controller 通信
- ❌ 不用 `UnityEvent`；不用 `Debug.Log` 写在 Core 层
- ❌ 不自行创建文档文件（`.md`），除非用户明确要求
- ❌ 不用文件系统命令（mv/rm/cp）直接搬 `Assets/` 下的资产，必须走 Unity AssetDatabase

</rule>
