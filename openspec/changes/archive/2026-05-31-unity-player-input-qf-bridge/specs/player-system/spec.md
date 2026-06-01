## ADDED Requirements

### Requirement: PlayerSystem 订阅 PlayerInputModel.IsAiming 驱动瞄准状态

`PlayerSystem.OnInit()` SHALL 在获取 `PlayerModel` 引用后，额外获取 `PlayerInputModel` 引用，并订阅 `PlayerInputModel.IsAiming` 的 `Register` 回调：
- 当 `PlayerInputModel.IsAiming.Value` 变化时，自动调用 `this.SetAiming(newValue)`

此订阅 SHALL 使用 `UnRegisterWhenGameObjectDestroyed` 或等效的生命周期管理（注：System 层不继承 MonoBehaviour，需使用 Architecture 生命周期管理，或在适当时机 UnRegister）。

`SetAiming(bool)` 方法本身不变（写 `PlayerModel.IsAiming` + `SendEvent<AimStateChangedEvent>`）。

#### Scenario: PlayerInputModel.IsAiming 变化触发 PlayerSystem.SetAiming

- **WHEN** Play Mode 下 `PlayerInputModel.IsAiming.Value` 从 `false` 变为 `true`（由 `SetAimStateCommand` 写入）
- **THEN** `PlayerModel.IsAiming.Value` SHALL 随即变为 `true`，`AimStateChangedEvent` SHALL 被广播（AnimatorAimBridge / CameraAimBridge 响应）

#### Scenario: SetAimStateCommand 链路完整（保持 B1b.2 行为）

- **WHEN** Play Mode 下发出 `SendCommand(new SetAimStateCommand(true))`
- **THEN** 链路 `PlayerController → SetAimStateCommand → PlayerSystem.SetAiming → PlayerModel.IsAiming → AimStateChangedEvent → AnimatorAimBridge / CameraAimBridge` SHALL 完整触发，视觉效果与 B1b.2 一致
