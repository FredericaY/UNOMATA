# 环境恢复与首次运行记录

> 后续更新（2026-09-18）：输入动作与生命周期修复已实施，150 个自动回归断言通过；恢复阶段已获用户整体确认，Audio 残留已清理并通过两次独立运行/音频复验，当前输入恢复 change 已归档，见 [INPUT_BASELINE.md](INPUT_BASELINE.md)。本文保留 2026-09-17 的实际故障记录。

> 日期：2026-09-17（America/Chicago）。范围：Codex 开发入口、工具链恢复、编译与首次 Play Mode 检查。没有修改玩法 C#、输入资产、场景、Prefab 或包版本。

## 已恢复的环境

| 项目 | 实际结果 |
|---|---|
| 开发客户端 | 统一使用 Codex；旧客户端规则、skills 和工作流目录已删除 |
| 规则入口 | AGENTS.md 自动加载规则，agent.md 作人工索引；均指向唯一 rules.md |
| OpenSpec | CLI 1.12.0，6 个 Codex skills；严格校验 13/13 通过 |
| Unity | 2022.3.62f3；通过 MCP 确认 projectRoot 为 UNOMATA，编辑器就绪 |
| MCP | 本机 Streamable HTTP 服务连接成功，完成资源读取、场景操作与代码诊断；项目级配置保存到本地 .codex/config.toml |
| .NET | 微软签名安装脚本安装 SDK **8.0.425** 到当前用户的 LocalAppData/Microsoft/dotnet，已加入用户 PATH；系统 PATH 未改 |
| Core 依赖与构建 | 3 个项目 restore / build 成功，**0 warning、0 error** |
| Core 测试 | **139 passed、0 failed、0 skipped**；本次真实重跑 |
| Console | 输出 scaffold ready；符合当前占位实现，不是完整骇入演示 |
| 检查助手 | 能发现用户目录 SDK，文档文件链接与规格检查通过，退出码 0 |

安装采用 [微软官方 dotnet-install](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script) 的用户目录安装方式。项目级 MCP 配置遵循 [Codex MCP 文档](https://developers.openai.com/zh-Hans/docs/extend/mcp)，配置不包含凭据。当前任务的原生工具列表未动态加载该服务，本轮通过同一服务的标准 MCP 协议完成操作；后续客户端重新加载项目配置后核对原生连接。

## Unity 检查结果

1. 初始是未修改的空场景，确认 isDirty=false 后加载项目 SampleScene。
2. 场景验证报告：**0 Missing Script、0 Broken Prefab、0 自动修复**。
3. 请求脚本重新编译并等待 Domain Reload 后重新连接；没有编译错误。期间出现一次 MCP WebSocket 初始化警告，连接已恢复。
4. Build Settings 的实际 Editor API 返回正确场景路径 `Assets/_Project/Scenes/SampleScene.unity`、原 GUID、enabled=true、index=0。上轮静态文件中的旧路径已被 Unity 通过 GUID 解析，不再列为当前构建路径阻塞；未手改配置。
5. Play Mode 中 QFrameworkValidator 输出基础 Command → System → Event 与 Phase2 System/Model 链路通过。
6. Play Mode 的角色启动检查失败，详见下节。移动、跳跃、相机切换、音频听感和 IK 手感 **不计为通过**。
7. 检查结束后退出 Play Mode；SampleScene 保持打开，isDirty=false，未保存测试状态到资产。

## 首次运行发现的阻塞

### R2-01：输入资产缺少 Aim / Fire

实际加载的 `Assets/_Project/Settings/UnomataPlayer.inputactions` 以及运行时动作列表都只有：

```text
Player/Move
Player/Look
Player/Jump
Player/Sprint
```

`PlayerController.Start()` 在第 31–32 行查找 Aim / Fire；第 **40 行**对空的 Aim Action 订阅时抛出 NullReferenceException。退出 Play Mode 时，第 **55 行**退订同一个空动作，再次抛出 NullReferenceException。

这不是 SDK 或 Unity 版本问题，是已有源码与序列化输入资产不一致。虽然旧输入桥接已归档，实际保存到仓库的资产没有满足代码依赖。应先在新的输入恢复变更中补齐并持久化已约定动作/绑定，覆盖缺失配置与禁用/重新启用路径，再继续瞄准验收；不要改写旧归档来宣称当时通过。

### R2-02：进入 Play Mode 的缺失脚本日志

Console 出现：`The referenced script (Unknown) on this Behaviour is missing!`，没有来源路径或调用栈。

静态场景验证、运行时场景对象扫描和退出后 **575 个已加载 GameObject** 扫描均未发现可定位的缺失组件。来源目前 **未确定**，可能涉及启动时加载的对象；不能凭该日志删除场景或第三方组件。下一步在单独复现中关联加载时点、实例与资产来源。

### 其他验收限制

本轮多次读取的 Time.frameCount 为 1，EditorApplication.isPaused=false。尚未定位其与运行异常/编辑器刷新之间的关系，因此不据此宣称角色循环、持续输入或动画运行正常。已保留本地 Console 和截图记录；截图不构成手感验收。

## 下一步顺序

1. 输入恢复：补齐持久化 Aim / Fire，验证按下/松开、禁用/重新启用、残留输入和 Look 路由；先建立对应变更。
2. 复现并定位启动时缺失脚本报错，确认运行持续推进且 Console 无未解释错误。
3. 重跑 R2 的移动、跳跃、冲刺、相机、音频与输入生命周期基线。
4. 再继续活动 IK 变更的枪口方向、俯仰与死区联动验收。IK 本轮保持 22/37，未归档。

近期待办见 [TODO.md](TODO.md)。首次回顾见 [PROJECT_REVIEW.md](PROJECT_REVIEW.md)，本记录补充其历史环境观察。
