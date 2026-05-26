## 1. 准备工作

- [x] 1.1 验证本机已安装 .NET 8 SDK：执行 `dotnet --list-sdks` 应输出至少一个 8.x 版本
- [x] 1.2 删除 `CardChainCore/.gitkeep`

## 2. 解决方案与公共配置

- [x] 2.1 在 `CardChainCore/` 下执行 `dotnet new sln -n CardChainCore`，生成 `CardChainCore.sln`
- [x] 2.2 在 `CardChainCore/Directory.Build.props` 写入公共编译属性（`TargetFramework=net8.0`、`LangVersion=latest`、`Nullable=enable`、`ImplicitUsings=enable`、`TreatWarningsAsErrors=true`）

## 3. Core 类库项目

- [x] 3.1 执行 `dotnet new classlib -o src/Unomata.Core -n Unomata.Core --framework net8.0`
- [x] 3.2 删除 `dotnet new classlib` 默认生成的 `Class1.cs`
- [x] 3.3 编辑 `src/Unomata.Core/Unomata.Core.csproj`：移除 `<TargetFramework>`、`<Nullable>`、`<ImplicitUsings>` 等已被 `Directory.Build.props` 提供的属性，使其只保留 `<Project Sdk="Microsoft.NET.Sdk">` 与空 `<PropertyGroup>` 或精简到无重复声明
- [x] 3.4 执行 `dotnet sln add src/Unomata.Core/Unomata.Core.csproj`

## 4. 测试项目

- [x] 4.1 执行 `dotnet new xunit -o tests/Unomata.Core.Tests -n Unomata.Core.Tests --framework net8.0`
- [x] 4.2 删除模板自动生成的 `UnitTest1.cs`（保持 tests 目录无任何测试用例，验证空套件可跑）
- [x] 4.3 编辑 `tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj`：移除被 `Directory.Build.props` 覆盖的属性；确认含 `<IsPackable>false</IsPackable>`；确认 `<PackageReference>` 列表恰为 `xunit`、`xunit.runner.visualstudio`、`Microsoft.NET.Test.Sdk` 三项
- [x] 4.4 执行 `dotnet add tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj reference src/Unomata.Core/Unomata.Core.csproj`
- [x] 4.5 执行 `dotnet sln add tests/Unomata.Core.Tests/Unomata.Core.Tests.csproj`

## 5. 控制台项目

- [x] 5.1 执行 `dotnet new console -o console/Unomata.Core.Console -n Unomata.Core.Console --framework net8.0`
- [x] 5.2 编辑 `console/Unomata.Core.Console/Unomata.Core.Console.csproj`：移除被 `Directory.Build.props` 覆盖的属性；保留 `<OutputType>Exe</OutputType>`
- [x] 5.3 用以下内容覆盖 `console/Unomata.Core.Console/Program.cs`，命名空间设为 `Unomata.Core.Console`，含一个打印占位文本 `[Unomata.Core.Console] scaffold ready. Phase 1 will populate this entrypoint.` 的 `Main` 方法
- [x] 5.4 执行 `dotnet add console/Unomata.Core.Console/Unomata.Core.Console.csproj reference src/Unomata.Core/Unomata.Core.csproj`
- [x] 5.5 执行 `dotnet sln add console/Unomata.Core.Console/Unomata.Core.Console.csproj`

## 6. 验收

- [x] 6.1 在 `CardChainCore/` 下执行 `dotnet sln list`，确认输出恰好列出三个项目路径
- [x] 6.2 在 `CardChainCore/` 下执行 `dotnet build CardChainCore.sln`：退出码 0，三个项目均报告 Build succeeded，零警告
- [x] 6.3 在 `CardChainCore/` 下执行 `dotnet test CardChainCore.sln`：退出码 0（零通过零失败属于成功）
- [x] 6.4 在 `CardChainCore/` 下执行 `dotnet run --project console/Unomata.Core.Console`：退出码 0，标准输出包含步骤 5.3 中定义的占位文本
- [x] 6.5 在 `CardChainCore/src/Unomata.Core/` 下递归搜索 `using UnityEngine`：确认无任何匹配（应当如此，本步仅作回归保险）

## 7. 提交准备

- [x] 7.1 确认仓库根 `.gitignore` 已涵盖 `bin/`、`obj/`、`*.user`、`.vs/`；如缺失则补充
- [x] 7.2 确认 `CardChainCore/` 下 `bin/` 和 `obj/` 未被纳入版本控制（`git status --ignored CardChainCore/`）
- [x] 7.3 阅读 `git status` 列出的待加文件清单，与本任务列表对齐后再提交
