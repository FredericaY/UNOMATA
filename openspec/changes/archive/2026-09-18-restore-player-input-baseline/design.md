## Context

动机与范围见 [proposal.md](proposal.md)。本文依据 2026-09-18 磁盘代码核对及 [首次恢复记录](../../../../Docs/ENVIRONMENT_RECOVERY.md)，这部分记录规划时的事实；实际实施与验收结果见 Docs/INPUT_BASELINE.md。

| 已观察事实 | 对实现的约束 |
|---|---|
| inputactions 只有 Player/Move、Look、Jump、Sprint；Sprint=PassThrough | 在现有 Player Map 补动作，不重命名 Map；按值处理 Sprint 释放 |
| 历史主规格写 PlayerCharacterControls，归档 tasks 实际写 Player | delta 明确修正契约，不改写归档 |
| PlayerController 在 Start 订阅、OnDisable 退订，Aim/Fire 无空检查 | 要有可重复的绑定/清理流程，先整体校验再订阅 |
| SetAimStateCommand 直接调 PlayerSystem.SetAiming；PlayerSystem 又订阅 InputModel.IsAiming | 原链路未把输入写入 Model；改为一次写入、单路传播 |
| SAInputAdapter 在 Start 绑定 Look、仅 OnDestroy 退订，在 LateUpdate 同步业务输入 | 启停时清理 Look，运动缓冲在 TPC.Update 前准备 |
| TPC.Update 消费 Jump，空中将缓冲 jump=false | 适配器不能用仍为 true 的 Model 每帧重新填充已消费请求 |
| QFramework AbstractSystem 实际提供 OnDeinit，GameApp 无应用级关闭包装 | 使用已有 System 钩子，不引入不存在的 Shutdown API |
| 当前自有验证只有 Gameplay/Tests/QFrameworkValidator，无项目测试 asmdef | 使用 Editor 专用可重复验证入口，不为本修复重构全部程序集 |

本机 Input System 1.14.2 的 Documentation~/ActionsEditor.md 说明 PassThrough 主要提供值而非完整阶段信息；本提案不靠 performed 等同按下来处理 Sprint。

## Goals / Non-Goals

**Goals:** 让项目保存的配置、每次启停的动作实例和两个瞄准状态视图保持一致；针对释放、生命周期、错误配置建立可重复验证，形成继续 IK 的可靠输入起点。

**Non-Goals:** 延续 proposal 的范围；不改变运动/跳跃物理参数，不把 Look 纳入业务 Model，不调整 Strafe 或 IK，不新增运行时测试依赖或全局单例，除 D8 中已经定位并获准清理的 Audio 残留外，不扩展到其他资产问题。

## Decisions

### D1 — 修复源资产而非运行时补动作

在现有 UnomataPlayer.inputactions 的 Player Map 增加两个 Button，并配置 KeyboardMouse 分组；原四个动作的 ID、类型、处理器、绑定及 controlSchemes 不变。用 Editor/MCP 和 Input System 支持的资产保存/重导入流程持久化，核对磁盘 JSON、导入资产和 PlayerInput 运行时实例三个视角。

不重新 CopyAsset 产生新 GUID，不修改供应商资产，也不在 Awake 临时 AddAction 以掩盖源文件缺失。输入接线仅在确有错误时保存；已批准的 Audio 清理另按 D8 保存精确场景差异。

### D2 — 绑定与清理具有对称生命周期

PlayerController 与 SAInputAdapter 各自维护已解析引用、实际订阅的动作实例和绑定状态。启用时执行幂等初始化/绑定；PlayerInput 若尚未生成其实际动作实例，使用首次 Start/有限的就绪重试完成首次绑定，不能在 OnEnable 永久绑定供应商资产或随后被替换的旧实例。监听输入源启停/实例变化，防止 Map 或 PlayerInput 停用后组件仍接受旧回调。

Controller 校验必要对象、Player Map、五个业务动作（Move=Vector2、其余按钮可读）及输入资产所需 Look；收集全部缺项，失败不注册半套业务回调。适配器校验其本地 StarterAssetsInputs 与 Look。每次失败尝试输出一次含对象与缺项的诊断，正常每帧不刷错。

禁用/销毁的公共清理路径幂等、逐项安全退订，仅清理已初始化且仍有效的上下文；缓存已有 Architecture 引用与绑定状态，禁止在清理阶段重新访问惰性入口来创建 GameApp。输入字段复位沿 Command 写入，不由 Controller 或 View 直接写业务 Model。必要的“复位输入”聚合命令只负责已有字段清零，不承载新玩法。

不采用给每行加 null 判断后继续运行的方案，因为它会留下看似可用的部分输入。重新启用不得重复订阅；首次未 Start 就禁用和初始化失败后的销毁均是必测边界。

