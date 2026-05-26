# DEPENDENCIES.md — 依赖清单

> 记录所有环境、包、资产依赖。两人开发环境需保持一致。

---

## 开发环境

| 项目 | 版本 |
|------|------|
| Unity Editor | 2022.3.x LTS（最新补丁版本） |
| 渲染管线 | Universal Render Pipeline (URP) |
| .NET（CardChainCore） | .NET 8 |
| IDE | Visual Studio 2022 / Rider（任选） |

---

## Unity Package Manager

| 包名 | 来源 | 用途 |
|------|------|------|
| QFramework | GitHub Release (unitypackage) | 项目架构框架，导入到 `Assets/QFramework/` |
| Cinemachine | Unity Registry | TPS相机系统 |
| Input System | Unity Registry | 新输入系统 |
| Animation Rigging | Unity Registry | 瞄准IK |
| Universal RP | Unity Registry | URP渲染管线 |

### QFramework 安装方式
QFramework **未发布到 OpenUPM**，必须手动安装：

1. 下载地址：https://github.com/liangxiegame/QFramework/releases
2. 选最新版（当前 `1.0.187-Unity2018Compatible`，向下兼容到 2018，在 Unity 2022.3 LTS 上可正常使用）
3. 下载 `.unitypackage` → Unity 中 `Assets → Import Package → Custom Package` 导入
4. **保留默认导入路径**，不要移动：
   - `Assets/QFramework/`     ← 框架本体
   - `Assets/QFrameworkData/` ← 框架运行时配置（ResKit/UIKit 等，路径写死，不可移动）
5. 若弹出 API Updater 提示，选 "I Made a Backup, Go Ahead!" 让 Unity 自动升级 API

> **说明**：QFramework 不放在 `Assets/ThirdParty/` 下，原因有二：
> 1. 官方教程/示例/菜单路径默认 `Assets/QFramework/`，保留默认便于对照学习与升级
> 2. `QFrameworkData/` 内部硬编码该路径，移动会导致配置丢失

---

## Asset Store 资产

| 资产名 | 用途 | 目标目录 | 状态 |
|--------|------|---------|------|
| CombatGirls - RifleCharacterPack | 玩家角色模型+动画 | `Assets/ThirdParty/CombatGirls/` | ✅ 已验证-方案B |
| Starter Assets - Third Person Controller | TPS控制器基础 | `Assets/ThirdParty/StarterAssets/` | ✅ 已验证-方案B |
| MagicaCloth2 | CombatGirls 布料物理依赖 | `Assets/ThirdParty/MagicaCloth2/` | ✅ 已导入 |
| 怪物模型+动画（待填写） | 敌人 | `Assets/ThirdParty/Monsters/` | 待填写 |
| 场景/地图（待填写） | 竞技场场景 | `Assets/ThirdParty/Environment/` | 待填写 |
| 科幻VFX特效包（待填写） | 命中/受击/骇入特效 | `Assets/ThirdParty/VFX/` | 待填写 |

---

## CardChainCore（独立.NET项目）

Core 层采用**独立 .NET 8 控制台工程**方案：开发期在 `CardChainCore/` 内迭代，Phase 4 时一次性将 `src/Unomata.Core/*.cs` 复制到 `Assets/_Project/Scripts/Core/` 并配 `Unomata.Core.asmdef`（`noEngineReferences=true`）。`tests/` 与 `console/` 不迁入 Unity。

### 运行时依赖

| 依赖 | 说明 |
|------|------|
| 无第三方 NuGet 包 | 运行时保持零外部依赖，纯 C# 标准库 |

### 开发期依赖（仅 tests/console 工程）

| 依赖 | 说明 |
|------|------|
| xUnit | 单元测试框架 |
| xunit.runner.visualstudio | IDE / `dotnet test` 运行器 |
| Microsoft.NET.Test.Sdk | .NET 测试 SDK |

> 不引入 FluentAssertions、Moq 等额外测试库：Core 为纯逻辑无外部依赖，xUnit 原生断言已足够。

---

## QFramework 实测兼容性记录

| 项目 | 结果 |
|------|------|
| 验证日期 | 2026-05-25 |
| Unity 版本 | 2022.3.62f1 LTS |
| QFramework 版本 | 1.0.187-Unity2018Compatible |
| 编译错误 | ✅ 零红色错误 |
| QFramework 菜单 | ✅ 正常出现 |
| `using QFramework;` 编译 | ✅ 通过 |
| `GameApp : Architecture<GameApp>` 初始化 | ✅ Play Mode 正常运行 |
| Command → System → Event 链路 | ✅ 全链路验证通过 |
| API Updater | 无弹窗；ResKit 有3条 CS0618 警告（UnityWebRequest.isNetworkError 废弃 API），不影响框架可用性 |
| **总结** | **完全可用**，可按 ARCHITECTURE.md 规划开始 Phase 1/2 开发 |
