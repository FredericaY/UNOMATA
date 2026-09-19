## Why

移动与越肩瞄准已获验收，但 Fire 仍只记录输入，角色尚不能通过射击使敌人扣血或死亡。本 change 合并原 B2a/B2b 与 B3a 的胶囊受击部分，交付可以实际游玩的瞄准射击、伤害结算和现有素材视听效果，为下一份敌人模型、骨架、动画及简单 UI change 提供稳定业务基础。

## What Changes

- 仅支持键鼠瞄准射击：保持右键瞄准，左键按住按配置射速连发，松开任意一键停止；取消旧计划中的腰射。无限弹药，射线即时命中，曳光只作表现。
- 在本帧最终相机、武器和双臂求值完成后消费真实枪口与枪管方向；射线同时检测环境与敌人，最近实体阻挡优先。枪口前有墙时打在墙上，起点嵌入、过期帧、无效姿态与举放枪过渡禁止射击。
- 建立可替换外观的胶囊敌人：独立身份、HP、基础减免率 R、骇入系数 H、受击及单次死亡。状态由 QFramework Model/System 拥有，碰撞和显示由项目适配层负责。
- 固化本轮已确认的消费端公式：剩余减免率 `R * (1 - clamp(H, 0, 1))`，额外增伤率 `max(H - 1, 0)`，实际伤害 `D * (1 - 剩余减免率) * (1 + 额外增伤率)`。通过测试配置/调试命令给目标设置 H，不接 Core 会话；不决定接龙溢出如何生成 H。
- 使用已导入 SciFiEffects 与 SciFiWeaponsBulletHell 的枪口、曳光、命中特效和枪声等实际素材，补齐音频播放出口；评估现有玩家射击动画，加入轻微武器/上身反馈。素材适配、连发节奏和实际视听效果属于本次验收。
- 提供简单准星/命中反馈、可关闭的伤害调试读数和目标重置入口，验证不同 R/H 与遮挡、移动跳跃射击、启停/失焦边界。

### Scope and Non-Goals

本次只使用一种现有步枪与胶囊目标；下一份 change 才接入 MechPack 等敌人模型、骨架、动画和正式敌人 UI。敌人 AI、波次、Core/HackSession、骇入触发、同步率、系数持续时间、充能、弹匣/换弹、换武器、手柄扩展及持续压枪玩法不在本次。数值和素材组合通过本次试射调节，不升级引擎/包、不采购资产、不改供应商源码，不扩展成全包 VFX 修复。

### Relationship to Existing Changes

复用 `fix-over-shoulder-aim-locomotion` 和 `restore-player-input-baseline` 已验收成果，并重新验证受影响行为。旧 `aim-ik-rig-constraint` 的 22/37 历史保留，不作为前置工作。现有主规格中“Fire 不产生射击”的阶段性文字改为“输入层不直接执行射击，由射击系统消费”，保留输入唯一入口和生命周期保障。

## Capabilities

### New Capabilities

- `aimed-shooting`: 瞄准门控、连发节奏、当帧枪口命中、环境遮挡和射击生命周期。
- `enemy-damage`: 胶囊目标身份与状态、减免/增伤公式、目标系数设置、受击/死亡和测试重置。
- `shooting-feedback`: 与真实射击结果一致的素材特效、轻微射击动作、准星/命中提示与视听验收。

### Modified Capabilities

- `player-input-model`: 明确 Fire Command 只写输入，授权独立射击系统消费，消除旧“骨架空实现/B2a 填射击”歧义。
- `audio-system`: 实装枪声与表面/敌人命中音播放、配置、并发和清理，保持脚步/落地行为。

## Impact

- 代码规划：`Assets/_Project/Scripts/Gameplay/` 新增 Shooting、Enemy 领域及 Controller/View/Utility；扩展 GameApp 注册、Audio 播放和玩家表现协调者中的射击反馈接入点。
- 资产规划：修改项目 `ShoulderAimPlayer.prefab` 和 SampleScene 的必要引用；新增项目步枪配置、胶囊 Prefab、射击反馈副本/配置和显式射击验证场景。通过 Editor/AssetDatabase 操作，保留供应商资产。
- 依赖：沿用 Unity 2022.3.62f3、URP 14.0.12、QFramework 与现有动画/音效/特效包。部分 FORGE3D 预制体有自驱动框架依赖或坏材质，采用前逐项验证选中素材。
- 风险：射击动作可能破坏已验收枪口/握把精度；Blocked 状态可能覆盖过渡状态，不能只按一个状态枚举放行；特效和音频可能重复触发或滞留。设计规定当帧双重状态检查、唯一射击事实与完整回归。
- 文档：同步 Docs/TODO、DEVELOPMENT_PLAN 中的新顺序，以及 GAME_DESIGN/INTERFACE 的已确认消费端规则；实现后再同步 ARCHITECTURE、DEPENDENCIES、动画专项和 README。规划不视为实现，不提前发布 delta 到主 specs。
