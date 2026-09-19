# 射击与胶囊受击基线

> 2026-09-19，用户确认“验收通过”。[unity-shooting-damage-loop](../openspec/changes/archive/2026-09-19-unity-shooting-damage-loop/proposal.md) 的 33/33 项任务完成，5 份正式规格已同步并归档；本次不提交或推送 Git。

## 使用入口

- 正式场景：`Assets/_Project/Scenes/SampleScene.unity`。
- 独立验证场景：`Assets/_Project/Scenes/Sandbox/Sandbox_Shooting.unity`。
- 按住右键瞄准，左键按住连发；任意一键释放即停止新射击。无限弹药，当前调试配置为每发 20、8 发/秒、200m。
- 三个胶囊初始 HP=100、基础减免率 R=0.95，H 分别为 0、0.5、1.2；每发对应约 1、10.5、24 伤害。墙面能够阻挡射击，胶囊死亡后关闭碰撞并隐藏。
- Play Mode 选择胶囊，打开 `UNOMATA > Diagnostics > Shooting` 可查看 D/R/H、剩余减免、结算/实际扣血和前后 HP，显式设置 H 或重置选中目标。调试窗口不承担正式敌人 UI 职责。

## 当前实现

输入 Command 只保存按键。ShootingController 在角色最终姿态之后驱动 ShootingSystem，系统验证当帧 Solution/Snapshot 与输入，再从真实枪口沿枪管方向查询最近实体。稳定的枪口路径遮挡命中墙面；过渡、起点嵌入、不可达和旧帧禁止开火。

EnemyModel/System 保存每目标 HP/R/H 与注册身份，处理公式、重复射击去重和一次死亡。ShootingFeedbackView、CombatAudioView 只消费事实；表现错误不会重复扣血。轻微后坐位移由原 PlayerAimPresentation 协调，下一帧在最终对齐前求解，不增加持续压枪或散布。

伤害消费端为：

```text
remainingReduction = R * (1 - clamp(H, 0, 1))
bonus = max(H - 1, 0)
resolvedDamage = D * (1 - remainingReduction) * (1 + bonus)
appliedDamage = min(CurrentHp, resolvedDamage)
```

Core 仍未生成 H；接龙溢出如何影响 H、骇入效果时长保持后续事项。

## 素材与反馈

- 项目 `VFX/Shooting/RifleMuzzle`、`RifleTracer`、`RifleImpact` 来自 SciFiEffects 的 Vulcan 系列；保留已存在的 FORGE3D/URP/Additive、AlphaBlended 材质引用。当前采用链路无缺失 Shader，不代表包内其余 19 个历史坏材质已修复。
- 枪声采用 SciFiWeaponsBulletHell 的 `SFX_SCIFI_WEAPON_Rifle_Shoot_1/2/3`。普通表面命中采用 SciFiEffects 的 `impact_projectile_001/002/003`，敌人金属命中采用 `impact_projectile_metal_001/002/004`。
- `R_Shoot` 与 `R_AimIdle_AutoShoot` 均约 0.967 秒，包含 SwitchSocket 事件。当前使用程序化 2.5cm 后坐位移与 0.09 秒恢复来保持唯一武器来源；原 fbx 不修改，也不执行其中的挂点事件。
- 反馈配置位于 `Assets/_Project/Settings/Combat/RifleFeedback.asset`；效果池上限合计 32 个根实例、战斗声部 24 个，与脚步/落地两个播放端分开。
- 命中效果和音效保持世界坐标，枪口闪光跟随枪口。声音和效果缺配置时有一次明确诊断，业务结算仍独立。
- 供应商源文件及包版本保持不变；精确采用路径见 DEPENDENCIES。

## 已执行的验证与边界

| 检查 | 当前证据 |
|---|---|
| Unity 实际领域验证 | ShootingDomainValidation：81 项通过（补齐无效握把与不可达方向）；3 条预期错误配置诊断单独记录 |
| Unity 实际 Physics 查询 | ShootingPhysicsValidation：9 项通过，覆盖最近墙、自身/Trigger、距离、内部起点及缓冲饱和 |
| 60fps 完整输入与反馈 | runtime-full-60：27 项通过；10.016 秒 80 发，实测约 60.00fps |
| 反馈故障与资源上限 | runtime-edge-60：21 项通过；缺声音/效果各一次预期诊断，超限有界，恢复需新按键 |
| 动作射击预检 | aim-smoke-60：26/26 通过，130 发，未降低既有枪管与握把门槛 |
| 完整 60fps 矩阵 | 417/417 通过，5,854 发；所有旧门槛保留，射击帧不一致 0 |
| 30fps 动作/最差组合 | 30/30 通过，460 发；实测约 29.94–30.05fps |
| 120fps 动作/最差组合 | 30/30 通过，464 发；实测约 119.00–120.12fps |
| 独立射速 | 30/120fps 各 14 项通过；逐发误差不超过一个实际采样帧，10 秒约 80 发 |
| 60 秒连发 | 14 项通过；60.028 秒 480 发，约 59.99fps，停火后效果/音频回到空闲 |
| 原输入基线 | 独立会话 150 项通过，15 条预期故障诊断单列 |
| 旧边界 / 真实输入与布料 | 30/120fps 边界各 53 项通过；输入/布料 17 项通过，627/627 帧布料有效且时序正确 |
| 保存重载 | 旧角色 79 项、新射击 37 项通过；无测试/运行时对象保存残留 |
| 动作样片与音频 | 6/6 组双机位录制通过；实际混音峰值约 0.505，无削波样本；最终听感获用户确认 |
| 用户视觉与听感、真实失焦体验 | 2026-09-19 用户确认整体验收通过；依据用户确认收口，不新增代理物理输入日志 |

