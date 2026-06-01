# player-input-model Specification

## Purpose
TBD - created by archiving change unity-player-input-qf-bridge. Update Purpose after archive.
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

`Unomata.Gameplay` 命名空间 SHALL 定义 `PlayerController` 类，继承 `MonoBehaviour`，实现 `IController`，`DefaultExecutionOrder` 为 `-20`。

`PlayerController` SHALL 持有 `PlayerInput` 的引用，在 `Awake` 或 `Start` 中通过 `PlayerInput.actions` 订阅以下 Action 回调（Action Map 名称与 Action 名称须与 `UnomataPlayer.inputactions` 资产一致）：
- `Move`（PlayerCharacterControls Action Map）→ performed/canceled → `SendCommand<SetMoveInputCommand>(v2)`
- `Jump` → performed/canceled → `SendCommand<SetJumpInputCommand>(bool)`
- `Sprint` → performed/canceled → `SendCommand<SetSprintInputCommand>(bool)`
- `Aim`（新增 Action）→ performed/canceled → `SendCommand<SetAimStateCommand>(bool)`
- `Fire`（新增 Action，骨架）→ performed/canceled → `SendCommand<SetFireInputCommand>(bool)`

`PlayerController` SHALL 实现 `IBelongToArchitecture.GetArchitecture() => GameApp.Interface`。

#### Scenario: 禁用 PlayerController 后角色完全无响应

- **WHEN** 在 Play Mode 中通过 Inspector 将 `PlayerController` 组件 `enabled` 设为 false，然后按 WASD / 空格 / 右键
- **THEN** 角色 SHALL 完全无响应（`PlayerInputModel` 所有字段保持初始值，SAInputs 不更新，TPC 不驱动角色移动）

#### Scenario: Move 输入走 QF 链路

- **WHEN** Play Mode 下按 W 键（Move Action performed）
- **THEN** `SetMoveInputCommand` SHALL 被发出，`PlayerInputModel.Move.Value.y` SHALL 变为正值

#### Scenario: 瞄准切换走 QF 链路

- **WHEN** Play Mode 下按住鼠标右键（Aim Action performed）
- **THEN** `SetAimStateCommand(true)` SHALL 被发出，`PlayerInputModel.IsAiming.Value` SHALL 变为 `true`

---

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

### Requirement: SAInputAdapter 单向同步 Model 到 StarterAssetsInputs

`Unomata.Gameplay` 命名空间 SHALL 定义 `SAInputAdapter` 类，继承 `MonoBehaviour`，`DefaultExecutionOrder` 为 `-10`。

`SAInputAdapter` 的 `LateUpdate()` SHALL 在每帧将 `PlayerInputModel` 的字段值单向写入同 GameObject 上的 `StarterAssetsInputs` 公开字段：
- `_sai.move = _model.Move.Value`
- `_sai.jump = _model.Jump.Value`
- `_sai.sprint = _model.Sprint.Value`

`Look` 字段 SHALL NOT 经由 SAInputAdapter 写入（保持 SA 默认路由，PlayerInput 直接触发 `OnLook`）。

#### Scenario: SAInputAdapter 同步生效

- **WHEN** `PlayerInputModel.Move.Value = new Vector2(1, 0)` 被写入后的同帧 LateUpdate
- **THEN** `StarterAssetsInputs.move` SHALL 等于 `Vector2(1, 0)`，且 `ThirdPersonController` 驱动角色向右移动

#### Scenario: look 字段不经 SAInputAdapter 写入

- **WHEN** 检查 `SAInputAdapter.LateUpdate` 的实现
- **THEN** 代码中 SHALL NOT 存在对 `StarterAssetsInputs.look` 的写操作

---

### Requirement: UnomataPlayer.inputactions 资产

`Assets/_Project/Settings/UnomataPlayer.inputactions` SHALL 通过 `AssetDatabase.CopyAsset` 从 StarterAssets 原 `.inputactions` 文件复制而来，在副本上新增：
- `Aim`：Button 类型，Binding `<Mouse>/rightButton`，位于 `PlayerCharacterControls` Action Map
- `Fire`：Button 类型，Binding `<Mouse>/leftButton`，位于 `PlayerCharacterControls` Action Map

`PlayerArmature` 的 `PlayerInput` 组件 `Actions` 字段 SHALL 改引用 `UnomataPlayer.inputactions`（不再引用 SA 原文件）。`PlayerInput.defaultActionMap` 应保持 `PlayerCharacterControls`。

SA 原 `.inputactions` 文件 SHALL NOT 被修改（`git status -- Assets/ThirdParty/` 输出为空）。

#### Scenario: 副本资产存在

- **WHEN** 在 Unity Editor Project 视图浏览 `Assets/_Project/Settings/`
- **THEN** 存在 `UnomataPlayer.inputactions` 资产，且 .meta GUID 与 SA 原文件不同

#### Scenario: Aim 和 Fire Action 可读取

- **WHEN** 通过 `PlayerInput.actions.FindAction("Aim")` 查找 Action
- **THEN** 返回非 null Action，Binding count >= 1（包含 `<Mouse>/rightButton`）

---

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

