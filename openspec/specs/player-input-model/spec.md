# player-input-model Specification

## Purpose
定义玩家业务输入的状态所有权、Command 写入、第三方运动适配与瞄准转向边界，覆盖移动、跳跃、冲刺、开火和瞄准，作为输入实现及后续生命周期复验的契约。

## Requirements
### Requirement: PlayerInputModel 数据结构

`Unomata.Gameplay` 命名空间 SHALL 定义 `PlayerInputModel` 类，继承 `AbstractModel`，包含以下公开属性，全部使用 `BindableProperty<>`：
- `BindableProperty<Vector2> Move`：移动输入，初始值 `Vector2.zero`
- `BindableProperty<bool> Jump`：跳跃输入，初始值 `false`
- `BindableProperty<bool> Sprint`：冲刺输入，初始值 `false`
- `BindableProperty<bool> IsAiming`：瞄准输入，初始值 `false`
- `BindableProperty<bool> Fire`：射击输入，初始值 `false`

`PlayerInputModel` SHALL NOT 包含任何业务逻辑，仅存储输入状态数据。`GameApp.Init()` SHALL 在 `PlayerModel` 之后注册 `PlayerInputModel`（`RegisterModel<PlayerInputModel>(new PlayerInputModel())`）。

#### Scenario: PlayerInputModel 初始值正确

- **WHEN** `GameApp` 初始化，进入 Play Mode
- **THEN** `PlayerInputModel.Move.Value` SHALL 等于 `Vector2.zero`，`Jump.Value` / `Sprint.Value` / `IsAiming.Value` / `Fire.Value` SHALL 均为 `false`

#### Scenario: 任何 BindableProperty 可写读并触发回调

- **WHEN** 对 `PlayerInputModel.Move` 赋新值（如 `Move.Value = Vector2.right`）
- **THEN** 再次读取 `Move.Value` SHALL 返回 `Vector2.right`，且注册的回调 SHALL 被触发

---

### Requirement: PlayerController 接管 PlayerInput 回调

`PlayerController` SHALL 保持项目的 QFramework Controller 入口与执行顺序 -20，通过当前 `PlayerInput` 所使用的 **Player** Action Map 接收业务输入，并将 Move、Jump、Sprint、Aim、Fire 分别交给既有输入 Command。业务输入状态 SHALL 由 `PlayerInputModel` 保存；Fire 在本阶段仅表示输入，不产生射击。

启用接收前 SHALL 验证 PlayerInput、其动作资产、Player Map 及所需动作存在且类型兼容。验证失败时 SHALL 不留下部分业务订阅，五个业务输入值保持中立，报告含对象、资产/Map 和缺项名称的可操作错误；禁用或销毁 SHALL 不因此抛出空引用。配置恢复后重新启用 SHALL 能恢复输入，每个动作变化只处理一次。

Move SHALL 反映方向值，Jump/Sprint/Aim/Fire SHALL 反映真实按下或释放状态；SHALL NOT 将所有 performed 回调无条件解释为 true，尤其必须覆盖现有 PassThrough Sprint 的归零事件。

#### Scenario: 禁用 PlayerController 后角色完全无响应
- **WHEN** 在 Play Mode 按住移动/冲刺/瞄准/开火后禁用 PlayerController，再按 WASD / 空格 / 右键 / 左键
- **THEN** 五个业务输入 SHALL 归零并保持中立，不产生新的移动、跳跃、瞄准或开火意图；这里的无响应指这些业务输入，Look 相机直通由其独立适配器生命周期控制，不声称重力或所有动画静止

#### Scenario: Move 输入走 QF 链路
- **WHEN** 正常启用时按 W 并随后释放
- **THEN** Move Command SHALL 使 PlayerInputModel.Move 的 y 为正，并在释放后归零

#### Scenario: 瞄准切换走 QF 链路
- **WHEN** 正常启用时按住并释放鼠标右键
- **THEN** SetAimStateCommand SHALL 使 PlayerInputModel.IsAiming 先为 true 后为 false，后续状态与事件按 player-system 契约变化

