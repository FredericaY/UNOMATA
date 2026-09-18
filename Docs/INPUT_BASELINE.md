# 输入基线修复 — 实现与验收

> 2026-09-18，变更 `restore-player-input-baseline`。实现已落地，自动回归通过；用户已确认恢复基线达到本阶段目标，21/21 项收口并归档；Audio 清理与运行/音频复验已通过，IK 偏差保留给后续新 change。

## 已实现

- 项目输入副本的 Player Map 现在持久包含 Move、Look、Jump、Sprint、Aim、Fire。Aim=右键、Fire=左键，均为 Button / KeyboardMouse。原四个动作、绑定、控制方案、输入资产 GUID 保持不变。
- PlayerController 对引用、Map、全部必要动作及类型先作整体校验。失败不保留部分订阅，每次失败绑定尝试只报具体缺项；启停、失焦、输入源停用与销毁会清理状态。
- 初次/重新绑定在 Start 或 Update 中处理，以使用 PlayerInput 当前实例及游戏输入上下文。不会在清理时调用惰性 GameApp 入口创建新架构。
- Move/Sprint/Aim 恢复后重新采样；Jump/Fire 对失效期间仍按住的按钮要求释放后重新按下，避免重放旧请求。Sprint 按实际值处理 PassThrough 的松开。
- 瞄准路径：SetAimStateCommand → PlayerInputModel.IsAiming → PlayerSystem → PlayerModel.IsAiming / AimStateChangedEvent。同值重复写入不重复发事件；System.OnDeinit 注销输入订阅。
- SAInputAdapter 在 Update(-10) 准备运动缓冲；Jump 只在新按下边沿投递，TPC 消费后不会每帧重新注入。Look 单独直通，不进入业务 Model、不累加已累计的 delta，停用/失焦时清零。

本阶段左键只记录 Fire 输入，没有射击效果。单独禁用业务 Controller 不禁止独立 Look 适配器；停用适配器或输入源才清理 Look。第三方代码、IK 参数与包版本未修改。随后经用户授权，仅清理了 SampleScene/Audio 的已定位序列化残留。

## 验证入口

在 Unity Play Mode 中执行菜单 **UNOMATA → Validation → Run Input Baseline**，或通过 MCP 调用：

```csharp
Unomata.Editor.Validation.InputBaselineValidation.Start();
Unomata.Editor.Validation.InputBaselineValidation.Status();
Unomata.Editor.Validation.InputBaselineValidation.Cancel();
```

验证器位于项目 Editor/Validation，默认不挂入场景。它临时停用场景输入组件，创建隔离对象与虚拟键鼠/手柄，通过真实 InputAction 回调验证业务链路，并自动恢复原组件状态。验证期间不能同时进行人工游玩。

测试临时开启运行态 runInBackground 以等待真实游戏帧，结束恢复原值，不修改 PlayerSettings 或全局 Input System 设置。失焦用例是显式发送焦点生命周期消息的自动回归，**不等同真实窗口切换实测**。

故障矩阵故意制造缺引用/动作等错误，因此 Console 中以 `InputBaselineValidation_Fixture` 为对象的绑定失败诊断是测试预期。判断完整结果应查看状态和报告，不能把预期诊断当成正常场景的新错误。正常场景需用单独 Play 会话验证。

本地结果保存到 `.utmp/restore-input/validation.json`，本轮完整通过报告另外保留为 `validation-passed-final.json`。这些临时文件不提交；共享结论记录在本文。

## 本轮自动回归结果

