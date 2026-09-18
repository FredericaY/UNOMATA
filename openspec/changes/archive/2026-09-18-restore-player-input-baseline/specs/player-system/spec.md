## MODIFIED Requirements

### Requirement: PlayerSystem 订阅 PlayerInputModel.IsAiming 驱动瞄准状态

瞄准输入 SHALL 以 PlayerInputModel.IsAiming 为唯一权威输入值。`SetAimStateCommand(bool)` SHALL 先写该值；PlayerSystem 初始化时 SHALL 同步已有输入值并订阅变化，以更新 PlayerModel.IsAiming 并广播 AimStateChangedEvent。该命令 SHALL NOT 同时直接调用 PlayerSystem.SetAiming 或另发事件，避免一份意图产生双路通知。

每次实际瞄准状态转换 SHALL 产生一次匹配的新状态事件；同值重复写入 SHALL 不产生额外转换事件。输入复位 SHALL 经相同链路退出瞄准。SetAiming 的状态同步行为及动画/相机消费者接口 SHALL 保持兼容；System 订阅 SHALL 随其架构生命周期释放，不持有场景 MonoBehaviour。

#### Scenario: PlayerInputModel.IsAiming 变化触发 PlayerSystem.SetAiming
- **WHEN** SetAimStateCommand 将 PlayerInputModel.IsAiming 从 false 改为 true
- **THEN** PlayerModel.IsAiming SHALL 随即为 true，并恰好广播一次 AimStateChangedEvent(true)，现有动画和相机消费者可响应

#### Scenario: SetAimStateCommand 链路完整（保持 B1b.2 行为）
- **WHEN** 正常启用时发出 SetAimStateCommand(true)，随后发出 false
- **THEN** 链路 SHALL 为 PlayerController → Command → PlayerInputModel → PlayerSystem → PlayerModel / AimStateChangedEvent → 动画/相机；进入和退出转换正常，不要求本 change 解决未验收的 IK 枪口姿态

#### Scenario: 同值写入不重复广播
- **WHEN** 连续写入 true、true、false、false，初始值为 false
- **THEN** 观察者 SHALL 仅收到一次进入和一次退出事件，两个 Model 的值始终一致

#### Scenario: 输入清理退出瞄准
- **WHEN** 玩家正在瞄准时发生输入失效并复位
- **THEN** 两个 Model 的瞄准值 SHALL 均为 false，发生一次退出事件；相机和动画按已有过渡规则退出，不残留上一轮状态

#### Scenario: 系统释放与重新初始化
- **WHEN** 当前架构按已有生命周期释放，随后建立独立的新上下文
- **THEN** 旧 System 的输入订阅 SHALL 已注销，新上下文只有一份有效订阅；不得以重新创建架构完成旧对象清理
