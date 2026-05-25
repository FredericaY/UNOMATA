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
| QFramework | OpenUPM / 官网导入 | 项目架构框架 |
| Cinemachine | Unity Registry | TPS相机系统 |
| Input System | Unity Registry | 新输入系统 |
| Animation Rigging | Unity Registry | 瞄准IK |
| Universal RP | Unity Registry | URP渲染管线 |

### QFramework 安装方式
1. 下载地址：https://github.com/liangxiegame/QFramework
2. 或通过 OpenUPM：`openupm add com.liangxiegame.qframework`
3. 导入后放置于 `Assets/ThirdParty/QFramework/`

---

## Asset Store 资产

| 资产名 | 用途 | 目标目录 |
|--------|------|---------|
| CombatGirls - RifleCharacterPack | 玩家角色模型+动画 | `Assets/ThirdParty/CombatGirls/` |
| Starter Assets - Third Person Controller | TPS控制器基础 | `Assets/ThirdParty/StarterAssets/` |
| 怪物模型+动画（待填写） | 敌人 | `Assets/ThirdParty/Monsters/` |
| 场景/地图（待填写） | 竞技场场景 | `Assets/ThirdParty/Environment/` |
| 科幻VFX特效包（待填写） | 命中/受击/骇入特效 | `Assets/ThirdParty/VFX/` |

---

## CardChainCore（独立.NET项目）

| 依赖 | 说明 |
|------|------|
| 无第三方NuGet包 | 纯C#标准库，保持零外部依赖 |
| NUnit（可选） | 单元测试，按需添加 |
