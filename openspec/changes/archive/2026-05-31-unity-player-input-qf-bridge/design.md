## Context

B1b.2 (`unity-character-aim-layer`) 已归档，瞄准动画层与双相机切换可用。但遗留三个问题：

1. **输入入口未 QF 化**：`TempAimInputDriver` 直接读 `Input.GetMouseButton(1)`，其余输入（移动/跳跃/冲刺）仍由 `StarterAssetsInputs` 的 `OnXxx(InputValue)` 回调直接写字段，没有经过 QFramework Model 层。
2. **MoveX/Y 绕过 QF**：`AnimatorAimBridge.Update()` 直接读 `Input.GetAxis`，违反单一数据源规范。
3. **Strafe 缺失**：瞄准移动时角色朝移动方向转身（TPC 默认行为），而非锁定相机朝向进行侧移。

约束：`ThirdPersonController.cs` 及 `StarterAssetsInputs.cs` 为第三方文件，**不得修改**。

---

## Goals / Non-Goals

**Goals:**

- `PlayerInputModel` 成为全部输入状态的唯一数据源（Move/Jump/Sprint/IsAiming/Fire）
- 所有输入 → Command → Model 路径完整走 QF 链路
- `StarterAssetsInputs` 字段退化为 `ThirdPersonController` 的只读缓冲，由 `SAInputAdapter` LateUpdate 单向写入
- 瞄准时角色身体锁定相机朝向（Strafe 侧移，不转身）
- `AnimatorAimBridge` MoveX/Y 改从 `PlayerInputModel.Move` 读取
- 临时文件 `TempAimInputDriver.cs` 删除
- 禁用 `PlayerController` 后角色完全无响应（验证唯一入口）

**Non-Goals:**

- 不实现射击逻辑（`SetFireInputCommand.OnExecute` 骨架，B2a 填充）
- 不修改 `ThirdPersonController` 或 `StarterAssetsInputs` 的任何代码
- 不实现 Look（视角旋转）的 QF 化（`_input.look` 仍由 SA 默认路由处理，未列入 PlayerInputModel）
- 不修改 B1b.2 的双相机切换逻辑或 Animator Controller 资产

---

## Decisions

### D1 — Adapter 方案：保留 StarterAssetsInputs，新增 SAInputAdapter 单向写入

**问题**：`ThirdPersonController` 大量读取 `StarterAssetsInputs` 的公开字段，不可绕开。

**选项**：
- **A（Adapter）**：`PlayerController` 把输入写入 QF Model，`SAInputAdapter` LateUpdate 把 Model 值单向 copy 到 `StarterAssetsInputs` 字段。TPC 无感知继续读 SA 字段。
- **B（Fork TPC）**：复制 `ThirdPersonController.cs` 到项目目录，改为直接读 Model。

**决策**：选 A。Fork TPC 会引入维护负担，且违反"第三方只读"约定。Adapter 模式完全不动第三方文件。

**执行顺序**（Unity `[DefaultExecutionOrder]` 控制）：
```
PlayerController      [-20]  每帧：PlayerInput 回调 → SendCommand → Model 更新
SAInputAdapter        [-10]  LateUpdate：Model → SAInputs 字段写入
ThirdPersonController  [0]   Update/LateUpdate：读 SAInputs 字段驱动移动
StrafeController       [LateUpdate, 默认0, 在TPC LateUpdate后运行]
```
> SAInputAdapter 用 LateUpdate Execution Order `-10`（早于 TPC.Update），确保 TPC 每帧 Update 时 SAInputs 已是最新值。

---

### D2 — Strafe 实现：StrafeController LateUpdate 覆盖朝向

**问题**：TPC 的 `Move()` 每帧 Update 中把 `transform.rotation` 设为移动方向，无法外部干预（_targetRotation 是 private）。

