# 近期计划

> 更新：2026-09-18。输入恢复和移动/越肩瞄准修复均已获用户验收；本次 47/47 项完成，正式规格已同步并归档。后续进入人物控制与战斗闭环开发。

## 本轮已完成

- [x] 回顾代码、工程、历史归档与搁置原因，保留旧计划快照。
- [x] 对照 VG2GroupProject 当前标准核对并补齐规则：QFramework、Unity MCP、文档同步与 Git 纪律；Codex 作为唯一开发客户端。
- [x] 恢复 Unity 2022.3.62f3、MCP 与 .NET SDK 8.0.425；Core 三项目构建成功，恢复阶段实际重跑 139 项测试通过。
- [x] 输入基线修复通过 150 个自动断言、15 种错误配置；真实 TPC 场景注入验证同帧消费及跨落地不连跳。
- [x] 清理 Audio 场景残留，两个独立 Play/Stop 与音频复验通过，最终 Console 零错误/警告。
- [x] 用户对“恢复到暂停前阶段”作出阶段验收确认；保留未逐项记录的物理键鼠日志边界，不宣称瞄准姿态问题已解决。
- [x] restore-player-input-baseline 的 21 项任务收口，三个 delta 同步主规格并归档。见 [归档任务](../openspec/changes/archive/2026-09-18-restore-player-input-baseline/tasks.md) 与 [验收记录](INPUT_BASELINE.md)。

- [x] **完成并验收 [fix-over-shoulder-aim-locomotion](../openspec/changes/archive/2026-09-18-fix-over-shoulder-aim-locomotion/proposal.md)**：47/47 项完成；八方向行走、纯向前举枪奔跑、移动跳落、枪口/双手、相机与音频已修复，用户确认最终验收全部通过。正式规格已同步；见 [归档任务](../openspec/changes/archive/2026-09-18-fix-over-shoulder-aim-locomotion/tasks.md) 和 [验收记录](AboutTheAnimation.md)。
- 新实现替代旧 `aim-ik-rig-constraint`；旧 22/37 及未验收 delta 保留历史，不自动标为完成，也不在本轮另行归档。

## 下一步

- [ ] A4–A7：HackSession、奖励池、结算与完整 Console 演示；A5/A6 前确认 factor 和溢出规则矛盾。
- [ ] B2a / B3a：明确受击接口与状态所有权，形成射击 → 敌人受击/死亡的最小闭环。
- [ ] B3b / B3c / B4：敌人 AI、波次与骇入触发。
- [ ] Phase 3 / 4：UI 和 Core 接入 Unity，先核对运行时兼容性及单一源码维护方式。

本轮反馈的非瞄准奔跑穿枪、举枪动作混合、快跑下半身和起跳时序均已修复并获用户最终认可。举枪仅纯 W 可跑；A/S/D 与斜向即使 Shift 也走路。

## 保留的待定项

- 骇入削减系数与敌人剩余减免率分别定义，不能直接同名赋值；溢出后 factor 是否继续增长仍待确认。
- 原 A/B 分工仅作为工作流编号，人员、时间与目标平台未重新确认。
- UI 资产、惩罚与同步率数值、效果持续时间、充能上限保持 TBD。
- SciFiEffects Shader 与新增跳跃玩法仍留后续；当前走跑跳动画衔接、武器挂点和双手 IK 已纳入本次瞄准修复范围。

当前基线已恢复，完整“射击 + 骇入接龙”游戏循环仍未完成；阶段规划见 [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)。

最新用户复验：侧向/后退举枪跑观感未通过，明确改为举枪仅纯 W 可跑，A/S/D 与斜向即使 Shift 也走路；本轮新规则专项 20 项、30/120fps 各 30 项、实际输入链 17 项、保存引用 82 项均通过；最终规则已获用户认可；旧方向跑录像仅作历史证据。

最新跳跃反馈已逐帧确认：首帧腿部响应滞后及 AirR→AirL 重播。已改为同帧起跳混合和物理相位驱动的连续动作，30/60/120fps 各 8 项、60fps 动作回归 30 项、输入/布料 17 项及保存引用 79 项通过，保存重载已确认；用户已确认观感验收通过。