### D3 — 瞄准只有一条数据路径

```text
PlayerInput.Player/Aim
 → PlayerController
 → SetAimStateCommand
 → PlayerInputModel.IsAiming（权威输入）
 → PlayerSystem（一次有效订阅）
 → PlayerModel.IsAiming（业务镜像）+ AimStateChangedEvent
 → 原有 AnimatorAimBridge / CameraAimBridge / StrafeController
```

SetAimStateCommand 只写 InputModel。PlayerSystem 持有 IUnRegister，初始化同步当前值，OnDeinit 释放订阅；SetAiming 保持同步业务镜像的角色，对无状态变化的调用不再次广播。不得在命令和 System 同时发相同事件。

这样禁用/失焦复位与正常松开走同一路径，既能保持既有消费者接口，也能用事件计数识别重复订阅。无需更改 PlayerModel / PlayerInputModel 的公开字段，也不改 HP、Wave 或 Audio 业务。

### D4 — 适配器尊重当前帧消费与按钮边沿

SAInputAdapter 仍为只读 Model 的本地表现/第三方适配层，使用 Update(-10) 在 TPC.Update(默认顺序) 前写 move/sprint。用有帧号的输入和消费记录验证次序，不能以 DefaultExecutionOrder 属性本身作为通过证据。

Jump 的 Model 表示按钮状态，下游 SA jump 是可消费的请求缓冲：适配器只在新的按下边沿置 true，允许 TPC 自行消费；释放清零。禁用/失焦时清除缓冲和边沿状态，恢复时对尚未释放的 Jump/Fire 加释放门槛，避免把旧 held 状态重新解释为一次按下。新按下仍遵守 TPC 原有落地与超时规则，不添加空中跳或跳跃排队。

Sprint 保留原 PassThrough 配置，回调读真实按钮值；取消/归零均写 false。不通过将所有 performed 都当 true 处理释放。

Look 保留原有适配器直通路径，成对订阅/退订；不叠加已累计的鼠标 delta。单独禁用业务 Controller 只禁止其五个业务意图，不把相机 Look 当作它已验证的禁止范围；适配器自身、PlayerInput/Map 停用或失焦时则清零 Look。

### D5 — 恢复策略与场景状态

本修复采用“失效先清零、恢复重新采样”的策略：Move/Sprint/Aim/Look 可根据恢复后的有效采样恢复；Jump/Fire 须观察释放和新按下，不能重放失效前请求。这是本 change 的明确恢复行为，不宣称原代码已实现。

失焦时不修改全局 Input System 设置来规避测试，不永久打开 runInBackground；根据当前组件可见的焦点与输入源状态禁用接收并复位。实际运行中动作实例替换后退订旧实例，绑定当前实例，维持一个写入者。

### D6 — 验证入口独立于正式场景

在 `Assets/_Project/Editor/Validation/` 放置可显式运行的输入基线验证入口（Editor 程序集）；诊断与输入注入仅用于本 change 的回归，不作为业务入口，不由 SampleScene 默认执行。临时对象/动作副本用可清理的测试会话管理，退出和异常均释放设备、订阅、对象及临时场景。避免引入新的运行时 asmdef 或升级测试包。

验证优先经 Input System 虚拟设备/动作事件穿过 PlayerController，记录实际 Model、SA 缓冲、事件数和帧号；不得只调用命令/写 Model 然后宣布输入链路通过。单独检查 SetAimStateCommand 的测试可直接发命令，因为其断言就是命令到 Model/System 的边界。

| 验证层 | 必须证明 |
|---|---|
| 资产 | 六动作、绑定与分组、原 GUID/绑定保留、重导入及独立 Editor 重载后持久化 |
| 故障注入 | 缺 PlayerInput / 资产 / Map / Aim / Fire / 不兼容动作类型，均无部分订阅；清理无异常 |
| 正常输入 | 按下/松开 Move/Jump/Sprint/Aim/Fire，Look 单一路由、鼠标停止不残留 |
| 时序与消费 | 输入更新后同帧 TPC 消费正确缓冲；一次持续按住 Jump 跨落地不自动连跳 |
| 生命周期 | 首次禁用、三次组件启停、PlayerInput/Map 停用、失焦外释放、重复 Play 会话和销毁 |
| 瞄准状态 | InputModel 与 PlayerModel 始终一致，true/true/false/false 只产生两个转换事件，System 释放后无旧订阅 |
| 场景冒烟 | 连续至少 120 个实际游戏帧，移动/跳跃/冲刺、相机进入/退出、动画权重、脚步/落地触发，无新增输入异常 |

