# UNOMATA — 重启回顾

> 核对日期：2026-09-17（本地时间）。基线：`main`，本地 HEAD `b501cde`（2026-06-01）。本次未查询远端最新状态。
> 范围：首次回顾为仓库阅读与整理。随后用户授权环境恢复，.NET/MCP 已恢复，Core 139 项测试通过；首次 Unity 运行暴露输入资产缺陷，最新状态见 [ENVIRONMENT_RECOVERY.md](ENVIRONMENT_RECOVERY.md)。下文保留首次盘点的历史观察。

> 后续状态（2026-09-18）：输入恢复与 [移动/越肩瞄准修复](../openspec/changes/archive/2026-09-18-fix-over-shoulder-aim-locomotion/proposal.md) 均已获用户验收；新修复 47/47 完成，正式规格已同步归档。下方仍保留首次盘点原貌，当前进度以 [TODO](TODO.md) 与 [动画验收记录](AboutTheAnimation.md) 为准。

> 后续状态（2026-09-19）：[瞄准射击与胶囊受击](../openspec/changes/archive/2026-09-19-unity-shooting-damage-loop/proposal.md) 已获用户最终验收，33/33 项完成并同步五份正式规格归档。射击/受击、素材反馈与轻微后坐已实现，详细证据见 [射击基线](SHOOTING_BASELINE.md)。下一个 change 接入敌人模型、骨架、动画及简单 UI；AI/波次与完整骇入仍未实现。下方首次盘点保持历史原貌。

## 结论

项目仍处于基础原型阶段，尚未形成“射击 + 接龙骇入”的完整循环。已有资产整合、角色移动和瞄准框架、QFramework 输入/音频接入，以及独立 Core 的卡牌类型、规则和发牌器。

**搁置主因已由用户本轮确认：人物瞄准动作和枪口朝向始终做不好。** 这与最后一个活动变更及其调试记录一致。不能把最后提交标题中的“瞄准上半身 IK”理解为已通过验收。

## 实现盘点

| 模块 | 当前事实 | 证据与限制 |
|---|---|---|
| 工程与资产 | Unity `2022.3.62f3`、URP `14.0.12`；第三方分类目录及 5 个 Sandbox 场景存在 | 工程配置、`Assets/_Project/Scenes/Sandbox/`；历史验证不等于本轮通过 |
| Core A1–A3 | 类型、`SessionState`、`CardChainRules`、`HackDifficultyConfig`、`OptionGenerator` 已实现 | `CardChainCore/src/Unomata.Core/CardChain/`；历史 A3 记录为 139 项测试通过 |
| Core A4–A7 | `HackSession`、`HackResult`、计时/结束/奖励池尚未实现；Console 只输出 scaffold 文本 | 源码清单、`console/Unomata.Core.Console/Program.cs` |
| QFramework | 注册 4 个 Model、3 个 System；玩家扣血方法存在 | `GameApp.cs`、`PlayerSystem.cs`；存在方法不代表完整受伤链路 |
| 角色移动、模型、基础动画 | RifleGirl + StarterAssets + 布料及基础动画已整合 | B1a/B1b.1 历史归档；跳跃手感遗留见动画笔记 |
| 瞄准层与相机 | UpperBodyAim、双虚拟相机及桥接脚本存在 | B1b.2 历史归档 |
| 输入桥接 B1b.3 | Command → Model → System / Adapter 路径存在 | 已归档但 tasks 仍有 **9 项未勾选**，其中包含 Play Mode 验收，须补验 |
| IK B1b.4 | Rig、脊椎约束、AimTargetDriver、Rig 权重驱动已提交 | 唯一活动 change `aim-ik-rig-constraint`；标定和最终验收未完成 |
| 音频 B1c.1/B1c.2 | 相位驱动脚步/落地音、Audio Model/System/Bridge 已实现 | 历史归档；当前方案不依赖动画事件转发 |
| 射击、敌人、波次 | 射击和敌人业务未实现；`WaveSystem` 两个业务方法为空 | `DamagePlayerCommand` 仍是空骨架，不能算可玩战斗 |
| 骇入 UI 与联动 | UI、Linking、HackSystem、SyncRate 尚未实现 | `StartHackCommand` / `SelectCardCommand` / `HealCommand` 为骨架；Core 也尚未迁入 Unity |