#### Scenario: 缺少必要引用或动作
- **WHEN** PlayerInput/动作资产引用缺失、Player Map 不存在，或任一必要动作缺失/类型不兼容
- **THEN** 业务输入 SHALL 保持中立，没有部分输入回调残留，每次失败的绑定尝试只报告一条包含完整缺项的诊断，不每帧刷错；随后禁用/销毁不抛异常

#### Scenario: 冲刺和开火释放
- **WHEN** Shift 按下后释放并产生当前 PassThrough Sprint 的归零回调，或鼠标左键按下后释放
- **THEN** 对应 Sprint/Fire 状态 SHALL 从 true 回到 false，不能因回调仍处于 performed 阶段而卡在 true；Fire 不调用武器或伤害逻辑

### Requirement: SetMoveInputCommand / SetJumpInputCommand / SetSprintInputCommand / SetFireInputCommand

`Unomata.Gameplay` 命名空间 SHALL 在 `Assets/_Project/Scripts/Gameplay/Commands/` 目录下定义以下 4 个命令类，均继承 `AbstractCommand`：
- `SetMoveInputCommand`：构造器接收 `Vector2 move`；`OnExecute` 写 `this.GetModel<PlayerInputModel>().Move.Value = move`
- `SetJumpInputCommand`：构造器接收 `bool jump`；`OnExecute` 写 `PlayerInputModel.Jump.Value`
- `SetSprintInputCommand`：构造器接收 `bool sprint`；`OnExecute` 写 `PlayerInputModel.Sprint.Value`
- `SetFireInputCommand`：构造器接收 `bool fire`；`OnExecute` **骨架空实现**（写 `PlayerInputModel.Fire.Value`，实际射击逻辑 B2a 填充）

#### Scenario: SetMoveInputCommand 写入 Model

- **WHEN** 通过 `this.SendCommand(new SetMoveInputCommand(Vector2.up))` 发出命令
- **THEN** `PlayerInputModel.Move.Value` SHALL 等于 `Vector2.up`，Console 无红错

#### Scenario: SetFireInputCommand 骨架不抛异常

- **WHEN** 通过 `this.SendCommand(new SetFireInputCommand(true))` 发出命令
- **THEN** Console SHALL 无红色错误，`PlayerInputModel.Fire.Value` SHALL 变为 `true`

---

### Requirement: UnomataPlayer.inputactions 资产

`Assets/_Project/Settings/UnomataPlayer.inputactions` SHALL 继续作为项目自有的 StarterAssets 输入副本，保留现有 GUID 和 **Player** Map。该 Map SHALL 持久包含 Move、Look、Jump、Sprint、Aim、Fire；原有动作及绑定 SHALL 保留，新增 Aim/Fire 均为 Button，分别绑定 `<Mouse>/rightButton` / `<Mouse>/leftButton`，参与 KeyboardMouse 控制方案。

PlayerArmature 的 PlayerInput SHALL 使用该项目资产、defaultActionMap=Player、InvokeCSharpEvents。SHALL NOT 为修复旧规格里的 PlayerCharacterControls 名称而重命名实际 Player Map。修改 SHALL 保存至源资产并可经重导入/重新打开工程恢复，不能依赖仅存活于内存的动作实例。第三方输入资产 SHALL 不被修改，本 change 不添加手柄 Aim/Fire。

#### Scenario: 副本资产存在
- **WHEN** 检查项目输入资产和它的元数据
- **THEN** 项目副本 SHALL 存在、GUID 与 StarterAssets 原件不同，且保持本次修改前的项目副本 GUID

#### Scenario: Aim 和 Fire Action 可读取
- **WHEN** 从 PlayerInput 当前动作实例查找 Player/Aim 与 Player/Fire
- **THEN** 两者 SHALL 均非 null、为 Button，并分别包含右键和左键绑定且可在 KeyboardMouse 下触发

#### Scenario: 重导入后仍完整
- **WHEN** 保存并强制重导入输入资产，再关闭/重开工程或执行等效的独立 Editor 重载，重新进入场景
- **THEN** 六个动作、绑定和默认 Map SHALL 仍正确，启动与退出均不出现因缺少 Aim/Fire 导致的空引用

