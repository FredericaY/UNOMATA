# 开发环境与工作入口

> 当前状态：SDK 8.0.425、Unity MCP 与编译环境已恢复。输入基线修复已实施并通过 150 个自动断言，用户已确认恢复阶段验收完成，已归档，见 [INPUT_BASELINE.md](INPUT_BASELINE.md)。首次恢复证据见 [ENVIRONMENT_RECOVERY.md](ENVIRONMENT_RECOVERY.md)。

## 首次恢复

1. 读 [重启回顾](PROJECT_REVIEW.md)、[近期计划](TODO.md)、[架构](ARCHITECTURE.md)；玩法和 Core 契约分别见 GAME_DESIGN / INTERFACE。
2. 在 Unity Hub 中使用 **2022.3.62f3** 打开仓库根目录，不跟随“最新版本”升级。打开 `Assets/_Project/Scenes/SampleScene.unity`，等待导入与编译结束。
3. 本机已安装 **.NET SDK 8.0.425**。新终端中确认 `dotnet --list-sdks` 能列出 8.x；仅安装运行时不能执行 build/test。若当前终端尚未刷新用户 PATH，可执行 `$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"`。
4. 在仓库根目录执行下方命令，记录实际结果。

```powershell
dotnet --list-sdks
dotnet restore CardChainCore/CardChainCore.sln
dotnet build CardChainCore/CardChainCore.sln --no-restore
dotnet test CardChainCore/CardChainCore.sln --no-build
dotnet run --project CardChainCore/console/Unomata.Core.Console
```

Console 当前应输出 scaffold 提示；完整骇入演示尚未实现。2026-09-17 环境恢复时已实际重跑 139 项测试并通过；后续运行以新产生的结果为准。依赖版本与资产遗留见 [DEPENDENCIES.md](DEPENDENCIES.md)。

可运行仓库检查助手：

```powershell
powershell -NoProfile -File Tools/Check-Project.ps1
powershell -NoProfile -File Tools/Check-Project.ps1 -RunCoreTests -ValidateSpecs
```

默认检查环境、关键路径和文档链接；开关分别运行 Core 测试与 OpenSpec 严格校验。缺 SDK 或校验失败会非零退出，明确报出阻塞项，不代替 Unity 验收。

## Codex 与 OpenSpec

本项目后续统一使用 Codex。根目录 `AGENTS.md` 是自动加载入口，`agent.md` 是人工索引，均连接唯一的 `rules.md`。旧客户端的规则、skills 和工作流已移除。规则不记录进度；每次新任务从磁盘读取。

