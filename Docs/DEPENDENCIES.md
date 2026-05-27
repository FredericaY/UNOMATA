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

### Package Registry 配置（国内镜像源）

**决策日期**：2026-05-27
**适用范围**：两人开发团队均位于国内
**生效方式**：`Packages/manifest.json` 的 `scopedRegistries` 字段

#### 为什么走镜像

Unity 官方 registry（`packages.unity.com` / `download.packages.unity.com`）国内访问普遍 < 100 KB/s，首次拉取 19 个包（≈ 680 MB）耗时数十分钟到数小时。Unity 中国官方维护 `packages.unity.cn` 完整同步官方包，国内访问稳定 5~20 MB/s。

**关键认知**：Unity Package Manager **不支持 registry 自动 fallback**——一个 scope 只会路由到一个 registry。所以无法"两个源都留着自动切"，必须二选一作为主源。

#### manifest.json 配置

`Packages/manifest.json` 顶层加 `scopedRegistries` 字段：

```json
{
  "scopedRegistries": [
    {
      "name": "Unity China",
      "url": "https://packages.unity.cn",
      "scopes": [
        "com.unity"
      ]
    }
  ],
  "dependencies": {
    "...": "..."
  }
}
```

**scope 解析规则**：
- `com.unity` 是**前缀匹配**，覆盖所有 `com.unity.*` 包（含 `com.unity.cinemachine` / `com.unity.inputsystem` 等）
- 内置模块 `com.unity.modules.*`（如 `modules.animation`）不走 registry，引擎自带，不受影响
- 第三方包 `com.coplaydev.unity-mcp`（GitHub git URL）不走 registry，直接 git clone，不受影响

#### 镜像完整度

`packages.unity.cn` 与 `packages.unity.com` **完整双向同步**所有 `com.unity.*` 命名空间包。本项目所需的全部 18 个 `com.unity.*` 包均可拉到。

#### 应急回退（镜像不可用时）

若 `packages.unity.cn` 临时挂掉，**临时**改 manifest 把 `scopedRegistries` 数组置空 `[]` 或整段删除，Unity 会回退到默认官方 registry。**用完改回**，不要把空配置提交到主分支。

更稳妥的做法：本地新建 `Packages/manifest.local.json` 备份，临时切换时改 `manifest.json`，应急完毕后从 `manifest.local.json` 恢复。

#### 验证走的是镜像

打开 Unity → `Window → Package Manager` → 任选一个 `com.unity.*` 包 → 右侧 Details 面板的 `View documentation` 链接如果指向 `docs.unity.cn`（而不是 `docs.unity3d.com`），说明配置生效。

或命令行检查：

```powershell
# 看 Unity 缓存的 _resolved.json，能看到每个包的 actual url
Get-Content Packages\packages-lock.json | Select-String "packages.unity"
```

应看到 `packages.unity.cn` 路径。

#### 异地协作注意

若**未来有队员到境外**（如出差、留学）：
- 短期：本地通过环境变量 `HTTPS_PROXY` 走代理，manifest 不动
- 长期：评估是否需要拆分配置（不推荐——Unity 不支持 manifest 多 profile）

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
