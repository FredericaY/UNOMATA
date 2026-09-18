# 近期计划

> 更新：2026-09-18。用户确认项目已回到当初暂停开发时的基线，本轮回顾、规范整理与输入恢复阶段验收完成。瞄准动画仍有偏差，后续单独新建 change。

## 本轮已完成

- [x] 回顾代码、工程、历史归档与搁置原因，保留旧计划快照。
- [x] 对照 VG2GroupProject 当前标准核对并补齐规则：QFramework、Unity MCP、文档同步与 Git 纪律；Codex 作为唯一开发客户端。
- [x] 恢复 Unity 2022.3.62f3、MCP 与 .NET SDK 8.0.425；Core 三项目构建成功，恢复阶段实际重跑 139 项测试通过。
- [x] 输入基线修复通过 150 个自动断言、15 种错误配置；真实 TPC 场景注入验证同帧消费及跨落地不连跳。
- [x] 清理 Audio 场景残留，两个独立 Play/Stop 与音频复验通过，最终 Console 零错误/警告。
- [x] 用户对“恢复到暂停前阶段”作出阶段验收确认；保留未逐项记录的物理键鼠日志边界，不宣称瞄准姿态问题已解决。
- [x] restore-player-input-baseline 的 21 项任务收口，三个 delta 同步主规格并归档。见 [归档任务](../openspec/changes/archive/2026-09-18-restore-player-input-baseline/tasks.md) 与 [验收记录](INPUT_BASELINE.md)。

## 下一步

- [ ] **单独新建瞄准动画/枪口偏差修复 change**：先确定复现姿态与枪口方向指标，再选择实现和验收方案。
- 旧 `aim-ik-rig-constraint` 的 22/37 记录只保留为历史实现与未验收问题的参考。本轮没有把它标为完成或归档；后续新 change 明确承接/替代关系，不自动继续旧任务。
- [ ] A4–A7：HackSession、奖励池、结算与完整 Console 演示；A5/A6 前确认 factor 和溢出规则矛盾。
- [ ] B2a / B3a：明确受击接口与状态所有权，形成射击 → 敌人受击/死亡的最小闭环。
- [ ] B3b / B3c / B4：敌人 AI、波次与骇入触发。
- [ ] Phase 3 / 4：UI 和 Core 接入 Unity，先核对运行时兼容性及单一源码维护方式。

## 保留的待定项

- 骇入削减系数与敌人剩余减免率分别定义，不能直接同名赋值；溢出后 factor 是否继续增长仍待确认。
- 原 A/B 分工仅作为工作流编号，人员、时间与目标平台未重新确认。
- UI 资产、惩罚与同步率数值、效果持续时间、充能上限保持 TBD。
- SciFiEffects Shader、跳跃美化、武器挂点/左手 IK 等在相应后续 change 处理。

当前基线已恢复，完整“射击 + 骇入接龙”游戏循环仍未完成；阶段规划见 [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)。