#### Scenario: 已有输入配置不回退
- **WHEN** 对比输入资产修复前后
- **THEN** Move/Look/Jump/Sprint 的动作标识、现有绑定和控制方案 SHALL 保留，第三方资产与元数据不变

### Requirement: StrafeController 瞄准下半身朝向（死区迟滞 TPS 模型）

`Unomata.Gameplay` 命名空间 SHALL 定义 `StrafeController` 类，继承 `MonoBehaviour`，实现 `IController`，`[DefaultExecutionOrder(10)]`（在 TPC LateUpdate 之后覆盖朝向），并持有 `PlayerInputModel` 引用以判断移动/静止。

`StrafeController.LateUpdate()` SHALL 在 `PlayerInputModel.IsAiming.Value == true` 时按以下规则控制角色下半身（整体 transform）绕世界 Y 轴的 Yaw：

1. **进入瞄准当帧**：`transform.rotation` SHALL snap 对齐 `Camera.main` 的 Yaw（一次性，上下半身整体对齐）。
2. **保持瞄准 + 移动**（`PlayerInputModel.Move.Value.sqrMagnitude` 大于移动阈值）：`transform.rotation` SHALL 锁定相机 Yaw（无死区），使 strafe BlendTree 的 MoveX/Y 方向正确。
3. **保持瞄准 + 静止**（`Move ≈ 0`）：设 `delta = Mathf.DeltaAngle(身体Yaw, 相机Yaw)`：
   - 若 `|delta| ≤ 死区阈值`（`SerializeField`，默认 10°）：`transform.rotation` SHALL 保持不变（脚定住）。
   - 若 `|delta| > 死区阈值`：身体 Yaw SHALL 以可配置角速度（`SerializeField`，`Mathf.MoveTowardsAngle`）平滑追向相机 Yaw。
4. `IsAiming == false` 时 SHALL 不干预 transform（TPC 默认控制朝向）。

死区阈值与追身角速度 SHALL 暴露为 `SerializeField` 以便运行时调参。`StrafeController` SHALL 在覆盖 `transform.rotation` 后恢复 `PlayerCameraRoot` 的世界 rotation（防止破坏 TPC 的 Cinemachine 相机目标，避免抖动）。

#### Scenario: 进入瞄准瞬间下半身 snap 对齐相机

- **WHEN** 角色朝向与相机 Yaw 相差较大时按下右键进入瞄准
- **THEN** 当帧角色整体 SHALL 立刻转向相机 Yaw（无平滑过渡）

#### Scenario: 静止瞄准小幅转视角时脚不动（上半身扭腰）

- **WHEN** 瞄准且不移动，水平转动相机但幅度在死区阈值内
- **THEN** 角色下半身（transform Yaw）SHALL 保持不动，仅上半身（`AnimatorAimBridge` yawOffset）跟随相机扭转

#### Scenario: 静止瞄准大幅转视角时下半身追身

- **WHEN** 瞄准且不移动，水平转动相机超过死区阈值
- **THEN** 角色下半身 SHALL 以配置角速度平滑转向相机 Yaw，残差收敛回死区内

#### Scenario: 瞄准移动时下半身锁相机不转身

- **WHEN** Play Mode 下长按鼠标右键进入瞄准，然后按 A（横移输入）
- **THEN** 角色身体 SHALL 保持朝向相机方向（不转身），仅横向位移，播放 `AimWalk_FL/FR/BL/BR` 等侧移动画

#### Scenario: 非瞄准时移动正常转身

- **WHEN** Play Mode 下不按右键（未瞄准），按 WASD
- **THEN** 角色 SHALL 正常朝向移动方向转身（TPC 默认行为未受影响）

#### Scenario: 瞄准移动中 B1b.2 视觉验收不退化

- **WHEN** Play Mode 下瞄准并移动，观察上半身动画与相机行为
- **THEN** UpperBodyAim Layer 权重 SHALL 为 1，瞄准持枪动画 SHALL 正确叠加；双相机切换 SHALL 正常（PlayerAimCamera Priority=15）；Console 零红错

### Requirement: StarterAssets 输入消费与 Look 直通

