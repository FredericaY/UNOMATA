## MODIFIED Requirements

### Requirement: PlayerController 接管 PlayerInput 回调

`PlayerController` SHALL 保持项目的 QFramework Controller 入口与执行顺序 -20，通过当前 `PlayerInput` 所使用的 **Player** Action Map 接收业务输入，并将 Move、Jump、Sprint、Aim、Fire 分别交给既有输入 Command。业务输入状态 SHALL 由 `PlayerInputModel` 保存；Fire 只表示输入意图，输入回调 SHALL 不直接执行射线、扣血或播放枪声，由独立射击系统在最终当帧姿态就绪后消费。

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
- **THEN** 对应 Sprint/Fire 状态 SHALL 从 true 回到 false，不能因回调仍处于 performed 阶段而卡在 true；Fire 输入回调不直接调用武器或伤害逻辑，射击系统读取释放状态后停止后续发射

### Requirement: SetMoveInputCommand / SetJumpInputCommand / SetSprintInputCommand / SetFireInputCommand

`Unomata.Gameplay` 命名空间 SHALL 在 `Assets/_Project/Scripts/Gameplay/Commands/` 目录下保留以下 4 个命令类，均继承 `AbstractCommand`：
- `SetMoveInputCommand`：构造器接收 `Vector2 move`；`OnExecute` 写 `PlayerInputModel.Move.Value = move`
- `SetJumpInputCommand`：构造器接收 `bool jump`；`OnExecute` 写 `PlayerInputModel.Jump.Value`
- `SetSprintInputCommand`：构造器接收 `bool sprint`；`OnExecute` 写 `PlayerInputModel.Sprint.Value`
- `SetFireInputCommand`：构造器接收 `bool fire`；`OnExecute` 写 `PlayerInputModel.Fire.Value`，SHALL NOT 执行射击规则、射线、伤害或声音；射击由独立业务系统消费输入

#### Scenario: SetMoveInputCommand 写入 Model
- **WHEN** 通过 `this.SendCommand(new SetMoveInputCommand(Vector2.up))` 发出命令
- **THEN** `PlayerInputModel.Move.Value` SHALL 等于 `Vector2.up`，Console 无红错

#### Scenario: SetFireInputCommand 骨架不抛异常
- **WHEN** 通过 `this.SendCommand(new SetFireInputCommand(true))` 发出命令
- **THEN** Console SHALL 无红色错误，`PlayerInputModel.Fire.Value` SHALL 变为 `true`；单独执行该输入写入 SHALL 不直接产生射击或伤害
