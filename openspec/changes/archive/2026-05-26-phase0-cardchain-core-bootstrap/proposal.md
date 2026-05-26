## Why

队友昨天完成 Unity 端 Phase 0（资产整理 / QFramework 验证 / 角色控制器方案 B 验证）后，A 端 Phase 0 仅剩"搭 `CardChainCore` 控制台项目"一项未完成。该工程是 Phase 1 Core 层开发与 TDD 验收的载体，必须在写任何接龙业务逻辑之前先把空脚手架立起来，作为 Phase 0 的最终收尾。

## What Changes

- 在 `CardChainCore/` 下创建 `CardChainCore.sln`，包含三个项目：
  - `src/Unomata.Core/Unomata.Core.csproj`：纯 .NET 8 类库，零第三方依赖，命名空间 `Unomata.Core`
  - `tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj`：xUnit 测试工程（xUnit + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk）
  - `console/Unomata.Core.Console/Unomata.Core.Console.csproj`：用于 Phase 1 验收输出的控制台 host
- 三个 `.csproj` 统一使用 `net8.0` / `<Nullable>enable</Nullable>` / `<ImplicitUsings>enable</ImplicitUsings>`
- 不写任何业务代码，仅保证 `dotnet build` / `dotnet test` / `dotnet run` 三条命令在干净 clone 下均可成功
- 删除 `CardChainCore/.gitkeep`（脚手架建立后不再需要）

## Capabilities

### New Capabilities

- `cardchain-core-scaffold`：定义 Core 层独立 .NET 8 工程的骨架结构（解决方案、项目分布、目标框架、零业务代码的可构建可测试基线）

### Modified Capabilities

<!-- 无现有 spec 涉及 Core 工程结构 -->

## Impact

- 新增目录：`CardChainCore/CardChainCore.sln` 及 `src/`、`tests/`、`console/` 三个子目录
- 删除文件：`CardChainCore/.gitkeep`
- 文档无需变更：`Docs/DEVELOPMENT_PLAN.md`、`Docs/DEPENDENCIES.md`、`Docs/ARCHITECTURE.md` 已在上一轮决策中同步更新
- 不影响 Unity 工程、不影响队友 B 的 Phase 2 工作
- 引入开发期 NuGet 依赖：xunit / xunit.runner.visualstudio / Microsoft.NET.Test.Sdk（仅 tests 工程）
