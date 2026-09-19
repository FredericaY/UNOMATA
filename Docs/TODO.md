# 近期计划

> 更新：2026-09-19。人物与基础战斗表现小阶段已全部验收：移动瞄准、射击伤害、敌人模型/动画/UI 均已归档，TMP 导入整理完成。本轮统一提交与推送阶段成果，后续计划另行讨论。

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

- [x] **完成并验收 [unity-shooting-damage-loop](../openspec/changes/archive/2026-09-19-unity-shooting-damage-loop/proposal.md)**：B2a/B2b + B3a 胶囊受击，33/33 项完成。仅右键瞄准时左键连发、无限弹药、真实枪口射线，独立 HP/减免/骇入系数结算与死亡，现有特效/声音及轻微射击动作均已接入。81 项领域、9 项物理、27 项完整输入、21 项反馈边界和 417 组完整矩阵通过；三档帧率、一分钟连发与受影响输入/布料回归已完成。2026-09-19 用户确认最终验收通过，五份正式规格已同步；见 [归档任务](../openspec/changes/archive/2026-09-19-unity-shooting-damage-loop/tasks.md) 与 [射击基线](SHOOTING_BASELINE.md)。


- [x] **完成并验收 [unity-enemy-presentation](../openspec/changes/archive/2026-09-19-unity-enemy-presentation/proposal.md)**：原 26 项与用户追加的 TMP 资源整理 3 项共 **29/29** 完成。mech_defender 待机/受击/死亡、血条/减伤/易伤和身份生命周期均已通过自动验证及用户最终验收；三份主规格已同步归档。TMP 所用资源保 GUID 归入 ThirdParty/UI/TextMeshPro，无引用示例与重复配置已清理并复验。详见 [敌人验收记录](ENEMY_PRESENTATION.md)。
- [x] **当前人物与基础战斗表现小阶段完成**：移动、瞄准、射击、胶囊/机甲受击与表现均已验收；完整游戏循环和后续方向仍待下一次讨论。


## 下一步（待后续讨论）

当前人物与基础战斗表现小阶段已全部验收完成。按用户要求，本轮只做归档、文档同步与阶段 Git 收口，不启动新的开发 change。

后续方向保留为讨论候选，不在此固定优先级：
- A4–A7：完整 HackSession、奖励池、结算与 Console 演示；A5/A6 前确认溢出是否继续增加 H。
- B3b / B3c / B4：敌人 AI、波次与骇入触发。
- Phase 3 / 4：完整 UI、Core 兼容性与单一源码接入、双线联动。

射击 change 上次延后的 Git 收口与本次敌人表现、TMP 整理一起纳入当前阶段提交；提交和推送结果以 Git 记录为准。

本轮反馈的非瞄准奔跑穿枪、举枪动作混合、快跑下半身和起跳时序均已修复并获用户最终认可。举枪仅纯 W 可跑；A/S/D 与斜向即使 Shift 也走路。

## 保留的待定项

- 骇入系数 H 与基础减免率 R 已区分，敌人伤害按 `D * (1 - R * (1 - clamp(H, 0, 1))) * (1 + max(H - 1, 0))` 结算，已实现并验收。Core 溢出后 H 是否继续增长、效果持续时间仍待后续确认。
- 原 A/B 分工仅作为工作流编号，人员、时间与目标平台未重新确认。
- UI 资产、惩罚与同步率数值、效果持续时间、充能上限保持 TBD。
- 本次射击采用的 SciFiEffects 素材已完成所需兼容适配及运行/用户验收；其余未使用素材的 Shader 遗留和新增跳跃玩法仍留后续。走跑跳动画衔接、武器挂点和双手 IK 已由瞄准修复完成。

当前基线已恢复，完整“射击 + 骇入接龙”游戏循环仍未完成；阶段规划见 [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)。

移动瞄准阶段复验记录（2026-09-18）：侧向/后退举枪跑观感未通过，明确改为举枪仅纯 W 可跑，A/S/D 与斜向即使 Shift 也走路；本轮新规则专项 20 项、30/120fps 各 30 项、实际输入链 17 项、保存引用 82 项均通过；最终规则已获用户认可；旧方向跑录像仅作历史证据。

跳跃阶段反馈记录（2026-09-18）：首帧腿部响应滞后及 AirR→AirL 重播。已改为同帧起跳混合和物理相位驱动的连续动作，30/60/120fps 各 8 项、60fps 动作回归 30 项、输入/布料 17 项及保存引用 79 项通过，保存重载已确认；用户已确认观感验收通过。
