## Context

Phase 0 仅余 A 端的 Core 工程脚手架。Core 层契约 (`Docs/INTERFACE.md`) 已冻结，工程方案 (`Docs/DEPENDENCIES.md` / `Docs/ARCHITECTURE.md` / `Docs/DEVELOPMENT_PLAN.md`) 已在上一轮对话决策完成：

- **方案 X**：独立 .NET 8 工程 `CardChainCore/`，Phase 4 一次性复制 `src/Unomata.Core/*.cs` 至 Unity
- **测试框架**：xUnit，零额外测试库（不引入 FluentAssertions / Moq）
- **运行时依赖**：Core 严禁引用 `UnityEngine`，亦不引入任何第三方 NuGet
- **目录基线**：`CardChainCore/` 当前仅含一个 `.gitkeep`，干净起步

本设计仅覆盖工程结构与构建配置，不涉及任何接龙业务代码（Phase 1 范畴）。

## Goals / Non-Goals

**Goals:**

- 三个 `.csproj` 在 `dotnet build`、`dotnet test`、`dotnet run` 下均可成功，作为 Phase 1 TDD 闭环的起点
- 工程结构对 Phase 4 迁移友好：`src/Unomata.Core/*.cs` 是唯一会进 Unity 的目录，`tests/` 与 `console/` 永远留在外面
- 配置统一可继承：通用编译选项（`net8.0` / `nullable enable` / `implicit usings enable`）通过 `Directory.Build.props` 单点维护，避免三份 `.csproj` 漂移
- 解决方案文件可被 Rider / Visual Studio / `dotnet` CLI 三种入口正确识别

**Non-Goals:**

- 不实现任何 `CardData` / `HackSession` 等 Core 业务类（Phase 1）
- 不写任何真实测试用例（占位空测试套件 0 passed / 0 failed 即满足）
- 不接 CI（GitHub Actions 或类似）—— 暂不在本次范围
- 不引入代码风格工具（`.editorconfig` 之外的 analyzer / StyleCop / Roslynator）
- 不为 console 工程设计任何 CLI 参数解析框架，`Program.cs` 仅打印一行占位文本

## Decisions

### 1. 解决方案与项目布局

```
CardChainCore/
├─ CardChainCore.sln
├─ Directory.Build.props          ← 统一 net8.0 / Nullable / ImplicitUsings
├─ src/
│   └─ Unomata.Core/
│       └─ Unomata.Core.csproj   (类库)
├─ tests/
│   └─ Unomata.Core.Tests/
│       └─ Unomata.Core.Tests.csproj (xUnit)
└─ console/
    └─ Unomata.Core.Console/
        ├─ Unomata.Core.Console.csproj (Exe)
        └─ Program.cs
```

**理由**：`src` / `tests` / `console` 三段式是 .NET 开源项目主流布局（参考 dotnet/runtime、xunit/xunit 自身仓库）。Phase 4 迁移时只需 `Copy CardChainCore/src/Unomata.Core/*.cs Assets/_Project/Scripts/Core/`，路径单一无歧义。

**备选方案**：扁平结构（三个项目直接在 `CardChainCore/` 下平铺）。否决：解决方案根目录文件过多，且无法清晰表达"哪些目录会迁、哪些不会"的边界。

### 2. `Directory.Build.props` 统一公共配置

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

**理由**：三份 `.csproj` 共享同一组核心编译选项；任何一项升级（如未来切 `net9.0`）只改一处。`TreatWarningsAsErrors=true` 强制把 nullability 警告挡在编译期，与"严格 nullable"的决策对齐。

**备选方案**：每份 `.csproj` 内重复声明。否决：易漂移。

### 3. 项目类型与依赖

| 项目 | SDK | OutputType | 引用 | NuGet |
|---|---|---|---|---|
| `Unomata.Core` | `Microsoft.NET.Sdk` | `Library`（默认） | 无 | 无 |
| `Unomata.Core.Tests` | `Microsoft.NET.Sdk` | `Library`（默认，`IsPackable=false`） | `Unomata.Core` | xunit / xunit.runner.visualstudio / Microsoft.NET.Test.Sdk |
| `Unomata.Core.Console` | `Microsoft.NET.Sdk` | `Exe` | `Unomata.Core` | 无 |

**理由**：Core 项目零 NuGet 是一条硬约束——Phase 4 迁入 Unity 时若依赖任何 NuGet 包都会变成噩梦。Console 工程作为可执行入口，Phase 1 用来跑 `DEVELOPMENT_PLAN.md` 第 73 行的接龙 demo 输出。

**备选方案**：把 Console 合并进 Tests。否决：Console 是面向人的演示，Tests 是面向 CI/IDE 的断言，混在一起会污染测试输出。

### 4. xUnit 版本与测试 SDK 版本固定策略

不在 `.csproj` 中固定具体版本号，使用 `dotnet add package` 时的最新稳定版（截至 2026-05 为 xunit 2.x 稳定线）。版本由生成的 `.csproj` 自然落盘。

**理由**：当前是脚手架阶段，无历史包袱；后续若需要锁定版本，再引入 `Directory.Packages.props` 集中管理。

**备选方案**：立刻引入 Central Package Management。否决：过度工程，三个项目两个外部依赖不值得。

### 5. `Program.cs` 占位内容

```csharp
namespace Unomata.Core.Console;

internal static class Program
{
    private static void Main()
    {
        System.Console.WriteLine("[Unomata.Core.Console] scaffold ready. Phase 1 will populate this entrypoint.");
    }
}
```

**理由**：明确标记当前是脚手架；Phase 1 进来时一眼能看到"这里要被替换"。

### 6. `.gitignore` 范围

依赖现有仓库根 `.gitignore`，不在 `CardChainCore/` 下新建专用 `.gitignore`。需确认根 `.gitignore` 已涵盖 `bin/`、`obj/`、`*.user`、`.vs/`；若缺失则在 tasks 中补。

## Risks / Trade-offs

- **风险**：`TreatWarningsAsErrors=true` 可能在引入第三方测试包时遇到包内警告导致编译失败 → 缓解：xUnit 当前主流版本对 Nullable 友好；若真出现，可在 Tests 项目内局部 `<NoWarn>` 而不是全局放开
- **风险**：开发者本机未装 .NET 8 SDK → 缓解：`Docs/DEPENDENCIES.md` 已声明 ".NET 8"，本次 tasks 验收要求 `dotnet --list-sdks` 含 8.x；不再额外加 `global.json`（避免锁死 patch 版本）
- **权衡**：三段式目录比扁平结构多一层路径，但换来 Phase 4 迁移路径的零歧义，值得
- **权衡**：不引入 CI 意味着脚手架的"可构建性"只在本机自证；但本次是 Phase 0 收尾，CI 留给后续单独立项

## Migration Plan

不适用——纯新增，无迁移、无回滚。如需撤销，删除 `CardChainCore/` 下除 `.gitkeep` 外的全部内容即可恢复原状。