完整输入回归记录的枪口最大误差约 0.0000078°、双手最大位置误差约 0.087mm，射击帧不一致和枪管方向不一致均为 0。这只是该次输入回归的结果，不替代后续完整矩阵。

报告保存在忽略目录 `.utmp/shooting/`：`domain-validation.json`、`physics-validation.json`、`runtime-full-60.json`、`runtime-edge-60.json`、`aim-smoke-60/report.json` 及素材 `setup-report.json`。这些是本次运行产生的证据，未重复使用历史 139/150 项或旧瞄准矩阵的通过数。

首次输入检查曾在姿态求值之前读取当帧快照，验证器现改为读取 afterLateUpdate 采样。首次连发计数失败记录保留；隔离远程查询后，后续实际 60fps 采样无长帧且计数通过。Domain Reload 期间 MCP 有一次 WebSocket 重连警告，连接恢复后复查编译与干净运行，不把工具重连警告当作游戏逻辑通过证据。

## 交付前诊断记录

- 连续切换验证器时曾出现虚拟输入未到达的失败；保留首次报告，射击验证器已增加动作映射停用/启用及恢复。原输入和布料回归分别在独立 Play 会话重跑通过，不把连续运行失败记录改写为通过。
- 快速启停期间曾出现 Persistent 34 / MeshDataArray 1 分配告警。开启分配栈后，多次稳定运行和第三帧提前退出未复现；恢复检测级别时产生旧追踪批次，包含非零行。射击开发前 2026-09-18 的追踪切换日志也有 21 条非零记录，不能据此断言由新射击代码引起。
- 新射击/受击代码没有直接 NativeArray/MeshDataArray 分配；项目内 MeshDataArray 读取位于既有 MagicaCloth2。这里只记录来源线索和复验边界，不声称已定位并修复第三方泄漏。原 Enabled 检测级别已恢复，最后两次独立运行均推进至少 180 帧、布料有效，运行/退出复查 Console 无错误/警告；结果保存在 final-clean-* 记录。
- 临时连接恢复和早退诊断脚本已通过 AssetDatabase 删除。音频录制探针以 UNITY_EDITOR 编译，录制结束即从相机销毁，未写入任何场景/Prefab。
- TagManager 新增 Enemy 层，同时持久化了原角色 Prefab 已引用的 CinemachineTarget 标签。Unity 自动序列化空字段的尾空格保留原生格式，未手改 Scene/Prefab YAML；代码/文档另做差异格式检查。

## 试玩材料

- `.utmp/shooting/shooting-review.mp4`：真实固定机位与越肩机位动作片段，未合成画面；该剪辑不含音轨。
- `.utmp/shooting/shooting-audio-preview.wav`：独立的实际 Unity 游戏混音预览。
- 代理工具无法直接听取音频；已验证引擎输出与录音指标，主观声音搭配由用户试听确认。最终验收以 SampleScene 实际游玩为准：移动/跳跃时右键瞄准、左键连发，检查近墙、停火、命中/击杀、声音搭配和真实切窗恢复。

## 下一阶段

本 change 已完成验收；下一份独立 change 接入敌人模型、骨架、动画和简单 UI，复用当前受击/死亡事实。AI、波次、骇入会话与双线联动继续按共享计划推进。

## 最终用户验收与归档（2026-09-19）

用户明确确认“验收通过”，授权勾选任务、同步文档与归档。这次确认收口此前保留的素材声音搭配、单发/连发节奏、命中/击杀、移动跳落射击观感及真实切窗恢复验收。自动运行证据与用户体验确认分别记录；不声称代理在本次文档收口期间重新试听了全部候选或执行了新的逐键测试。上方历史失败与原生分配告警的诊断边界继续保留。

[归档任务](../openspec/changes/archive/2026-09-19-unity-shooting-damage-loop/tasks.md) 为 33/33。正式规格新增 aimed-shooting、enemy-damage、shooting-feedback，更新 audio-system 与 player-input-model。下一步资产接入列于 [TODO](TODO.md)，本次按用户要求不提交、不推送 Git。
