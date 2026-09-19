# 开发阶段规划

> 更新：2026-09-19。按可独立验证的交付物划分阶段，不绑定过期日期。移动/越肩瞄准修复与瞄准射击/胶囊受击均已通过自动验证及用户体验验收，正式规格已同步归档。
> A/B 沿用历史工作流编号（A=Core，B=Unity），不假定当前仍有原定人员分工。

## 阶段与现状

| 阶段 | 交付标准 | 当前状态 |
|---|---|---|
| Phase 0 环境与资产 | 工程、框架、资产与基础验证入口可用 | 工具环境已恢复；输入修复与场景验证通过，恢复阶段已获用户整体确认 |
| Phase 1 Core | Console 可完整运行一次骇入，规则/事件/结束路径有测试 | A1–A3 已实现；A4–A7 未实现 |
| Phase 2 Unity TPS | 移动、瞄准、射击、受击、敌人和波次形成闭环 | 移动、瞄准、输入、射击、胶囊受击与音频已验收；敌人资产/动画/UI 已验收，当前人物小阶段完成；AI/波次及完整战斗闭环未完成，下一步另行讨论 |
| Phase 3 副线 UI | 可显示牌、选项、计时、链数与反馈 | 未实现 |
| Phase 4 双线联动 | Core 接入 Unity，骇入结果正确作用于目标 | 未实现 |
| Phase 5 平衡 | 波次、难度与同步率形成可评估体验 | 未开始 |
| Phase 6 打磨与研究验证 | 回答游戏设计中的双线验证问题 | 未开始 |

## 恢复里程碑 R

恢复运行、输入及移动瞄准里程碑已完成，射击与胶囊受击已于 2026-09-19 验收；当前次序见 [TODO.md](TODO.md)。恢复历史见 [PROJECT_REVIEW.md](PROJECT_REVIEW.md)，射击证据见 [SHOOTING_BASELINE.md](SHOOTING_BASELINE.md)。格式校验通过不替代运行验收。

2026-09-19 用户确认当前人物与基础战斗表现小阶段全部完成；本轮归档和 Git 收口后，下一步范围与优先级另行讨论。下列未完成阶段保留为规划参考，不代表已启动。

## Phase 1 — 独立 Core

- **A1 类型、A2 合法性与状态更新、A3 发牌器**：源码与测试存在，历史归档分别为 2026-05-26 / 05-26 / 05-27。SessionState、IsValidNext、ApplyPrev 已实现，不再列为未开始。
- **A4 HackSession**：创建/启动、计时、合法选择与轮次、8 个正式事件；尚无源码。
- **A5 奖励池**：basePot / maxPot、满档 latch、反转/王牌奖励、方向变化、溢出；先消除 factor 与溢出语义矛盾。
- **A6 结束与结果**：TimeUp / WrongCard / Surrender、幂等结束、边界行为、HackResult。
- **A7 Console**：真实会话演示，覆盖结束路径与死局响应；当前 Program 仅为 scaffold。

依赖 A1 → A2 → A3 → A4 → A5 → A6 → A7。Core 不依赖 Unity，运行失败与测试失败须区分。正式验收为实际 build/test 成功及完整会话输出，不复用旧的示例日志作为结果。

## Phase 2 — Unity TPS

| 编号 | 内容 | 状态 |
|---|---|---|
| B0 | QFramework 骨架 | 历史已归档；Wave/骇入等仍为占位 |
| B1a / B1b.1 | 模型、布料、基础动画 | 历史已归档；遗留起跳时序已修复，通过三档帧率检查并获用户认可 |
| B1b.2 | 瞄准动画层与双相机 | 历史已归档 |
| B1b.3 | QFramework 输入桥接与下半身 Strafe | 实现已归档，但 9 项验收未勾选；补验与风险见回顾 |
| B1b.4 | Animation Rigging 上半身瞄准 | 旧 22/37 保留历史；[fix-over-shoulder-aim-locomotion](../openspec/changes/archive/2026-09-18-fix-over-shoulder-aim-locomotion/proposal.md) 承接完整移动瞄准修复，正式场景已接入，自动验证及用户验收通过，47/47 完成并归档 |
| B1c.1 / B1c.2 | 脚步落地音与 Audio QFramework | 已先于 B1b.4 归档，不再作为其未来下游 |
| B2a / B2b + B3a 受击部分 | 瞄准射击、胶囊伤害结算与素材反馈 | unity-shooting-damage-loop 已通过自动验证与用户最终验收，33/33 完成；5 份正式规格已同步，2026-09-19 归档 |
| B3a 资产与表现部分 | 敌人模型、骨架、动画和简单 UI | [unity-enemy-presentation](../openspec/changes/archive/2026-09-19-unity-enemy-presentation/proposal.md) 已实现 mech_defender、三态动画及头顶 UI，自动验证及用户验收全部通过，含 TMP 整理 29/29 项完成并归档 |
| B3b / B3c | 敌人 AI、波次 | 未实现，WaveSystem 仅有空方法 |
| B4 | 骇入目标检测/触发 | 未实现，StartHackCommand 为占位 |

