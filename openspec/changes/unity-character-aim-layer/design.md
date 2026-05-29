# Design: unity-character-aim-layer

## 相机设计

### 越右肩构图原则

两种视角均保持角色在画面偏左（越右肩），右侧空间预留给接龙 UI。

```
 非瞄准（自由视角）             瞄准（锁定视角）
 ┌──────────────────────┐      ┌──────────────────────┐
 │                      │      │          ┼           │
 │   [玩]               │      │  [玩]                │
 │   [家]  敌人  环境   │      │  [家]  敌人  [接龙UI]│
 │                      │      │  举枪                │
 │  相机可绕角色旋转     │      │  角色+相机朝向锁定   │
 └──────────────────────┘      └──────────────────────┘
```

### Cinemachine 参数（Cinemachine 2.x · 3rd Person Follow）

| 参数 | PlayerFollowCamera（已有） | PlayerAimCamera（新建） |
|------|--------------------------|------------------------|
| Priority（默认） | 10 | 0 |
| Priority（瞄准激活） | 10 | 15 |
| FOV | 60 | 45 |
| Shoulder Offset X | 0.4 | 0.35 |
| Shoulder Offset Y | 0.0 | 0.0 |
| Shoulder Offset Z | 0.0 | 0.0 |
| Vertical Arm Length | 0.3 | 0.2 |
| Camera Distance | 4.5 | 2.8 |
| Camera Radius | 0.2 | 0.2 |
| Damping X / Y / Z | 0.1 / 0.2 / 0.1 | 0.05 / 0.1 / 0.05 |

**过渡时间**：Cinemachine Brain 默认 Ease In Out，混合时长 **0.3s**（Brain `Default Blend` 字段保持默认）。

> Phase 5 数值平衡时可调整 FOV / Camera Distance / Shoulder Offset。

---

## 动画层设计

### Avatar Mask（UpperBody.mask）

- Humanoid Mask
- 勾选：Spine / Chest / UpperChest / Neck / Head / LeftShoulder / LeftUpperArm / LeftLowerArm / LeftHand / RightShoulder / RightUpperArm / RightLowerArm / RightHand
- 不勾：Hips / LeftUpperLeg / LeftLowerLeg / LeftFoot / RightUpperLeg / RightLowerLeg / RightFoot / Root

### UpperBodyAim Layer 参数

| 属性 | 值 |
|------|----|
| Blending | Override |
| Default Weight | 0（非瞄准时完全走 Base Layer） |
| Mask | UpperBody.mask |

### UpperBodyAim 状态机

```
[Entry] → [Empty(空状态, 无动画)]
              │  IsAiming=true
              ▼
          [AimMove BlendTree]
              │  IsAiming=false
              └────────────────▶ [Empty]
```

### AimMove BlendTree（2D Cartesian）

| 参数 | 说明 |
|------|------|
| 类型 | Blend Tree · 2D Simple Directional（或 Freeform Cartesian） |
| X 轴参数 | `MoveX`（新增 float 参数，范围 -1~1） |
| Y 轴参数 | `MoveY`（新增 float 参数，范围 -1~1） |

| Motion | Position (X, Y) |
|--------|-----------------|
| R_AimIdle | (0, 0) |
| R_AimWalk_F | (0, 1) |
| R_AimWalk_B | (0, -1) |
| R_AimWalk_L（实为 FL/BL 中间值） | (-1, 0) |
| R_AimWalk_R（实为 FR/BR 中间值） | (1, 0) |
| R_AimWalk_FL | (-0.7, 0.7) |
| R_AimWalk_FR | (0.7, 0.7) |
| R_AimWalk_BL | (-0.7, -0.7) |
| R_AimWalk_BR | (0.7, -0.7) |

> `MoveX` / `MoveY` 由 `AnimatorAimBridge` 每帧从 `Input.GetAxis("Horizontal")` / `"Vertical"` 读取并写入 Animator（B1b.3 后改从 `PlayerInputModel` 读）。

### Layer Weight 过渡

`AnimatorAimBridge.Update()` 用 `Mathf.MoveTowards` 驱动：
- 进入瞄准：0 → 1，速率 = 1/0.15f（约 0.15s）
- 退出瞄准：1 → 0，速率相同

---

## QF 数据流

```
[TempAimInputDriver]
  Input.GetMouseButton(1) 按下/松开
    │
    ▼  SendCommand
[SetAimStateCommand(bool isAiming)]
    │
    ▼  GetSystem
[PlayerSystem.SetAiming(bool)]
    │  写 Model
    ├──▶ PlayerModel.IsAiming = value
    │  SendEvent
    └──▶ AimStateChangedEvent { IsAiming }
              │
    ┌─────────┴──────────┐
    ▼                    ▼
[AnimatorAimBridge]  [CameraAimBridge]
  SetLayerWeight       PlayerAimCamera.Priority = 15 / 0
  IsAiming → Animator  Cinemachine Brain 自动 0.3s 过渡
  参数驱动
```

### 新增类清单

| 类 | 路径 | 职责 |
|----|------|------|
| `AimStateChangedEvent` | `Gameplay/Events/` | struct，含 `bool IsAiming` |
| `SetAimStateCommand` | `Gameplay/Commands/` | AbstractCommand，OnExecute 调 PlayerSystem.SetAiming |
| `AnimatorAimBridge` | `Gameplay/Player/` | IController，订阅 Event，驱动 Layer Weight + MoveX/Y 参数 |
| `CameraAimBridge` | `Gameplay/Player/` | IController，订阅 Event，切换 PlayerAimCamera Priority |
| `TempAimInputDriver` | `Gameplay/Player/` | 普通 MB（非 IController），读 Input.GetMouseButton(1)，SendCommand |

### PlayerModel 新增字段

```csharp
public BindableProperty<bool> IsAiming { get; } = new BindableProperty<bool>(false);
```

### PlayerSystem 新增方法

```csharp
public void SetAiming(bool isAiming)
{
    this.GetModel<PlayerModel>().IsAiming.Value = isAiming;
    this.SendEvent(new AimStateChangedEvent { IsAiming = isAiming });
}
```

---

## 已知遗留（不阻塞本 change 归档）

- **Strafe 朝向**：瞄准时移动，角色仍会转身面朝移动方向（不是侧移）。B1b.3 处理。
- **MoveX/Y 参数临时读法**：`AnimatorAimBridge` 直接读 `Input.GetAxis`，B1b.3 改从 `PlayerInputModel` 读。
- **准心 UI**：本 change 不做，Phase 3 副线 UI 阶段补。
- **PlayerFollowCamera Shoulder Offset 调整**：当前 PlayerFollowCamera 的 Shoulder Offset 可能为默认值（居中或轻微偏移），需在本 change 内调整到 X=0.4。