| 检查 | 实际结果 |
|---|---|
| 完整回归 | **150 个断言通过**，15 种配置故障，实际游戏帧 107 → 350（243 帧） |
| 初版完整回归 | 148 个断言通过；随后增加两个鼠标 delta 用例再运行，得到上面的 150 个 |
| 动作持久化 | 源 JSON、重导入、脚本 Domain Reload、多个独立 Play 实例均能读取六动作 |
| 配置故障 | 缺 PlayerInput、资产、Map、任一六动作，以及各动作类型错误；全部安全失败且无部分业务订阅 |
| 正常输入 | Move、Jump、Sprint、Aim、Fire 按下/释放，现有手柄绑定、Look 与鼠标 delta 行为均通过 |
| 生命周期 | 三次组件启停、首次未启用、PlayerInput/Map 停用、模拟失焦/恢复、配置修复后重新启用均通过 |
| 瞄准状态 | 两 Model 同步、同值去重、System 释放退订及新上下文单份订阅通过 |
| 跳跃缓冲 | 缓冲消费回归通过；追加真实 TPC 场景注入已验证同帧移动/冲刺、按住跳跃跨落地不连跳、释放重按可再次跳 |
| 验证器清理 | 成功、断言失败、显式 Cancel 后均无测试夹具遗留；Cancel 返回 cleanupComplete=true，原背景运行设置恢复 |
| 编译与导入 | 最终类型已编译并可运行；期间 API/验证调度类型错误已修正，Unity 源资产时间戳竞争经重新导入消除 |
| 保存边界 | 输入修复阶段原 SampleScene 未改；追加 Audio 清理仅删除 28 行，其他 69 个场景块和场景 .meta 不变。输入资产 .meta、用户已有 Code Coverage 设置保留；新增脚本 .meta 完整，供应商与包无差异 |

这 150 项是项目 Editor 验证器的回归断言，不是 Unity Test Runner 的测试数量。Core 源码未改，本次输入修复没有重复运行上轮的 139 项 Core 测试。

验证中发现 Editor 与游戏的 Input System 读取上下文不同：Editor 更新回调读取的动作值可能为零，而游戏 performed 回调已提供非零值。因此 Look 断言改为在实际动作回调内捕获并比较其值，恢复绑定也延迟至游戏 Start/Update 采样；未降低 Look 不累加和停止归零的要求。

## 验收范围与证据边界

- 用户已作出整体恢复阶段验收确认；物理键鼠/窗口切换未提供逐项原始日志。窗口自动化工具曾两次因沙箱初始化失败无法运行，模拟焦点检查不冒充物理实测记录。
- 真实 TPC 同帧消费、跨落地跳跃、两次独立运行和音频播放状态均有程序化证据；主观恢复基线按本轮用户确认收口。瞄准姿态偏差未纳入通过结论。
- Audio 残留清理与复验已完成；恢复阶段 R2 按本轮用户整体确认通过。

## R2-AUDIO-01：Audio 对象误保存运行时组件

症状为 `The referenced script (Unknown) on this Behaviour is missing!`，发生在 SampleScene 加载/进入 Play 后、AudioBridge.Awake 日志之前。已从 Unity Console 内部记录的 instanceID 追溯所属对象，定位成功。

本轮已核对：

- 项目序列化资产：45 处脚本 GUID，无未解析项。
- SampleScene 全依赖：167 个依赖、49 处脚本引用，无未解析 GUID / 类型。
- 已加载场景 MonoBehaviour 的运行类型、MonoScript 类型与序列化脚本引用一致。
- Project/Controller 未发现空 m_Script 引用；场景无预加载资产或 AssetBundle 标签。
- 仅含相机和光照的临时空场景，PlayerController=0，运行到 9622 帧，Console 无错误/警告；随后恢复原 SampleScene，未保存临时场景。
- Console 记录的缺失组件 instanceID=44490，通过 InternalEditorUtility.GetGameObjectInstanceIDFromComponent 得到 owner=44480，即 **Audio**。实例 ID 仅为本次会话诊断值。
- Audio 的运行时组件列表确有一个 null 槽，同时又有一份正常的 QFramework.UnRegisterOnDestroyTrigger。
- 场景源码中 Audio 的 m_Component 包含本地 fileID **229206899**；这个 MonoBehaviour 引用内嵌 MonoScript **1162665300**，类名 UnRegisterOnDestroyTrigger、命名空间 QFramework、程序集 QFramework。它不是正常外部 .cs GUID 引用，所以早先的外部 GUID 扫描无法覆盖。
- AudioBridge.Start 的 UnRegisterWhenGameObjectDestroyed 会在运行时创建正常的辅助组件；场景中另存了一份同类运行时组件，进入 Play 时该保存项失效。当前证据把问题定位为**已序列化的 Audio 生命周期辅助组件残留**，与 Aim/Fire 绑定无关。
- 具体修复建议：非 Play Mode 下，经 Editor/MCP 仅移除 Audio 上这份误保存的组件，保存 SampleScene，检查差异仅涉及该组件及失去引用的内嵌脚本，再复验启动和音频。不修改 QFramework 源码，不批量删除组件。

