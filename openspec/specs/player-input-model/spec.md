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

角色瞄准下半身朝向 SHALL 由唯一运动/朝向所有者在最终姿态求值前确定，不再要求在第三方控制器之后 LateUpdate 覆盖旋转。进入瞄准 SHALL 从当前朝向平滑收敛；移动瞄准 SHALL 使身体朝向跟随瞄准水平意图、位移方向独立，不因后退/侧移而转向运动方向。非瞄准时 SHALL 正常朝移动方向转身。

静止瞄准 SHALL 支持有迟滞的上身先行与身体追随；初始起转/停转阈值为 15°/5°，静止追随速度 360°/s，可在不降低 over-shoulder-aim 验收标准的前提下调校。进入瞄准的平滑对齐 SHALL 使用独立收敛过程，覆盖初始 180° 差值，不受静止追随速度限制。稳定有效瞄准时身体与瞄准水平意图的残差 SHALL 不超过 35°；跨 ±180°和方向反转不发生取长路径旋转或阈值抖动。身体旋转 SHALL 不破坏相机世界视线。

#### Scenario: 进入瞄准瞬间下半身 snap 对齐相机
- **WHEN** 角色与相机 Yaw 相差较大时按下右键
- **THEN** 系统 SHALL 从当前姿态平滑对齐并在 0.35s 内收敛，替代旧瞬时 snap；期间结果标为过渡，不扭曲上身硬凑枪口

#### Scenario: 静止瞄准小幅转视角时脚不动（上半身扭腰）
- **WHEN** 静止瞄准的偏转保持在起转阈值内
- **THEN** 下半身 SHALL 保持方向，上身/手臂自然配合枪口对准，脚不随每次细小鼠标变化抖动

#### Scenario: 静止瞄准大幅转视角时下半身追身
- **WHEN** 静止瞄准偏转超过起转阈值
- **THEN** 身体 SHALL 配合转身动作追随，收敛至停转阈值，正常连续转向时残差满足限制，无突兀滑转或过度扭腰

#### Scenario: 瞄准移动时下半身锁相机不转身
- **WHEN** 长按右键并向 A/D、后方或斜向移动（瞄准时均为走路）
- **THEN** 身体 SHALL 跟随瞄准水平意图而非转向移动方向；实际位移和腿步方向一致，枪管满足 over-shoulder-aim 误差门槛

#### Scenario: 非瞄准时移动正常转身
- **WHEN** 松开右键后按 WASD
- **THEN** 角色 SHALL 恢复面向运动方向的控制，速度、跳跃与碰撞行为不退化

#### Scenario: 瞄准移动中 B1b.2 视觉验收不退化
- **WHEN** 瞄准并移动，观察持枪和越肩镜头
- **THEN** 持枪、双手和镜头 SHALL 正常、切换平滑，无动画/相机错误；验收不再绑定某一层名称、权重数值或虚拟相机优先级常量

#### Scenario: 跨越角度边界
- **WHEN** 瞄准方向从 +179° 越过 -179°，或在起转阈值附近反复小幅移动
- **THEN** 身体 SHALL 按最短连续方向响应，迟滞防止反复启停，不绕整圈或瞬间反向

### Requirement: StarterAssets 输入消费与 Look 直通

运动适配层 SHALL 在当前帧运动消费前提供权威 Model 的 Move/Sprint，并将 Jump 新按下转换为一次可消费请求；消费后不得因按钮仍按住而再次注入，释放重按才可再跳。SHALL 不反向写权威输入 Model，不修改供应商源控制器；允许项目自有适配副本替代供应商实例，但 SHALL 只有一个活跃运动消费者。

Look SHALL 从当前 PlayerInput 实例直通唯一相机表现输入缓冲，不进入业务输入 Model、不恢复 SendMessages、不重复累加鼠标 delta。缓冲不要求固定为供应商字段，但现有 Look 数值、停止归零、当前控制方案与订阅清理语义 SHALL 保留。