## 停滞过程与恢复线索

1. 5 月 28 日，替换基础动画遇到素材时长与 StarterAssets 物理跳跃节奏不匹配，经历 7 轮试错，保留一个可接受但手感仍别扭的方案。
2. 5 月 31 日，手写 spine yaw/pitch 叠加无法稳定抵消持枪动画的固定姿态；idle/walk 偏差不同、轴向与脊椎曲线问题导致持续调参。
3. B1b.4 换用 Animation Rigging。历史记录指出，最初约束无法解析；调整 AimRig 层级后约束开始起效，但仍有枪口偏向左下和上下半身配合问题。该层级诊断仅作为本模型的历史观察，不推广为所有 Humanoid 的通用限制。
4. 最后提交包含 IK 实现，活动 tasks 的标定、Play Mode、文档和归档仍未完成。用户本轮确认瞄准动作和枪口方向正是搁置原因。

当前场景静态值：`Aim_Spine3.m_Offset = (-50, 20, -35)`。历史调试尾注曾写“探针 offset 清零后待标定”，说明场景后来已有进一步参数尝试；**这些值是恢复起点，不是验收通过的解法**。对齐时使用实际枪口的方向，不能以手骨 forward 与目标夹角替代枪口误差。

历史证据：`openspec/changes/aim-ik-rig-constraint/{proposal,design,tasks}.md`，B1b.3 归档 tasks，以及 [动画疑难记录](AboutTheAnimation.md)。恢复时沿用活动 change，先复现再修改。

## 需要处理的缺口

| 优先级 | 缺口 | 恢复方式 |
|---|---|---|
| P0 | 瞄准姿态、枪口方向与死区联动未通过验收 | 固定场景和姿态，分别检查静止/移动、俯仰、死区内外、进入/退出瞄准；记录角度、参数与画面后再调整 |
| 已解除（环境） | 首次盘点时 SDK / MCP 缺失 | 后续已恢复并完成 Core 139 项测试；Unity 首次运行缺陷见恢复记录 |
| P1 | `PlayerController.Start()` 订阅、`OnDisable()` 退订，但没有重新启用订阅或清空 Model 输入 | 代码静态风险已确认；运行时复现按住移动/瞄准时禁用、重新启用的行为，通过独立修复 change 处理 |
| P1 | Look 实现与主 spec 不符 | 当前 `SAInputAdapter` 直接订阅 Look，主 spec 仍要求保持 StarterAssets 默认路由；先补验再以变更同步契约 |
| P1 | 输入和 IK 更新时序尚无可靠运行证据 | Adapter 实际在 LateUpdate 写输入；AimTargetDriver 也在 LateUpdate。执行顺序特性不能单独证明它早于动画图求值，需实测 |
| 已核对 | 静态文件曾记录旧场景路径 | 后续 Editor API 已通过 GUID 返回正确的 `_Project/Scenes/SampleScene.unity`，无需手改 YAML |
| P1 | 溢出与 factor 契约相互矛盾 | 设计/接口写 `ChainCount / BasePot`，另一段说溢出不再影响 factor；A5/A6 前确认是否封顶计数及结算时点 |
| P1 | factor 与敌人剩余减免率被混用 | 设计中 factor=0 表示保持原减免，旧敌人计划却写 `raw × (1-factor)`；联动前分别命名和定义换算，不直接把两者相赋值 |
| P2 | Core 为 net8.0 且使用现代 C#；旧计划要求直接复制进 Unity | Phase 4 前先验证 Unity 编译兼容性，并确定单一源码方案，避免复制后两套规则分叉 |
| P2 | SciFiEffects 19 个材质 Shader 缺失等历史资产问题 | 仍登记在依赖文档；不阻塞最小逻辑验证，使用前单独验证 |