运动适配层 SHALL 在第三方运动控制器消费当前帧输入之前提供 Model 的 Move/Sprint，并把 Jump 按下转换为一次可被消费的请求。Jump 消费后，SHALL NOT 因输入按钮仍按住而在每帧重新写入 true；释放再按下才能产生新请求。适配层 SHALL 不反向改写权威输入 Model，也不修改供应商运动控制器。

Look SHALL 由适配器接收当前 PlayerInput 实例的 Player/Look，作为相机表现输入直通 StarterAssetsInputs.look，不进入业务 Model，不恢复 SendMessages 或建立第二个写入入口。回调 SHALL 写入当前值而非重复累加鼠标位移，释放/取消或适配器停用时清零。适配器自身禁用、重新启用与销毁 SHALL 成对管理订阅并清理下游缓冲。

#### Scenario: SAInputAdapter 同步生效
- **WHEN** 当前帧输入更新已将 Move 写为 Vector2(1,0)
- **THEN** 第三方运动控制器在该帧消费时 SHALL 读取到该方向，不能额外等待一帧 LateUpdate；适配器不改写 Model

#### Scenario: 已消费跳跃不被重复注入
- **WHEN** 玩家按住空格完成一次跳跃，第三方控制器已消费/清除请求，直到角色再次落地都未松开
- **THEN** 适配器 SHALL 不重复补发跳跃；释放并再次按下后能产生新的跳跃请求，不改变既有重力/落地规则

#### Scenario: Look 只有一个有效路由
- **WHEN** InvokeCSharpEvents 下输入鼠标位移，再停止位移或取消动作
- **THEN** Look SHALL 经适配器到达相机输入缓冲并在停止后归零，不依赖 StarterAssets 的 SendMessages 回调，不增加重复鼠标位移

#### Scenario: 适配器禁用与恢复
- **WHEN** 持续输入时禁用适配器，再重新启用并提供新输入
- **THEN** 禁用时下游 move/look/jump/sprint SHALL 归零且不再被旧回调写入，恢复后每个输入只处理一次，无对象销毁后的回调异常

### Requirement: 输入生命周期清理与恢复

业务输入接收者在禁用、输入源停用（PlayerInput 或当前 Map）、失焦、销毁或离开当前 Play 会话时 SHALL 释放自己的订阅并将其拥有的业务输入状态清为中立。Look 适配器 SHALL 对相同输入源失效和自身生命周期清理相机缓冲。SHALL NOT 为清理未初始化对象而创建新的 GameApp 或重启架构。

恢复到有效输入源后 SHALL 可重新接收输入而不叠加订阅；Jump/Fire 不得重放失效前的按下请求，须先观察释放及新的按下；Move/Sprint/Aim/Look 根据恢复后的有效采样处理，不能取用旧缓存。初始化前即禁用的组件也 SHALL 能在首次启用后工作。

#### Scenario: 连续禁用和重新启用
- **WHEN** 对 Controller 与适配器分别执行三次启用/禁用循环，再释放并重新按下各输入
- **THEN** 输入 SHALL 正常工作，一次输入变化只有一次相应处理，不累积回调；每次禁用后无残留业务或相机输入

#### Scenario: 按住输入时停用 PlayerInput 或 Map
- **WHEN** 按住输入时停用 PlayerInput 或 Player Map，随后重新启用
- **THEN** 状态 SHALL 清为中立，停用期间不接收业务输入；恢复后可接收新的操作，Jump/Fire 不重放旧请求

#### Scenario: 失焦与恢复
- **WHEN** 按住移动、瞄准或开火时切走窗口，在窗口外释放，再返回
- **THEN** 失焦时业务和 Look 缓冲 SHALL 归零；返回后不残留移动/瞄准/开火，释放并重新输入可正常响应

#### Scenario: 首次未启用与安全销毁
- **WHEN** 组件在 Start 之前被禁用、绑定失败后销毁，或正常运行后停止 Play Mode
- **THEN** 清理 SHALL 不抛异常、不隐式创建架构；首次真正启用可绑定，后续独立 Play 会话从中立输入开始