按 [Codex 项目指令文档](https://developers.openai.com/ko-KR/docs/agent-configuration/agents-md) 使用仓库入口；skills 使用项目级 `.agents/skills/`。这两个个人入口与新 skills 在本地保留，换机须恢复个人文件；共享文档和代码保留在 Git，阅读项目不需要安装 AI 工具。

当前已用 OpenSpec CLI `1.12.0` 为 Codex 生成 6 个技能：propose、explore、apply-change、update-change、sync-specs、archive-change。需要安装 CLI 时使用项目当前验证的版本，不自动追最新版：

```powershell
npm install -g @fission-ai/openspec@1.12.0
openspec.cmd init --tools codex
openspec.cmd list
openspec.cmd status --change aim-ik-rig-constraint --json
openspec.cmd validate --all --strict --no-interactive
```

`status` 的 planning artifacts 完成（包括其 `isComplete` 字段）仅表示方案文件齐全；实现进度用 `list` / tasks 查看，完成验收另看实际证据。当前 IK 的任务为 22/37，仍在进行中。

已有 config 时用其中的 `context` 指定中文，不通过 `init --language` 覆盖现有配置。后续生成文件用 `openspec.cmd update` 刷新并核对差异。已有 change 不重新初始化或归档；按需调用 `$openspec-explore` 等入口，并先读取对应 SKILL.md。

只维护 `.agents/skills/` 中的 Codex OpenSpec 工作流。原有 OpenSpec 已纳入 Git，因此继续跟踪；其规格和历史归档不受客户端清理影响。

## Unity 基线验收

连接 Unity MCP 后先核对 `projectRoot` 为当前仓库，确认非编译/导入中、活动场景正确。无 MCP 时应报告连接限制，不能声称已读取实时 Console 或完成 Play Mode。

第一次恢复应记录：编译与警告、场景是否有 Missing Script、角色渲染/布料、移动/跳跃/冲刺、脚步/落地音、瞄准相机、输入禁用/重新启用、静止和移动瞄准。专项步骤见 [动画笔记](AboutTheAnimation.md) 的重启节。

Build Settings 的旧路径须在 Editor 中核对和保存后再构建。场景改动使用 Editor / MCP 保存，资产移动使用 AssetDatabase 保持 GUID。

## 2026-09-17 恢复后的环境

- 工程使用 Unity `2022.3.62f3`。本机 MCP 服务为 `http://127.0.0.1:8080/mcp`，已实测连接正确工程，完成编译与场景检查。
- Codex 项目配置位于本地 `.codex/config.toml`，只保存本机 MCP 地址和超时，无凭据；与用户级配置分离。后续重新加载项目配置后检查原生 MCP 工具是否出现。本轮已经通过同一服务的标准 MCP 协议完成实际操作。
- .NET SDK `8.0.425` 安装于 `$env:LOCALAPPDATA\Microsoft\dotnet`，已加入用户 PATH，系统 PATH 未改变。现有终端可以临时刷新 PATH；检查助手也能直接发现此目录。
- Core restore/build/test 已实际完成，139 项通过；Console 输出现有 scaffold 提示。
- Node.js / OpenSpec 已可用，检查助手支持 Windows 默认安装位置与显式命令路径。
- SampleScene 现留在非 Play Mode、无未保存修改。首次运行故障见恢复记录；Audio 缺失脚本残留已清理并通过两次运行复验，详见 INPUT_BASELINE.md。恢复阶段已获用户整体确认；未新增逐项物理输入日志，瞄准偏差后续另建 change。

## 开发规范核对（2026-09-18）

已重新读取 VG2GroupProject 当前 AGENTS/rules（参照规则文件最后更新时间为 2026-09-09），逐项核对并补齐 UNOMATA 的现行标准：

| 范围 | 本项目现行要求 |
|---|---|
| QFramework | 唯一 GameApp 组合根，Model/Utility/System 依赖顺序，Command/Query/Event 分工；禁止表现直接写业务 Model、场景组件互调拼业务、运行态回写配置及运行程序集依赖 Editor/测试；订阅成对清理 |
| Unity MCP | 动态发现资源与工具、确认实例及 projectRoot、检查就绪/编译/导入、操作前后 Console；编辑后保存并核对实际结果，断连有限重试，不能把请求返回当作验收 |
| 文档同步 | Docs 职责明确、事实/计划/历史结果分开；功能 change 先规划、真实验证后勾选、归档前同步三个相关主规格与受影响文档；规则只放长期纪律 |
| Codex harness | AGENTS 自动入口与 agent 索引连接唯一 rules，6 个生成的 OpenSpec skills，项目本地 MCP 配置；旧客户端规则与工作流已移除 |

保留的项目差异：Unity 为 2022.3.62f3、业务入口为 GameApp.Interface、Core 独立使用 .NET 8；不复制 VG2 的课程版本、WebGL 专项要求或其专用生命周期 API。本仓库原有 OpenSpec 已跟踪，因此继续保存规格和归档历史；个人 rules/AGENTS/skills/MCP 仍本地忽略。

规则文件不保存这份核对流水；该记录在共享文档中。规范已对齐不表示所有历史功能已完成，已知瞄准偏差继续作为后续独立工作。
