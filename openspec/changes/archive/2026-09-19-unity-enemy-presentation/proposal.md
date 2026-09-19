## Why

瞄准射击与胶囊受击已验收，但场景目标仍无真实敌人外观、受击/死亡动画和正式状态 UI。本 change 承接 TODO 的 B3a 资产/表现部分，让同一套射击和伤害业务作用于可辨识的机甲敌人，为后续 AI 与波次提供可复用的项目 Prefab。

## What Changes

- 第一版采用已导入 MechPack 的 `mech_defender`，制作项目包装 Prefab、动画控制器和表现配置；接入模型、原配骨架及待机、受击、死亡动画。另两种模型保留后续扩展。
- 复用 EnemyModel/System 的独立身份、HP、基础减免率 R、骇入系数 H、射击去重和死亡事实；拆开 EnemyController 的碰撞生命周期与立即隐藏外观逻辑。致死当帧失去受击/阻挡资格，外观播放一次死亡动画后隐藏。
- 新增注册/注销的类型化事实，使动画和 UI 在首次启用、单独禁用 Controller、显式重置和注销时同步；事件只携带值数据，不把场景对象带入业务层。
- 提供头顶血条及当前剩余减伤/易伤百分比提示，区分 95% 减伤、无减伤及 H>1 的额外增伤；显示由当前快照驱动，墙后、屏幕外及死亡目标不显示 UI。
- 将 SampleScene 的三处胶囊演示位置替换为同种机甲实例，保留 HP/R/H 对照配置；保留 Sandbox_Shooting 的胶囊回归入口，新增独立敌人表现验证场景。
- 覆盖连续受击不反复卡住首帧、致死优先、死亡中重置、旧身份失效、UI 恢复同步及缺失配置；完成保存重载、实际试射和用户视觉验收。

### Scope and Non-Goals

交付行为为静止待机、可被现有步枪射中、独立扣血、受击反馈、一次死亡及简单状态显示。受击动画只作表现，不引入硬直规则、击退或无敌帧。使用粗粒度躯干命中体，不添加弱点/爆头、部件破坏、布娃娃或尸体物理。

本次不实现巡逻/追击/攻击 AI、玩家受伤链、波次/生成器、自动重生、完整对象池、Core/HackSession、骇入触发/计时/溢出、完整 HUD 或额外敌人种类。H 继续来自配置或显式调试命令，三目标对照不代表真实骇入联动；伤害公式和已验收的玩家射击契约保持。

模型缩放、动画过渡及 UI 尺寸/颜色为本提案中的可调实现起点，需实际运行确定；不声称素材存在即通过观感验收。不升级包、不采购资产、不改供应商源文件。

### Relationship to Existing Changes

直接复用已归档 `unity-shooting-damage-loop`。现有 `enemy-damage` 要求死亡目标停止阻挡，与延后隐藏模型兼容；立即隐藏是当前胶囊实现，不是要求所有敌人立即消失的规则。胶囊回归仍保留原可见行为。

旧 `aim-ik-rig-constraint` 由移动瞄准修复承接，保留历史状态，不是本 change 的前置任务。主规格仅在本次实现与验收后同步；既有未提交修改原样保留，Git 收口另按用户授权处理。

## Capabilities

### New Capabilities

- `enemy-presentation`: 机甲模型/骨架接入、待机/受击/死亡动画、碰撞与外观分离、表现生命周期和场景验收。
- `enemy-status-ui`: 目标绑定的生命条、减伤/易伤语义、可见性和只读状态同步。

### Modified Capabilities

- `enemy-damage`: 增加状态提交后的注册/注销事实，支持目标外观与 UI 的可靠绑定和失效处理；保留既有伤害、去重、死亡和系数契约。

## Impact

- 代码：`Assets/_Project/Scripts/Gameplay/Enemy/` 中的 EnemyController、EnemySystem、EnemyEvents，以及新增的动画/UI 表现和配置类型；仅扩展必要的只读目标投影接口，不重写射击或玩家动画。
- 资产：新增项目敌人 Prefab、`Animations/Enemies/`、表现/UI 配置及原生 UGUI/TMP 资产；修改 `Assets/_Project/Scenes/SampleScene.unity`，新增 `Assets/_Project/Scenes/Sandbox/Sandbox_EnemyPresentation.unity`。原胶囊 Prefab 按职责拆分作兼容迁移，供应商模型和动画保持只读。
- 验证：新增敌人表现/状态 UI 的定向验证；适配 ShootingSceneSetup、ShootingSavedSceneAudit 等确实受影响的验证入口，保留旧断言并分清胶囊基线与机甲场景。
- 依赖：沿用 Unity 2022.3.62f3、URP 14.0.12、现有 QFramework、MechPack、UGUI/TMP 与射击反馈；不引入 AI 或寻路依赖。
- 风险：动画死亡被旧隐藏逻辑截断、连发反复重启受击、初始化顺序导致漏绑、死亡动画重置后误隐藏新身份、UI 穿墙或把 H 错显示为减伤。设计逐项给出独立验证场景。
- 文档：规划期只建立 change 并更新共享计划中的入口与“提案就绪、尚未实现”状态；实施后同步 ARCHITECTURE、INTERFACE、DEPENDENCIES、GAME_DESIGN、验收专项、TODO/DEVELOPMENT_PLAN 与 README 双语。规则及历史归档不修改。

### User-authorized asset closeout

2026-09-19 用户验收通过后追加 TMP 导入整理：将共用字体/Shader 与许可按既有资产规则归到 ThirdParty/UI/TextMeshPro，保 GUID 清理未使用示例和重复配置，恢复导入覆盖副作用并同步相关路径和验证。此为本 change 的 UI 依赖收口，原玩法范围不变；具体迁移与任务见 design 的追加章节及 tasks 第 7 组。
