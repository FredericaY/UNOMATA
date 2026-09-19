# 敌人资产与状态 UI

> 2026-09-19。[unity-enemy-presentation](../openspec/changes/archive/2026-09-19-unity-enemy-presentation/proposal.md) 用户已确认“验收全部通过”。原 26 项实施/验收和追加 3 项 TMP 资源整理全部完成；三份主规格已同步，本 change 于 2026-09-19 归档。

## 试玩入口

- 正式场景：`Assets/_Project/Scenes/SampleScene.unity`。
- 独立机甲场景：`Assets/_Project/Scenes/Sandbox/Sandbox_EnemyPresentation.unity`，包含额外的后方目标。
- 原胶囊回归场景：`Assets/_Project/Scenes/Sandbox/Sandbox_Shooting.unity`，保留胶囊原有即时隐藏行为。
- 右键瞄准、左键连发；机甲原地待机，被击中时短暂反应，死亡时先关闭碰撞与血条，再完整播放倒下动作并隐藏。
- SampleScene 三个目标继续使用 HP=100、R=0.95、H=0/0.5/1.2 的测试对照，每发约扣 1/10.5/24 HP。正中与右侧目标较快出现死亡；左侧高减伤目标需要较长时间射击。
- Play Mode 选中敌人，`UNOMATA > Diagnostics > Shooting` 可查看伤害、修改指定 H 和重置。重置产生新身份，同时恢复模型、动作、命中体和血条。
- 本版没有移动/攻击 AI、波次、自动重生或真实骇入计时；H 仍是配置/显式调试输入。

## 表现与所有权

EnemyModel/System 仍是 HP/R/H、伤害和存活状态的唯一来源。注册/注销完成后增加 EnemyRegisteredEvent / EnemyUnregisteredEvent，事件仅含值数据。EnemyController 负责身份与碰撞，在死亡或注销时立即关闭命中体，不再写 Renderer。

EnemyPresentationView 负责 Idle/Hit/Death/Hidden。命中实际扣血大于零且非致死时播放 Hit；Hit 尚未播完时继续扣血，但不会反复从首帧重启或积压动作。死亡优先于受击，动作结束后隐藏；离屏仍推进，动作失效时有限兜底并诊断。重置/启停清理旧身份的计时与动作。胶囊使用同一 View 的非动画配置。

EnemyStatusView 订阅同一目标身份，通过 EnemyStatusData 复用既有 DamageCalculation 的减伤/增伤计算，只缓存显示值。世界空间 UGUI/TMP 的健康条与标签不监听输入，不含物理 Collider；在最终相机之后更新，墙后、背面、视口外和 40m 之外隐藏。遮挡忽略玩家/目标自身与 Trigger，查询饱和时完整重查。

| H（R=0.95） | 显示 |
|---|---|
| 0 | RESIST 95% |
| 0.5 | RESIST 47.5% |
| 1 | RESIST 0% |
| 1.2 | VULNERABLE +20% |
| 1.6 | VULNERABLE +60% |

现有 ShootingFeedbackView/CombatAudioView 继续负责命中声音、死亡粒子与击杀提示，敌人动画不重复请求反馈。相机、玩家 Collider 与 UI 引用由场景显式接线，表现之间不调用业务。

## 资产与工具

- `Prefabs/Enemies/MechDefender.prefab` 包装已有 MechPack 的 mech_defender，保留原配骨架与材质，移除项目副本中的供应商 Rigidbody/Collider，改用项目根的躯干 CapsuleCollider。
- `Animations/Enemies/MechDefender/` 保存项目 Idle/Hit/Death 副本与 controller。来源为 mech_defender@animations.FBX 的 idle_01、damage_01、death_01，实测时长约 3.333/0.333/3.333 秒；仅 Idle 循环，均无供应商回调。
- `Settings/Combat/Enemies/` 保存普通 H=0 的 Defender 配置、动画/UI 配置和胶囊兼容表现配置；演示实例仍引用原三组 Capsule 配置，运行不修改资产。
- UI 嵌入敌人 Prefab，引用已有 LiberationSans SDF。新增 `Settings/Resources/TMP Settings.asset`，为项目提供缺失的 TMP 初始化配置。
- `EnemyPresentationSetup.Configure()` 创建/接线机甲资产与场景，可重复运行；旧 ShootingSceneSetup 只允许操作已保存的 Sandbox_Shooting，避免在正式场景重建胶囊。
- ShootingSavedSceneAudit 按指定场景验证对应机甲或胶囊 Prefab，EnemyPresentationSavedSceneAudit 再检查机甲动画/UI 专项；原玩家保存审计保持。
- 上述资产均位于 `Assets/_Project/`。MechPack、包版本与规则文件保持；TMP 字体/Shader 在验收后的用户授权整理中保 GUID 迁到 ThirdParty/UI/TextMeshPro，源内容保持一致，许可一并保存。其余来源/授权待补信息仍见 DEPENDENCIES。

## 本次实际验证