最小战斗推进时先定义可独立使用的受击契约，再让射击和敌人接入，避免旧清单中 B2a/B3a 互相依赖的表述。HP、减免和死亡规则归 System/Model；Controller 承接碰撞与场景表现。保留 Raycast 射击、Behavior Designer AI 等原设计方向，具体实施通过新 change 明确，不照搬旧伪代码。

当前已确认的两次交付：先由 [unity-shooting-damage-loop](../openspec/changes/archive/2026-09-19-unity-shooting-damage-loop/proposal.md) 完成仅瞄准连发、胶囊 HP/减免/增伤/死亡及现有素材的成套射击反馈，再接入真实敌人外观与简单 UI。第一份使用无限弹药和射线即时命中，曳光只表现；轻微射击动作、枪口/命中特效和声音需要实际运行及用户视听验收。旧清单中的腰射取消，弹匣/换弹、持续压枪、AI/波次和骇入联动不并入第一份。第一份已完成场景接入、领域/物理/输入及完整动作矩阵验证，并于 2026-09-19 获用户整体视听与真实切窗验收确认，33/33 项完成并归档。第二份 [unity-enemy-presentation](../openspec/changes/archive/2026-09-19-unity-enemy-presentation/proposal.md) 已于 2026-09-19 完成自动验证与用户验收并归档：首版采用 mech_defender，接入原配骨架、待机/受击/死亡动画及头顶血条和减伤/易伤提示，复用 HP/R/H、受击及死亡事实；补齐注册/注销的表现同步，死亡立即关闭碰撞而在动作结束后隐藏。AI/波次另行推进。

移动/瞄准修复已完成并获用户验收，后续可进入人物控制与战斗开发。已交付范围覆盖真实枪口几何、双手握枪、八方向行走、纯向前举枪奔跑及移动跳落、自然身体转向、相机碰撞/视差及受影响的音频和生命周期；范围内疑点、缺陷与必要验收已完成闭环。实际射击、换弹、后坐力、敌人和骇入不纳入该 change。

## Phase 3 — 副线 UI

目标仍为世界空间骇入面板：当前牌、3–5 个选项、倒计时、链数、满档/失败反馈。UI 可以基于明确的模拟数据并行原型，但完整联动以实际 Core 契约验收为前提。“曾计划冻结接口”不表示 HackSession 已实现。

## Phase 4 — 双线联动

前置：完整 Core 与最小 TPS 闭环可验收。

- 先验证 .NET 8 源码在 Unity 环境的编译兼容性，确定单一源码维护方式；历史“复制源码迁入”是原方案，尚未执行。
- HackSystem 管理会话、Tick、订阅与结束；Linking 把 Core 事实转换为 Unity 命令，UI 只表现。
- 复用射击 change 已实现并验收的敌人消费端换算（见 GAME_DESIGN 3.7.3），由 Linking 设置指定目标的 H；Core 如何从接龙/溢出产生 H 仍须在 A5/A6 前明确，禁止直接把 H 当剩余减免率。
- 接入 SyncRate、弃牌/死局反应窗口、溢出充能；所有数值/惩罚待定项保持显式。
- 验收正常与失败路径、重复结束、目标消失、场景退出、输入与事件清理。

## Phase 5–6 — 平衡、打磨与研究

在闭环可玩后再调整波次参数、同步率、惩罚、效果持续时间、满档与溢出、充能缓存等。后续处理 VFX/音频/UI、美术和动画手感，并录制游玩结果，回答 [GAME_DESIGN.md](GAME_DESIGN.md) 的验证问题。

详细旧清单保存在 [2026-06-01 快照](Archive/2026-06-01-DEVELOPMENT_PLAN.md)，仅用于追溯当时方案。