**选项**：
- **A（LateUpdate 覆盖）**：独立脚本 `StrafeController`，在 TPC 运行后（LateUpdate），当瞄准时强制 `transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0)`。
- **B（伪造 _input.move）**：SAInputAdapter 在瞄准时重算 move 向量，让 TPC 认为输入方向就是相机方向。副作用难控，易出现速度/角度计算错误。
- **C（修改 TPC RotationSmoothTime）**：public 字段，但 _targetRotation 是 private 无法外部写，不可行。

**决策**：选 A。TPC.Update 写朝向 → StrafeController.LateUpdate 覆盖，每帧只有一次覆盖，视觉无感知延迟（同帧内完成）。TPC 内的位移计算（`_controller.Move`）仍用 `_targetRotation`（即移动方向），所以角色仍向输入方向移动，只是朝向被锁到相机。这正是 Strafe 的本质：**移动方向 ≠ 朝向**。

**MoveX/Y 坐标空间**：Strafe 实现后，移动时 `角色Forward = 相机Forward`，因此 `PlayerInputModel.Move.x` = 侧移方向，`.y` = 前后方向，直接写入 `MoveX / MoveY` 参数即为正确 Local Space 坐标，无需额外变换。（静止死区状态下 `Move ≈ 0`，无侧移动画播放，坐标空间差异不影响表现。）

**修订（B1b.3 apply 期）— Strafe 朝向从「硬锁」升级为「死区迟滞」TPS 模型**

原 A 方案每帧无条件 `transform.rotation = 相机Yaw`，导致下半身永远黏住相机、上半身无 Yaw 残差，缺失 TPS「扭腰领先 + 脚步迟滞」手感。`AnimatorAimBridge` 注释中描述的 `yawOffset`（上半身补偿下半身与相机 Yaw 差）也因残差恒为 0 而被删除。最终改为标准 TPS 双层模型，职责重新切清：**下半身 Yaw 归 `StrafeController`，上半身 Yaw + Pitch 归 `AnimatorAimBridge`**。

- **下半身 Yaw（`StrafeController`）**：
  - 进入瞄准当帧：snap 对齐相机 Yaw（一次性，上下半身整体对齐）
  - 保持瞄准 + 静止（`Move ≈ 0`）：死区迟滞——`|DeltaAngle(身体Yaw, 相机Yaw)| ≤ 死区(默认 ±10°)` 时脚不动；超过死区时身体 Yaw 以可配置角速度 `MoveTowardsAngle` 平滑追向相机
  - 保持瞄准 + 移动（`Move ≠ 0`）：下半身直接锁相机 Yaw（无死区），保证 strafe BlendTree 方向正确
- **上半身 Yaw + Pitch（`AnimatorAimBridge`）**：
  - `yawOffset = DeltaAngle(身体Yaw, 相机Yaw)`（死区内残差即扭腰量）+ `pitch = 相机俯仰角`（限幅 ±50°），在**世界空间**用 `Quaternion.AngleAxis`（Yaw 绕世界 Up、Pitch 绕角色 Right、`-pitch`=抬头）左乘到 spine 骨骼世界旋转
  - ⚠️ **不可用骨骼 local Euler 叠加**：实测 spine_03 的 local 轴 X≈世界下方 / Y≈角色前方 / Z≈角色左方，与世界轴完全不对齐，local Euler 会把 pitch 施加成水平偏转 → 上半身「偏左」、俯仰错位（B1b.3 首次实现踩坑，诊断脚本见 tasks 12.x）


死区阈值与追身角速度暴露为 `SerializeField`，Play 时可实时调（±10° 死区偏小、扭腰层次弱时当场加到 20~30°）。

---

### D3 — .inputactions 资产：复制 SA 原文件到项目目录后扩展

**问题**：SA 提供的 `.inputactions` 已定义完整 Action Map（Move/Look/Jump/Sprint），但在 ThirdParty 不可改。需新增 Aim/Fire 两个 Action。