#### Scenario: SAInputAdapter 同步生效
- **WHEN** 当前帧输入更新已将 Move 写为 Vector2(1,0)
- **THEN** 活跃项目运动消费者 SHALL 在该帧读取方向，不等待下一帧 LateUpdate，适配器不反向改写 Model

#### Scenario: 已消费跳跃不被重复注入
- **WHEN** 按住空格完成起跳直到再次落地，运动消费者已清除该请求
- **THEN** SHALL 不重复起跳；释放后重新按下可再次跳，重力和落地规则保持

#### Scenario: Look 只有一个有效路由
- **WHEN** 通过现有输入方案移动鼠标后停止或取消动作
- **THEN** 相机 SHALL 每次只消费一份当前 Look 数值并在停止后归零，无额外旧输入入口或 delta 累加

#### Scenario: 适配器禁用与恢复
- **WHEN** 持续输入期间停用适配器，再重新启用并输入
- **THEN** 下游 move/look/jump/sprint SHALL 清零，旧回调不能写入，恢复后处理一次，销毁不产生回调异常

#### Scenario: 项目适配不双重执行
- **WHEN** 正式角色使用项目运动实现
- **THEN** 供应商原运动组件 SHALL 不同时消费输入、移动胶囊或写旋转；现有输入资产 GUID、Map、动作和绑定保持

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

### Requirement: 瞄准表现生命周期与当前状态一致

相机、动画、身体转向和枪口表现接入时 SHALL 读取当前权威瞄准状态并订阅后续变化，不依赖必须先收到一次未来事件。停用/销毁 SHALL 解除本组件订阅并清理其姿态/镜头/输出；重新启用 SHALL 恢复当前状态，不累积订阅。清理 SHALL 不创建架构，失焦和输入源停用 SHALL 沿既有输入恢复契约使表现与输出失效。

#### Scenario: 首次启用时已按住瞄准
- **WHEN** 输入先进入瞄准，随后表现组件首次绑定或重新启用
- **THEN** 表现 SHALL 立即读取当前意图并正常进入瞄准，不要求用户先松开再按一次才能同步

#### Scenario: 组件停用不继续切镜头
- **WHEN** 单独停用相机/姿态桥接组件，再改变瞄准输入
- **THEN** 被停用组件 SHALL 不响应旧事件写镜头或姿态；恢复后从当前状态接续，三次循环不叠加回调

#### Scenario: 失焦与场景退出
- **WHEN** 按住瞄准/方向时切走窗口、停用输入源或退出场景
- **THEN** 表现 SHALL 不保持失效瞄准结果，清理不抛异常/创建新架构；返回或新运行后按现有有效采样恢复，Jump/Fire 仍需释放重按

### Requirement: 举枪奔跑仅允许纯向前输入

系统 SHALL 保留原始 Sprint 按钮状态，以当前瞄准状态和运动方向计算实际奔跑许可。举枪时仅 Move.y > 0 且 Move.x 为零的前向输入可奔跑；数值零允许 0.0001 的浮点容差。A/S/D 及四个斜向 SHALL 使用走速，即使 Shift 仍按着。非瞄准奔跑规则不变。

#### Scenario: 持续按住 Shift 换向
- **WHEN** 举枪 W+Shift 奔跑后加入 A/D 或切到 S
- **THEN** 同帧实际水平速度 SHALL 不超过走速，步态切到走路；原始 Sprint SHALL 仍为按住

#### Scenario: 恢复前向与放下枪
- **WHEN** 保持 Shift 后松回纯 W，或在侧向/后退时退出瞄准
- **THEN** SHALL 恢复可用奔跑并自然加速，无须重新按 Shift

#### Scenario: 空中与切入瞄准
- **WHEN** 空中改变方向，或非瞄准侧跑时开始举枪
- **THEN** 同一许可 SHALL 立即生效，禁止的瞄准方向不保留跑速；跳跃物理不变