| 检查 | 结果 |
|---|---|
| 新生命周期与 UI 数值领域检查 | 25 项通过，含事件内查询、失败不广播、旧身份失效及极大 H 的有限显示 |
| 原射击领域/物理回归 | 本轮重跑 81 / 9 项通过 |
| 30fps 敌人运行矩阵 | 66 项通过，采样约 30.03fps |
| 60fps 敌人运行矩阵 | 66 项通过，采样约 60.82fps |
| 120fps 敌人运行矩阵 | 66 项通过，采样约 120.28fps；在独立机甲场景运行 |
| 原胶囊完整输入射击 | 28 项通过，约 59.99fps，110 发；清理完成 |
| 正式机甲完整输入射击 | 28 项通过，约 60.00fps，110 发；清理完成 |
| 保存重载审计 | 正式机甲 64 项、独立机甲 83 项、射击公共/目标 37 项、既有玩家 79 项通过 |
| 玩家动作射击回归 | 30/30 类通过，460 发；实测各类约 58.04–60.16fps，清理完成 |
| 补充实际命中/配置失败验证 | 13 项通过，包含三种配置独立伤害/血条、无效 Controller 安全失效及恢复 |
| 用户视觉验收 | 2026-09-19 用户明确确认全部通过；据此完成 5.6，不以自动检查代替 |

66 项矩阵包含连续受击不重播首帧、零扣血、死亡优先/唯一性、死亡中重置、早/晚绑定、Controller/View 三次启停、旧事件隔离、字体网格、血量/防护标签、墙后/背面/超距隐藏、自身/Trigger 过滤、超过 64 个忽略碰撞体仍不漏墙、缺相机/锚点/图形/配置、动画缺失/停滞兜底及真实物理前后目标检测。每轮 8 条人为缺项/故障诊断单列，不能视作交付配置仍有错误。

报告位于被忽略的 `.utmp/enemy-presentation/`，包括 domain、runtime-30/60/120、两类完整射击副本和保存审计。运行截图 idle/hit/death/status-close/occluded.png 为实际 Unity 摄像机输出，不是生成示意图。

独立正常 Play/Stop：两轮各推进 180 帧，三个目标正常待机，运行和退出 Console 均无错误/警告；报告为 clean-run-1/2.json。

## 诊断与证据边界

- 初次脚本写入遇到一次 SourceAssetDB 文件时间戳导入错误；完整刷新后重新编译、运行检查通过。新增 Editor 脚本必须做资产刷新，仅请求脚本编译不保证新文件已导入。
- 资产 setup 首次使用 AnimationClip.events 触发 Editor 的持久化事件诊断，已改用 AnimationUtility.SetAnimationEvents 并重新执行 setup；交付 clips 无事件，后续配置/运行无该错误。
- 首次文字不显示的根因是 TMP_Settings 缺失，TMP Awake 提前返回；已新增项目配置，实际字形网格和截图复验通过，没有修改 TMP 包或供应商字体。
- 首次 60fps 可见性检查失败于测试相机被 Cinemachine 自动更新覆盖。测试现显式停用并恢复该相机驱动；失败报告保留为 runtime-60-first-camera-fixture-failure.json，后续矩阵重新通过。
- 首次 MCP 截图的合成路径报 Render Target 尺寸/帧时机错误，随后采用指定 Camera 或验证器直接渲染并释放 RenderTexture；不将工具截图错误计为游戏逻辑失败或掩盖它。
- 自动结果与最终视觉认可分别记录。当前工作没有新增 Core 能力，也没有以本次验证重写旧归档的历史验收。

最终差异复核补充：Controller 的命中体所有权限定为最近的 EnemyController，避免嵌套错误接线的父目标禁用合法子目标。专门运行验证 4 项通过（ownership.json），随后重新执行 60fps 敌人 66 项矩阵通过；30/120fps 报告来自本轮该保护修订前的正常配置回归。最终代码另跑 180 帧正常场景并通过，退出后 Console 无错误/警告，见 clean-run-final.json。胶囊立即隐藏/重置/注销兼容补验 3 项通过（capsule-compatibility.json）；旧胶囊 setup 对 SampleScene 的拒绝保护已实际核对。

## TMP 导入整理与最终验收 — 2026-09-19

用户明确确认“验收全部通过”，并要求按规则整理随后按提示导入的 TextMesh Pro，再统一归档和提交推送。原 26 项任务加 3 项资产收口任务共 29 项完成；本次验收来源为用户确认，不补写代理未执行的逐键体验。

TMP 现位于 Assets/ThirdParty/UI/TextMeshPro，包含实际使用的 LiberationSans 字体、SDF/回退资源、Mobile SDF Shader、共用 include 和 LiberationSans - OFL.txt。三份旧 meta 的 licenseType 导入覆盖已恢复；15 份原文件按 Git 内容哈希逐项一致，9 个迁移资产/目录 GUID 保持，运行时仅保留项目 TMP Settings。未采用示例、演示脚本、重复设置等在全工程依赖确认无引用后通过 AssetDatabase 清理；原导入可恢复备份位于忽略目录 .utmp/tmp-import/user-import-before-cleanup.unitypackage。

迁移后本轮重新通过：10 项 GUID/唯一配置检查、64 项机甲保存审计、60fps 敌人运行 66 项，以及正常运行/退出的 Console 检查。记录在 .utmp/tmp-import。移动工具曾返回异常，但逐路径/GUID核对证明移动已成功，因此没有重复移动；连接恢复后的检查正常。字体源内容和已验收外观保持，未升级依赖或重做玩法。

本 change 的三份 delta 已同步主规格并归档；当前人物与基础战斗表现小阶段完成。后续工作范围由用户下次讨论决定。本阶段射击与敌人相关成果统一提交，提交和推送结果以 Git 记录为准。