**决策**：`AssetDatabase.CopyAsset` 将 SA `.inputactions` 复制到 `Assets/_Project/Settings/UnomataPlayer.inputactions`，在副本上新增 `Aim`（Button，Binding: `<Mouse>/rightButton`）和 `Fire`（Button，Binding: `<Mouse>/leftButton`）。`PlayerArmature` 的 `PlayerInput` 组件 Asset 字段改引用新副本。

---

### D4 — PlayerSystem IsAiming 触发时机：订阅 Model 变化，非外部 SendCommand

**当前**（B1b.2）：`TempAimInputDriver` 直接 `SendCommand<SetAimStateCommand>` → `PlayerSystem.SetAiming(bool)` → 写 Model + SendEvent。

**B1b.3 后**：`PlayerController` → `SendCommand<SetAimStateCommand>` → `PlayerSystem.SetAiming(bool)` 路径不变。`PlayerSystem.OnInit` 额外订阅 `PlayerInputModel.IsAiming` 变化，确保即使未来有其他途径改变 PlayerInputModel.IsAiming，`SetAiming` 逻辑也自动触发。

> `SetAimStateCommand` 本身仍保留（PlayerController 使用），不废弃。

---

## Risks / Trade-offs

| 风险 | 缓解 |
|------|------|
| SAInputAdapter LateUpdate 顺序早于 TPC.Update 但晚于 TPC.LateUpdate（`CameraRotation`） | CameraRotation 读 `_input.look`，look 不进 PlayerInputModel，SA 默认路由不断，无影响 |
| StrafeController 与 TPC.LateUpdate 的 `CameraRotation` 争夺同帧执行顺序 | StrafeController 只改 `transform.rotation`，TPC.LateUpdate 不改 `transform.rotation`（它改 `CinemachineCameraTarget.transform.rotation`），两者独立，无冲突 |
| `PlayerInput.Behavior = Invoke C# Events` 切换后，SA 的 `OnMove/OnJump/OnSprint` 回调自动失效 | 这正是目标行为。`PlayerController` 接管全部回调，SA 字段由 `SAInputAdapter` 写入 |
| `_input.jump` 在 TPC 内 `JumpAndGravity` 末尾被置 false（`_input.jump = false`） | SAInputAdapter 单向写入，下一帧 `PlayerInputModel.Jump` 如仍为 true 会再次写 SA.jump=true，符合 BindableProperty 每帧写语义 |
| .inputactions 副本与 SA 原件 diverge（SA 更新时副本不跟进） | 本项目 SA 为冻结第三方资产，不走 Package Manager 更新，diverge 风险极低 |

---

## Migration Plan

1. 复制 `.inputactions` 到项目目录（UnityMCP `manage_asset action=duplicate`）
2. 新建脚本（PlayerInputModel / PlayerController / SAInputAdapter / StrafeController / 4 Commands）
3. 修改 `GameApp`、`PlayerSystem`、`AnimatorAimBridge`
4. 场景接线（PlayerInput.Behavior 切换 + 新组件挂载 + 旧 TempAimInputDriver 移除）
5. Play Mode 验收：
   - 禁用 `PlayerController` → 角色无响应
   - 移动/跳跃/冲刺/瞄准全部响应正常
   - 瞄准移动时侧移（不转身）、上半身动画 BlendTree 响应正确
   - Console 零红错

**回滚**：保留 `StarterAssetsInputs.cs` 不动，回滚只需恢复场景接线（PlayerInput.Behavior 改回 `Send Messages`）并还原 `AnimatorAimBridge`，TPC 即可恢复旧行为。

---

## Open Questions

无。所有约束与实现方案已在 explore 阶段确认。

> Fire Action 绑定鼠标左键，因此 `PlayerController` 中 `PlayerInput.actions` 需注意瞄准状态下左键不干扰相机点击。Phase 5 数值平衡时若需要区分瞄准/腰射精度，`PlayerController` 读 `PlayerInputModel.IsAiming` 判断即可，Command 层无需改动。
