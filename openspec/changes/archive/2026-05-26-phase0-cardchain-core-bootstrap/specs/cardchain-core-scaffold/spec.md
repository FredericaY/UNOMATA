## ADDED Requirements

### Requirement: 解决方案与项目布局

`CardChainCore/` 目录 SHALL 包含一个 `CardChainCore.sln` 解决方案文件，并以 `src/` / `tests/` / `console/` 三段式组织三个项目：`src/Unomata.Core/`、`tests/Unomata.Core.Tests/`、`console/Unomata.Core.Console/`。`.gitkeep` 在脚手架建立后 SHALL 被删除。

#### Scenario: 解决方案文件存在且包含三个项目
- **WHEN** 在 `CardChainCore/` 目录下执行 `dotnet sln list`
- **THEN** 输出 SHALL 列出且仅列出 `src/Unomata.Core/Unomata.Core.csproj`、`tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj`、`console/Unomata.Core.Console/Unomata.Core.Console.csproj` 三个项目

#### Scenario: 历史占位文件已清理
- **WHEN** 检查 `CardChainCore/` 目录
- **THEN** 该目录下 SHALL NOT 存在 `.gitkeep` 文件

### Requirement: 统一编译配置

`CardChainCore/Directory.Build.props` SHALL 集中声明所有项目共享的核心编译选项，包括 `TargetFramework=net8.0`、`Nullable=enable`、`ImplicitUsings=enable`、`LangVersion=latest`、`TreatWarningsAsErrors=true`。三个 `.csproj` SHALL NOT 重复声明这些属性。

#### Scenario: Directory.Build.props 包含约定属性
- **WHEN** 读取 `CardChainCore/Directory.Build.props`
- **THEN** 文件 SHALL 至少包含 `<TargetFramework>net8.0</TargetFramework>`、`<Nullable>enable</Nullable>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` 四个属性

#### Scenario: csproj 不重复声明公共属性
- **WHEN** 检查三个 `.csproj` 文件
- **THEN** 任一 `.csproj` SHALL NOT 显式声明 `<TargetFramework>`、`<Nullable>`、`<ImplicitUsings>` 这三个已由 props 提供的属性

### Requirement: Core 项目零运行时第三方依赖

`src/Unomata.Core/Unomata.Core.csproj` SHALL 是默认 SDK 类库（隐式 `OutputType=Library`），SHALL NOT 引用任何 NuGet 包，SHALL NOT 引用同解决方案内的任何其它项目，命名空间根 SHALL 为 `Unomata.Core`。

#### Scenario: Core 项目无 PackageReference
- **WHEN** 读取 `src/Unomata.Core/Unomata.Core.csproj`
- **THEN** 文件 SHALL NOT 包含任何 `<PackageReference>` 元素

#### Scenario: Core 项目无 ProjectReference
- **WHEN** 读取 `src/Unomata.Core/Unomata.Core.csproj`
- **THEN** 文件 SHALL NOT 包含任何 `<ProjectReference>` 元素

### Requirement: 测试项目使用 xUnit 且仅引用 Core

`tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj` SHALL 引用 `Unomata.Core` 项目，SHALL 通过 `<PackageReference>` 引入 `xunit`、`xunit.runner.visualstudio`、`Microsoft.NET.Test.Sdk` 三个包，且 SHALL NOT 引入其它第三方测试库（FluentAssertions、Moq 等）。`IsPackable` SHALL 为 `false`。

#### Scenario: 测试项目恰好引用三个测试包
- **WHEN** 读取 `tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj` 的 `<PackageReference>` 列表
- **THEN** 列表 SHALL 恰好包含 `xunit`、`xunit.runner.visualstudio`、`Microsoft.NET.Test.Sdk` 三个包名（顺序不限）

#### Scenario: 测试项目引用 Core
- **WHEN** 读取 `tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj`
- **THEN** 文件 SHALL 包含一个指向 `../../src/Unomata.Core/Unomata.Core.csproj` 的 `<ProjectReference>`

### Requirement: Console 项目作为可执行入口

`console/Unomata.Core.Console/Unomata.Core.Console.csproj` SHALL 设置 `<OutputType>Exe</OutputType>`，SHALL 引用 `Unomata.Core` 项目，SHALL NOT 引用任何 NuGet 包。`Program.cs` SHALL 包含一个最小 `Main` 方法用于占位，且未来 Phase 1 将在此扩展接龙 demo 输出。

#### Scenario: Console 是可执行项目
- **WHEN** 读取 `console/Unomata.Core.Console/Unomata.Core.Console.csproj`
- **THEN** 文件 SHALL 包含 `<OutputType>Exe</OutputType>`

#### Scenario: Console 引用 Core
- **WHEN** 读取 `console/Unomata.Core.Console/Unomata.Core.Console.csproj`
- **THEN** 文件 SHALL 包含一个指向 `../../src/Unomata.Core/Unomata.Core.csproj` 的 `<ProjectReference>`

### Requirement: 脚手架可构建可测试可运行

整个脚手架在干净 clone 后 SHALL 在不修改任何代码的前提下通过三条标准命令验收：`dotnet build` 全部成功（零警告，因 `TreatWarningsAsErrors=true`）、`dotnet test` 退出码 0（即使零测试用例）、`dotnet run --project console/Unomata.Core.Console` 输出非空文本。

#### Scenario: 解决方案构建成功
- **WHEN** 在 `CardChainCore/` 下执行 `dotnet build CardChainCore.sln`
- **THEN** 退出码 SHALL 为 0，且输出 SHALL 报告 3 个项目全部 Build succeeded

#### Scenario: 测试套件可运行
- **WHEN** 在 `CardChainCore/` 下执行 `dotnet test CardChainCore.sln`
- **THEN** 退出码 SHALL 为 0（零测试通过零测试失败属于成功）

#### Scenario: Console 可运行
- **WHEN** 在 `CardChainCore/` 下执行 `dotnet run --project console/Unomata.Core.Console`
- **THEN** 退出码 SHALL 为 0，且标准输出 SHALL 包含至少一行非空文本

### Requirement: Core 严禁引用 UnityEngine

`src/Unomata.Core/` 下的所有 `.cs` 源文件 SHALL NOT 出现 `UnityEngine` 命名空间引用，且 `Unomata.Core.csproj` SHALL NOT 引用任何 Unity 相关 DLL 或包。该约束保证 Phase 4 迁移到 Unity 内 `Assembly Definition`（`noEngineReferences=true`）时不会发生符号缺失。

#### Scenario: 源码无 UnityEngine 引用
- **WHEN** 在 `src/Unomata.Core/` 递归搜索 `using UnityEngine` 文本
- **THEN** SHALL 无任何匹配结果