历史 frameCount=1 的停帧观察在后续会话未持续复现：自动回归实际推进 243 帧，恢复背景设置后另一个采样已到 20068 帧。不能据此反推当时原因已经确定。

任务 5.5 的定位与问题登记已完成。用户随后明确授权在当前 change 清理，proposal/design/tasks 与 audio-system delta 已同步；新增 7.1–7.3 跟踪本次范围。原 IK 变更仍为 22/37，本次未修改它。

本轮只读场景观察曾记录 63479 个渲染帧，但观察期间没有收到移动/跳跃/冲刺/开火操作，因此不能据此完成真实键鼠验收。

## 2026-09-18 追加清理结果

- 用户明确授权将 R2-AUDIO-01 纳入当前变更，OpenSpec 新增 audio-system delta 和 7.1–7.3，并同步目标、非目标、设计与验收。
- 普通 Editor 缺失脚本计数为 0；组件数组直接编辑被 Unity 拒绝。已撤销失败尝试，确认场景无脏修改，将具体限制及受限例外写入设计 D8。
- 校验源场景与备份一致、两个本地 fileID 各只有预期引用后，仅删除一条组件引用及两个残留块，随即 AssetDatabase 导入并由 Editor 重载。
- diff：0 行新增、28 行删除。除 Audio 组件列表外，其余 69 个剩余场景块逐字不变；场景 .meta 未变。Audio 重载后为 Transform、AudioBridge、两份 AudioSource，无空槽。
- 最初运行复验曾因 MCP 返回“ping not answered”中断；用户聚焦 Unity 并确认连接后已恢复。该次未确认请求未计入通过记录，随后重新执行了下述两次可确认运行。

### 清理后的最终复验

| 项目 | 实际结果 |
|---|---|
| 第一次可确认 Play | Audio 有 2 个 AudioSource、0 个空槽、1 个正常运行辅助组件；模型保留 10 段脚步音与 Player_Land |
| 既有播放端 | 通过已有 AudioBridge 脚步/落地方法触发，两个 AudioSource 的 isPlaying 均为 true，无新增异常 |
| 实际 TPC 场景注入 | 游戏帧 2 → 1286；Move/Sprint 同帧消费成立，2 次起跳/2 次落地，最高离地约 1.345m；持续按住的第一次跳跃跨落地不连跳，释放重按才开始第二次 |
| 动画驱动音频 | 上述实际运动过程中观察到脚步和落地 AudioSource 播放；输入设备及临时背景设置均恢复 |
| 第二次独立 Play | 再次创建 1 个运行辅助组件、0 空槽；额外连续推进 180 帧（523 → 703） |
| 两次退出 | Audio 回到四个持久组件，没有保存运行辅助组件；最终非 Play、场景 isDirty=false |
| Console | 两次可确认运行及最终退出均 0 错误、0 警告，原 Missing Script 不再出现 |
| 文件核对 | 清理后仍只有 28 行删除，其他 69 个场景块逐字不变，场景 .meta 与第三方/依赖未改 |

这些是程序化场景证据。随后用户明确认可当前恢复基线，剩余阶段确认据此收口为 **21/21**；主 specs 已同步并归档，详细验收边界见下文。

## 2026-09-18 用户阶段验收与收口

用户明确确认“项目已经回到了当初暂停开发的那个阶段”，要求检查规范后勾选任务、同步文档、归档并提交推送。本 change 的阶段目标据此验收通过，21/21 项收口。

完成依据分开记录：150 个自动回归断言、真实 TPC 程序化场景验证、两次独立 Play/Stop 与音频检查，以及本轮用户对恢复基线的整体确认。本轮没有新增物理键鼠/窗口切换的逐项原始日志，不将模拟焦点消息表述为这类记录；用户整体阶段验收作为原 5.3/5.4 剩余人工确认的收口依据。

瞄准动画与枪口偏差是用户明确保留的后续事项，须另建 change。本次通过不表示 IK 姿态/手感已修好。原 aim-ik-rig-constraint 历史记录仍保留 22/37。

主规格同步覆盖 audio-system、player-input-model、player-system；归档路径为 [2026-09-18-restore-player-input-baseline](../openspec/changes/archive/2026-09-18-restore-player-input-baseline/tasks.md)。