## 文档和 harness 调整

- 参照 VG2GroupProject：AGENTS 仅作入口，rules 只放长期纪律；项目现状、阶段、近期优先级、专项证据分别维护。
- 历史大清单保存在 [旧 TODO](Archive/2026-06-01-TODO.md) 与 [旧开发计划](Archive/2026-06-01-DEVELOPMENT_PLAN.md)，保留任务编号与当时想法；当前入口改为简明、可核对的现状。
- Codex 的 6 个 OpenSpec skills 由本机 CLI `1.12.0` 生成。后续按用户指示统一为 Codex 开发，旧客户端的入口与工作流已移除；AGENTS.md / agent.md 连接唯一 rules.md。
- `openspec/config.yaml` 填入稳定的项目背景、边界与文档职责，进度继续从共享文档读取，避免再复制一份不断过时的进度日志。
- 新个人入口/skills 本地忽略；本仓库原本跟踪的 OpenSpec 继续保留。VG2 的全本地 OpenSpec 策略没有直接套用到已有历史上，也没有执行取消跟踪或历史重写。
- 本轮修正规格结构和 Purpose，保留 IK delta 原有用户可见场景；**没有同步未验收 IK 到主 spec，也没有归档**。

## 初次回顾的验证边界（环境恢复前）

- 已读取代码、场景序列化内容、Git 历史、14 个归档的任务状态和唯一活动变更；没有运行 Unity 场景或视觉验收。
- 已尝试 `dotnet test CardChainCore/CardChainCore.sln --no-restore`；结果是 **No .NET SDKs were found**，不是测试失败，也不是 139 项本轮通过。
- OpenSpec 初检：13 项（12 个主 spec + 1 个活动 change），2 项通过、11 项不通过；问题包含缺少结构标题、Purpose 占位和 delta 遗漏场景。整理后的结果见下方最终检查记录。
- 工作开始时已有 `ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json` 修改，本轮保留；空文件 `FAIL` 不是有效失败日志，本轮未删除。

## 初次回顾的检查记录（后续结果见环境恢复记录）

- OpenSpec 严格校验：**13/13 通过**（12 个主 spec + 1 个活动 change），0 个失败；仍有旧长段落的 INFO 提示，不影响通过。
- 13 份 Markdown 的本地文件链接检查通过（未验证页内锚点）；Git 差异格式检查通过。
- 两份旧计划快照与整理前 Git 内容一致；12 个主 spec 的原有 Requirement 正文、14 个历史归档保持不变；6 个生成 skills 的文件与元数据完整。
- OpenSpec 能读取项目配置；IK 仍为唯一活动变更，**22/37 项已勾选**，未完成的 15 项保持未完成。
- 检查助手实际执行后以 1 退出，唯一环境阻塞是缺少 .NET 8 SDK；Core 测试明确跳过，没有生成新的测试通过结果。
- 未改动游戏 C#、场景、Prefab、包依赖或第三方资产；开始时已有的 Code Coverage 设置工作区状态保留。未提交或推送。
- 恢复运行验证和功能修复继续见 [TODO.md](TODO.md)。

## 2026-09-19 敌人表现实施补充

当前 SampleScene 已由胶囊替换为 mech_defender，并接入待机、受击、死亡及头顶状态 UI。独立胶囊基线保留，新增注册/注销事实与表现生命周期；三档帧率专项及射击/动作回归已经实际执行通过，用户确认全部验收通过，现已同步规格归档；随后按用户授权整理 TMP 导入资源并复验。详细现状和失败诊断边界见 [ENEMY_PRESENTATION.md](ENEMY_PRESENTATION.md)。上方历史盘点与射击归档记录保留原貌。
