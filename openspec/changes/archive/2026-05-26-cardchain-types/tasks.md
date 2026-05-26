## 1. 源码目录与文件骨架

- [x] 1.1 创建 `CardChainCore/src/Unomata.Core/CardChain/` 子目录
- [x] 1.2 在 `CardChain/` 下创建空文件 `CardType.cs` / `CardColor.cs` / `ChainDirection.cs` / `EndReason.cs` / `ComboType.cs` / `CardData.cs`，每个文件顶端写 `namespace Unomata.Core;` 文件作用域命名空间
- [x] 1.3 验证 `dotnet build CardChainCore.sln` 仍通过（空文件不应破坏现有脚手架）

## 2. 枚举定义

- [x] 2.1 在 `CardType.cs` 定义 `public enum CardType { Number, Reverse, Wild, Empty }`
- [x] 2.2 在 `CardColor.cs` 定义 `public enum CardColor { Red, Blue, Green, Yellow }`
- [x] 2.3 在 `ChainDirection.cs` 定义 `public enum ChainDirection { Ascending, Descending }`
- [x] 2.4 在 `EndReason.cs` 定义 `public enum EndReason { TimeUp, WrongCard, Surrender }`
- [x] 2.5 在 `ComboType.cs` 定义 `public enum ComboType { None, SameColorTwice, SameDirectionTwice }`
- [x] 2.6 所有枚举不显式标序号（D6 决策），不加 XML doc 之外的内容

## 3. CardData 类与 Empty 单例

- [x] 3.1 在 `CardData.cs` 定义 `public sealed class CardData`（D1 决策）
- [x] 3.2 添加三个公开字段：`CardType Type`、`CardColor? Color`、`int? Number`（D2 决策）
- [x] 3.3 添加 `public static readonly CardData Empty` 字段，初始化器设 `Type = CardType.Empty`、`Color = null`、`Number = null`（D3 决策）
- [x] 3.4 类内**不**定义任何方法（包括 `CanFollow`、`ToString` 重写等），只保留默认构造
- [x] 3.5 类与字段添加 XML doc summary，简述用途与字段语义约束

## 4. 测试目录与镜像结构

- [x] 4.1 创建 `CardChainCore/tests/Unomata.Core.Tests/CardChain/` 子目录
- [x] 4.2 在该目录下创建空测试文件：`CardTypeTests.cs` / `CardColorTests.cs` / `ChainDirectionTests.cs` / `EndReasonTests.cs` / `ComboTypeTests.cs` / `CardDataTests.cs`，命名空间统一 `Unomata.Core.Tests.CardChain`

## 5. 枚举测试

- [x] 5.1 `CardTypeTests`：用 `Enum.GetNames<CardType>()` 断言成员集合恰好为 `Number / Reverse / Wild / Empty`
- [x] 5.2 `CardColorTests`：断言成员集合恰好为 `Red / Blue / Green / Yellow`
- [x] 5.3 `ChainDirectionTests`：断言成员集合恰好为 `Ascending / Descending`
- [x] 5.4 `EndReasonTests`：断言成员集合恰好为 `TimeUp / WrongCard / Surrender`
- [x] 5.5 `EndReasonTests`：单独添加用例验证 `EndReason` SHALL NOT 包含 `Manual`（旧版命名废弃验证）
- [x] 5.6 `ComboTypeTests`：断言成员集合恰好为 `None / SameColorTwice / SameDirectionTwice`

## 6. CardData 测试

- [x] 6.1 `CardDataTests`：构造 `Number` 牌（Red-5），断言三个字段值正确
- [x] 6.2 `CardDataTests`：构造 `Reverse` 牌（Blue），断言 `Number` 为 `null`
- [x] 6.3 `CardDataTests`：构造 `Wild` 牌，断言 `Color` 与 `Number` 均为 `null`
- [x] 6.4 `CardDataTests`：用反射 `typeof(CardData).GetMethods()` 过滤 `DeclaredOnly | Public | Instance`，断言不含 `CanFollow`
- [x] 6.5 `CardDataTests`：读取 `CardData.Empty`，断言 `Type == Empty && Color == null && Number == null`
- [x] 6.6 `CardDataTests`：两次访问 `CardData.Empty`，断言 `ReferenceEquals` 为 `true`

## 7. 类型层零业务逻辑约束验证

- [x] 7.1 `CardChain/` 源文件人工 review，确认仅有枚举定义、字段声明、`Empty` 静态初始化器；无方法实现
- [x] 7.2 验证 `CardChainCore/src/Unomata.Core/Unomata.Core.csproj` 的 `<PackageReference>` 与 `<ProjectReference>` 列表与 Phase 0 归档时一致（零依赖）
- [x] 7.3 在源码目录下 grep `using UnityEngine`，断言无任何匹配

## 8. 构建与测试验收

- [x] 8.1 `dotnet build CardChainCore.sln` 退出码 0，无警告（`TreatWarningsAsErrors=true` 保证）
- [x] 8.2 `dotnet test CardChainCore.sln` 退出码 0，本 change 引入的所有测试通过、零失败、零跳过
- [x] 8.3 `dotnet run --project console/Unomata.Core.Console` 仍能输出占位文本（脚手架功能不被破坏）

## 9. 文档与归档准备

- [x] 9.1 更新 `Docs/TODO.md` 中 Change 1 的状态（实现完成后改为已完成，归档时再标日期）
- [x] 9.2 自检 `Docs/INTERFACE.md` 第二节"`CardData`"段与本 change 实现一致（应已一致，确认即可）
- [x] 9.3 准备 `/opsx:apply` 与 `/opsx:verify` 流程