至少两个独立 Play 会话，并在 SampleScene 实测键鼠操作。现有手柄 Move/Look/Jump/Sprint 可用虚拟设备做非回退检查；不把此结果称为新手柄 Aim/Fire 支持或真实手柄手感验收。画面/音频主观质量仍需明确记录其手动观察边界。

### D7 — 未知 Console 错误与输入验收分开归因

保留恢复记录中的 Missing Script 日志和 frameCount=1 快照。修复输入后，按“启动前 Console 快照 → 场景/资源加载 → 首次帧 → 停止”定向复现，记录对象/资产、调用栈及帧推进。

若证据定位为本输入资产/组件引发，则在既定输入范围内处理；已定位的 Audio 残留经用户确认纳入本次处理（D8）。其他资产/包问题仍记录独立入口。不能无依据批量删除组件，不能清空 Console 后声称原错误消失。仍无法归因、或错误阻断实际输入测试时，相关验收任务保持未完成；即使隔离输入测试通过，SampleScene 的整体 R2 也不能因此被勾选为完成。

### D8 — 已批准的 Audio 精确清理

Console 的组件标识经 Unity 归属查询定位到 Audio。原场景的 MonoBehaviour fileID=229206899 引用内嵌 MonoScript fileID=1162665300，类型为 QFramework.UnRegisterOnDestroyTrigger。运行时该保存项成为 null 槽，AudioBridge.Start 又通过现有订阅扩展创建正常辅助组件，形成一份无效保存项和一份正常运行实例。

用户已明确要求顺便修复并同步产物。非 Play Mode 下，先确认场景无未保存修改并记录备份，通过 Editor/Undo 精确删除这个保存项；不修改 QFramework 或 AudioBridge 的播放代码。保存后比较场景差异，保留 AudioBridge、两个 AudioSource、全部 clips / Animator 引用和其他场景内容。Editor 保存可自然清除无引用的内嵌 MonoScript。

已实测的 Editor 限制：该内嵌引用在组件列表表现为 null，但 GameObjectUtility 的 missing 数为 0；SerializedObject 删除槽被 Unity 拒绝（It is not allowed to modify the data property），重载亦不能恢复可操作组件。撤销本轮失败尝试并确认场景无脏修改后，按 rules 的明确例外采用受限文本清理：校验场景与备份一致、两个本地 fileID 各只被预期位置引用，只移除 Audio 的一条组件引用、229206899 组件块、1162665300 内嵌 MonoScript 块；随后 AssetDatabase 导入并由 Editor 重新加载/验证。任何非预期差异立即停止，不扩大删除范围。

重新加载场景，再执行两次独立 Play/Stop，检查缺失脚本日志消失、运行辅助组件重新建立以及退出后不残留。程序化调用既有脚步/落地播放端并读取播放状态，验证功能不回退；主场景真实键鼠/听感验收仍按未完成任务保留。

## Risks / Trade-offs

- [初始化时序与动作克隆] → 从实际 PlayerInput 实例绑定，覆盖初次启用与重载，不依靠一次 Start。
- [清理回调再次触发状态写入] → 先标记不接收、退订，再复位；同值写入幂等。
- [输入适配改为 Update 后暴露 Jump 重复注入] → 不保持无条件复制 bool，按按钮边沿向可消费缓冲投递。
- [验证工具改变焦点/设备] → 记录真实帧推进，测试状态可回滚；把程序化注入与真实键鼠观察分别记录。
- [未解释错误导致误报通过] → 分离输入专项通过与整体 R2 通过，保留失败证据。
- [与活动 IK 的协作] → 本 change 不改 character-controller delta、Rig 或 Strafe 参数，输入恢复后再续 IK。

## Migration Plan

实施前记录工作区与相关源文件/GUID。先恢复输入资产，再实现绑定/清理与瞄准链路、适配器语义；编译后完成隔离回归与真实场景验收，最后同步 Docs/INPUT_BASELINE.md、ENVIRONMENT_RECOVERY、ARCHITECTURE、TODO 和双语 README。

主规格在真实验收后按输入、玩家状态与音频三个 delta 同步；旧归档不重写。回退时只还原本 change 改动的项目源文件与必要场景字段，保留用户原有修改、旧计划整理和 IK 工作；不切换回供应商 SendMessages 作为隐藏替代。

## Open Questions

Missing Script 的来源已定位并按 D8 处理。此前 frameCount=1 的历史成因仍不作无依据推断，后续已观察到持续帧推进；真实场景验收按实际结果完成。

## 最终阶段验收（2026-09-18）

用户明确接受恢复到暂停前开发基线，要求本 change 收口。验收依据是已记录的自动/程序化场景结果与用户整体阶段确认；原人工细目的收口依据为该确认，不新增未提供的物理输入日志。瞄准动画/枪口偏差被明确保留为后续独立 change 的目标。
